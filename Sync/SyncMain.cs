using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks;
using MultMain = MultiplayerBase.MultiplayerMain;
using Net = MultiplayerBase.Handlers.HandlerSystem;
using MultiplayerBase.Handlers;
using static CombineCardSystem;
using UnityEngine;
using HarmonyLib;

using Wave = BattleWaveManager.Wave;
using System.Collections;
using static Console;
using UnityEngine.SceneManagement;
using WildfrostHopeMod.VFX;
using UnityEngine.UI;
using WildfrostHopeMod;
using MultiplayerBase.Battles;
using MultiplayerBase;

namespace Sync
{
    public class SyncMain : WildfrostMod
    {
        internal static SyncMain Instance;
        //public static Dictionary<Friend, bool> synced;
        public static bool sync = false;
        public static int syncNextTurn = 0;
        public static bool sentSyncMessage = false;
        public static int syncCombo = 0;

        [ConfigManagerTitle("Synched Enemy Per Wave")]
        [ConfigManagerDesc("Determines the number of enemy units with sync effects <i>per wave</i>")]
        [ConfigOptions(0,1,2,3)]
        [ConfigItem(1, "", "SyncEnemies")]
        public int enemyPerWave = 1;

        [ConfigManagerTitle("Harder Enemy Syncs?")]
        [ConfigManagerDesc("Do you want more challenging enemies?")]
        [ConfigOptions("No", "Of course!")]
        [ConfigItem("Of course!","", "HarderSyncs")]
        public string harderSync = "No";

        [ConfigManagerTitle("Item Sync Frequency")]
        [ConfigManagerDesc("Sync effects provide a bonus if another player has played a sync card recently")]
        [ConfigItem(0.33f, "", "SyncItems")]
        public float itemSyncChance = 0.33f;

        [ConfigManagerTitle("Item Mystic Frequency")]
        [ConfigManagerDesc("Mystic allows items to be played on the board of another player")]
        [ConfigItem(0.2f, "", "MystItems")]
        public float itemMystChance = 0.2f;

        [ConfigManagerTitle("Item Promo Frequency")]
        [ConfigManagerDesc("Promo gives copies of the selected card to other players")]
        [ConfigItem(0.1f, "", "PromoItems")]
        public float itemPromoChance = 0.1f;

        [ConfigManagerTitle("Gaiden Frequency")]
        [ConfigManagerDesc("Gaiden allows companions in the reserve to join other player's battles")]
        [ConfigItem(0.05f, "", "GaidenComp")]
        public float compGaidenChance = 0.05f;

        private static bool commandsLoaded = false;

        public static List<object> assets = new List<object>();

        private List<StatusEffectDataBuilder> effects = new List<StatusEffectDataBuilder>();
        private List<KeywordDataBuilder> keywords = new List<KeywordDataBuilder>();
        public SyncMain(string modDirectory) : base(modDirectory)
        {
        }

        public override string GUID => "mhcdc9.wildfrost.sync";

        public override string[] Depends => new string[] {"hope.wildfrost.configs", "hope.wildfrost.vfx", "mhcdc9.wildfrost.multiplayer"};

        public override string Title => "Sync and Mystic";

        public override string Description => "Complicate the game by entangling multiple board states together.";

        public CardData.StatusEffectStacks SStack(string name, int count) => new CardData.StatusEffectStacks(Get<StatusEffectData>(name), count);

        public CardData.TraitStacks TStack(string name, int count) => new CardData.TraitStacks(Get<TraitData>(name), count);



        public override void Load()
        {
            Instance = this;
            CreateStatuses();
            Events.OnBattleStart += ClearSync;
            Events.OnBattlePreTurnStart += CheckSync;
            Events.OnEntityOffered += ApplyTraitsToItem;
            Events.OnEntityChosen += CheckPromo;
            Events.OnShopItemPurchase += CheckPromoShop;
            Events.OnCampaignGenerated += ModifyStartingInventory;

            GaidenSystem.Enable();
            Net.HandlerRoutines.Add("SYNC", SYNC_Handler);
            base.Load();
            if (!commandsLoaded)
            {
                Events.OnSceneChanged += Commands;
            }
            //commands.Add(new CommandSync());
        }

        public override void Unload()
        {
            Events.OnBattleStart -= ClearSync;
            Events.OnBattlePreTurnStart -= CheckSync;
            Events.OnEntityOffered -= ApplyTraitsToItem;
            Events.OnEntityChosen -= CheckPromo;
            Events.OnShopItemPurchase -= CheckPromoShop;
            Events.OnCampaignGenerated -= ModifyStartingInventory;
            GaidenSystem.Disable();
            Net.HandlerRoutines.Remove("SYNC");
            base.Unload();
            if (!commandsLoaded)
            {
                Events.OnSceneChanged -= Commands;
            }
        }

        

        public void Commands(Scene scene)
        {
            if (scene.name == "Battle" && Console.commands != null)
            {
                Console.commands.Add(new CommandSync());
                commandsLoaded = true;
                Events.OnSceneChanged -= Commands;
            }
        }

        public void ClearSync()
        {
            syncNextTurn = 0;
        }

        public void CreateStatuses()
        {
            #region GAIDEN
            assets.Add(Extensions.CreateBasicKeyword(this, "gaiden", "Gaiden", "Fights for other players whilst waiting in the reserve"));

            assets.Add(new StatusEffectDataBuilder(this)
                .Create<StatusEffectGaiden>("Find New Battles")
                .WithType("mhcdc9.gaiden"));

            assets.Add(this.CreateTrait("Gaiden", "gaiden", false, "Find New Battles"));

            assets.Add(Extensions.CreateBasicKeyword(this, "sidequesting", "Side-Questing", "This fight is not their highest priority"));

            assets.Add(new StatusEffectDataBuilder(this)
                .Create<StatusEffectSideQuest>("Prepare To Leave")
                .WithType("mhcdc9.gaiden"));

            assets.Add(this.CreateTrait("SideQuest", "sidequesting", false, "Prepare To Leave"));

            #endregion

            #region PROMO
            assets.Add(Extensions.CreateBasicKeyword(this, "promo", "Promo", "Upon pickup, gives a copy to all players|To their hand if possible"));

            assets.Add(new StatusEffectDataBuilder(this)
                .Create<StatusEffectPromo>("Send Copies Elsewhere")
                .WithCanBeBoosted(false)
                .WithType("")
                .WithConstraints(Extensions.NotGoop())
                );

            assets.Add(this.CreateTrait("Promo", "promo", false, "Send Copies Elsewhere"));
            #endregion

            #region MYSTIC
            assets.Add(Extensions.CreateBasicKeyword(this, "mystic", "Mystic", "Playable on another board|Reward: Add Zoomlin"));

            assets.Add(new StatusEffectDataBuilder(this)
                .Create<StatusEffectMystical>("Play Elsewhere")
                .WithCanBeBoosted(false)
                .WithType("")
                .WithConstraints(Extensions.IsItem(), Extensions.IsPlay(), Extensions.TargetsBoard())
                .FreeModify<StatusEffectApplyX>(
                (data) =>
                {
                    data.effectToApply = Get<StatusEffectData>("Temporary Zoomlin");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            assets.Add(this.CreateTrait("Mystic", "mystic", false, "Play Elsewhere"));

            assets.Add(new StatusEffectDataBuilder(this)
                .CreateTempTrait("Temporary Mystic", "Mystic")
                .WithConstraints()
                );
            #endregion

            #region SYNC
            assets.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Mystic", "<keyword=mhcdc9.wildfrost.sync.sync>: <keyword=mhcdc9.wildfrost.sync.mystic>", "", "Temporary Mystic")
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    data.targetConstraints = new TargetConstraint[] { Extensions.IsItem(), Extensions.IsPlay(), Extensions.TargetsBoard(), Extensions.NotTrait("Mystic") };
                }));

            assets.Add(Extensions.CreateBasicKeyword(this, "sync", "Sync", "Gain an effect when a <sync> card is played elsewhere|Effect *usually* lasts until end of turn"));
            

            assets.Add(new StatusEffectDataBuilder(this)
                .CreateTempTrait("Temporary Smackback", Get<TraitData>("Smackback"))
                .WithConstraints(Extensions.DoesAttack())
                );

            assets.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Attack", "<keyword=mhcdc9.wildfrost.sync.sync>: <+{a}><keyword=attack>", "", "Ongoing Increase Attack", boostable: true)
                .WithConstraints(Extensions.DoesDamage())
                );

            assets.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Effect", "<keyword=mhcdc9.wildfrost.sync.sync>: +{a} to effects", "", "Ongoing Increase Effects")
                .WithConstraints(Extensions.CanBeBoosted())
                );

            assets.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Frenzy", "<keyword=mhcdc9.wildfrost.sync.sync>: <x{a}><keyword=frenzy>", "", "MultiHit", boostable: true)
                .WithConstraints( Get<StatusEffectData>("Instant Apply Frenzy (To Card In Hand)").targetConstraints.Append(Extensions.NotGoop()).ToArray() )
                );

            assets.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Barrage", "<keyword=mhcdc9.wildfrost.sync.sync>: <keyword=barrage>", "", "Temporary Barrage")
                .WithConstraints(Extensions.DoesAttack(), Extensions.NotTrait("Barrage"), Extensions.TargetsBoard())
                );

            assets.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Zoomlin", "<keyword=mhcdc9.wildfrost.sync.sync>: <keyword=zoomlin>", "", "Temporary Zoomlin")
                .WithConstraints(Extensions.NotTrait("Zoomlin"), Extensions.NotGoop())
                );

            assets.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Smackback", "<keyword=mhcdc9.wildfrost.sync.sync>: <keyword=smackback>", "", "mhcdc9.wildfrost.sync.Temporary Smackback")
                .WithConstraints(Extensions.DoesAttack(), Extensions.NotTrait("Smackback"))
                );

            assets.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Heal", "<keyword=mhcdc9.wildfrost.sync.sync>: Restore <{a}><keyword=health>", "", "Heal", boostable: true, ongoing: false)
                .WithConstraints(Extensions.HasHealth())
                );

            assets.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Counter", "<keyword=mhcdc9.wildfrost.sync.sync>: Count down <keyword=counter> by <{a}>", "", "Reduce Counter", boostable: true, ongoing: false)
                .WithConstraints(Extensions.HasCounter(), ScriptableObject.CreateInstance<TargetConstraintOnBoard>())
                );

            

            assets.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Nothing", "<keyword=mhcdc9.wildfrost.sync.sync>: Do nothing?", "", "", ongoing: false)
                .WithConstraints(Extensions.IsPlay())
                );

            assets.Add(new StatusIconBuilder(this)
                .Create("sync icon", "mhcdc9.sync", GetSprite("status_sync.png"))
                .WithIconGroupName(StatusIconBuilder.IconGroups.crown)
                .WithKeywords("sync")
                .WithTextboxSprite(ImagePath("status_sync_arrow.png").ToSprite())
                .SubscribeToAfterAllBuildEvent(data =>
                {
                    Transform parent = data.icon.transform;
                    parent.GetComponent<Image>().color = new Color(1, 1, 1, 0.1f);
                    GameObject cycle = new GameObject("Cycle");
                    cycle.transform.SetParent(parent, false);
                    cycle.transform.SetAsFirstSibling();
                    cycle.AddComponent<Image>().sprite = GetSprite("status_sync_arrow.png");
                    cycle.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f, 1f);
                    cycle.AddComponent<SyncArrows>().enabled = false;
                    cycle.GetComponent<RectTransform>().sizeDelta = new Vector2(1f, 1f);
                }));
            #endregion SYNC
        }

        internal static Sprite GetSprite(string s)
        {
            return Instance.ImagePath(s).ToSprite();
        }

        static (string, int)[] ItemSyncEffects = new (string, int)[]
        {
            ("Sync Attack", 1),
            ("Sync Attack", 2),
            ("Sync Effect", 1),
            ("Sync Frenzy", 1),
            ("Sync Barrage", 1),
            ("Sync Zoomlin", 1),
            ("Sync Mystic", 1),
            ("Sync Nothing", 1)
        };

        public override List<T> AddAssets<T, Y>()
        {
            if (assets.OfType<T>().Any())
                Debug.LogWarning($"[{Title}] adding {typeof(Y).Name}s: {assets.OfType<T>().Select(a => a._data.name).Join()}");
            return assets.OfType<T>().ToList();
        }

        public Task ModifyStartingInventory()
        {
            if (References.PlayerData?.inventory?.deck != null)
            {
                foreach (CardData data in References.PlayerData.inventory.deck)
                {
                    if (Dead.Random.Range(0f, 1f) < itemMystChance)
                    {
                        Extensions.TryAddTrait(data, TStack("Mystic", 1));
                    }
                    if (Dead.Random.Range(0f, 1f) < itemSyncChance && data.cardType.name == "Item")
                    {
                        Extensions.TryAddSync(data, ItemSyncEffects);
                    }
                    
                }
            }
            return Task.CompletedTask;
        }

        internal void ApplyTraitsToItem(Entity entity)
        {
            if (entity.data.cardType.name == "Item")
            {
                if (Dead.Random.Range(0f, 1f) < itemMystChance)
                {
                    Extensions.TryAddTrait(entity, TStack("Mystic", 1));
                }
                if (Dead.Random.Range(0f,1f) < itemPromoChance)
                {
                    Extensions.TryAddTrait(entity, TStack("Promo", 1));
                }
                if (Dead.Random.Range(0f, 1f) < itemSyncChance)
                {
                    Extensions.TryAddSync(entity, ItemSyncEffects);
                }

            }
            if (entity.data.cardType.name == "Clunker")
            {
                if (Dead.Random.Range(0f, 1f) < itemPromoChance)
                {
                    Extensions.TryAddTrait(entity, TStack("Promo", 1));
                }
            }
            if (entity.data.cardType.name == "Friendly")
            {
                if (Dead.Random.Range(0f, 1f) < compGaidenChance)
                {
                    Extensions.TryAddTrait(entity, TStack("Gaiden", 1));
                }
            }
        }

        internal static void CheckSync(int turnCount)
        {
            sentSyncMessage = false;
            sync = false;
            UnityEngine.Debug.Log($"[Sync] You can sync again.");
            PerformSync();
        }
        
        internal static void CheckPromo(Entity entity)
        {
            CardData.TraitStacks stack = entity.data.traits.FirstOrDefault(t => t.data.name == Instance.GUID + "." + "Promo");
            if (stack != null)
            {
                //entity.data.traits.Remove(stack);
                string message = Net.ConcatMessage(false, "PROM", CardEncoder.Encode(entity.data));
                Net.SendMessageToAllOthers("SYNC", message, "Sending...");
            }
        }

        public void CheckPromoShop(ShopItem item)
        {
            Entity entity = item.GetComponent<Entity>();
            if (entity != null)
            {
                CheckPromo(entity);
            }
        }

        internal void SYNC_Handler(Friend friend, string message)
        {
            string[] messages = Net.DecodeMessages(message);
            Debug.Log("[Sync] " + message);
            switch(messages[0])
            {
                case "PROM":
                    CardData data = CardEncoder.DecodeData(messages.Skip(1).ToArray());
                    if (MissingCardSystem.IsMissing(data))
                    {
                        break;
                    }
                    if (References.PlayerData?.inventory?.deck != null)
                    {
                        References.PlayerData.inventory.deck.Add(data);
                        if (!HandlerBattle.instance.Queue(new ActionAddCardToBattle(data)))
                        {
                            MultTextManager.AddEntry($"Received {data.title} from {friend.Name}", 0.55f, new Color(1f, 0.75f, 0.38f), 1f);
                        }
                    }
                    break;
                case "SYNC":
                    int combo = int.Parse(messages[1]);
                    if (combo != 0)
                    {
                        syncNextTurn = Math.Max(syncNextTurn + 1, combo);
                    }
                    else
                    {
                        syncNextTurn = 0;
                    }
                    if (Battle.instance != null && Battle.instance.phase == Battle.Phase.Play && !sync)
                    {
                        PerformSync();
                    }
                    break;
                case "GAIDEN":
                    GaidenSystem.GAIDEN_Handler(friend, messages);
                    break;
                default:
                    Debug.Log("[Sync] Unknown message");
                    break;
            }
            
        }

        internal static void PerformSync()
        {
            if (syncNextTurn == 0)
            {
                syncCombo = 0;
                sync = false;
                ActionSync action = new ActionSync(0);
                ActionQueue.Add(action);
            }
            if (syncNextTurn > 0)
            {
                syncCombo++;
                sync = true;
                ActionSync action = new ActionSync(syncCombo);
                ActionQueue.Add(action);
                syncNextTurn = 0;
            }
        }

        public static void SyncOthers()
        {
            Net.SendMessageToAllOthers("SYNC", Net.ConcatMessage(true, "SYNC", $"{syncCombo + 1}"));
            //sentSyncMessage = true; 
        }
    }

    [HarmonyPatch(typeof(ScriptBattleSetUp), "SetUpEnemyWaves")]
    static class AddSyncToEnemies
    {
        static (string, int)[] EnemySyncEffects = new (string, int)[]
        {
            ("Sync Attack", 3),
            ("Sync Effect", 2),
            ("Sync Frenzy", 2),
            ("Sync Barrage", 1),
            ("Sync Heal", 4),
            ("Sync Counter", 1),
            ("Sync Smackback",1)
        };

        static (string, int)[] EasierEnemySyncEffects = new (string, int)[]
        {
            ("Sync Attack", 2),
            ("Sync Effect", 1),
            ("Sync Frenzy", 1),
            ("Sync Attack", 3),
            ("Sync Heal", 3),
            ("Sync Counter", 1),
            ("Sync Smackback",1)
        };


        static void Postfix(Character enemy)
        {
            BattleWaveManager component = enemy.GetComponent<BattleWaveManager>();
            foreach (Wave wave in component.list)
            {
                int points = SyncMain.Instance.enemyPerWave;
                foreach (CardData data in wave.units.InRandomOrder())
                {
                    (string, int)[] effects = SyncMain.Instance.harderSync == "No" ? EasierEnemySyncEffects : EnemySyncEffects;
                    if (points > 0 && Extensions.TryAddSync(data, effects))
                    {
                        points--;
                    }
                }
            }
        }

    }

    public class CommandSync : Command
    {
        public override string id => "sync";

        public override string format => "sync <amount>";

        public override void Run(string args)
        {
            int result = 10;
            Character player;
            if (args.Length > 0 && !int.TryParse(args, out result))
            {
                Fail("Invalid amount! (" + args + ")");
            }
            else if (TryGetPlayer(out player))
            {
                Net.SendMessage("SYNC", Net.self, Net.ConcatMessage(true, "SYNC", "1"));
            }

        }
    }
}
