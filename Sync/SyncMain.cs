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

        public static int enemyPerWave = 1;
        public static float itemSyncChance = 0.33f;

        public static float itemMystChance = 0.2f;

        private static bool commandsLoaded = false;

        private List<StatusEffectDataBuilder> effects = new List<StatusEffectDataBuilder>();
        private List<KeywordDataBuilder> keywords = new List<KeywordDataBuilder>();
        public SyncMain(string modDirectory) : base(modDirectory)
        {
        }

        public override string GUID => "mhcdc9.wildfrost.sync";

        public override string[] Depends => new string[0];

        public override string Title => "Sync and Mystic";

        public override string Description => "Complicate the game by entangling multiple board states together.";

        public CardData.StatusEffectStacks SStack(string name, int count) => new CardData.StatusEffectStacks(Get<StatusEffectData>(name), count);



        public override void Load()
        {
            Instance = this;
            CreateStatuses();
            Events.OnBattleStart += ClearSync;
            Events.OnBattlePreTurnStart += CheckSync;
            Events.OnEntityOffered += ApplySyncItem;
            Events.OnCampaignGenerated += SyncStartingInventory;
            Events.OnEntityOffered += ApplyMystItem;
            Events.OnCampaignGenerated += MystStartingInventory;
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
            Events.OnEntityOffered -= ApplySyncItem;
            Events.OnCampaignGenerated -= SyncStartingInventory;
            Events.OnEntityOffered -= ApplyMystItem;
            Events.OnCampaignGenerated -= MystStartingInventory;
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
            keywords.Add(Extensions.CreateBasicKeyword(this, "sync", "Sync", "Gain an effect when a <sync> card is played or attacks elsewhere"));
            keywords.Add(Extensions.CreateBasicKeyword(this, "mystic", "Mystic", "Playable on another board|Reward: replace this effect with Zoomlin"));

            effects.Add(new StatusEffectDataBuilder(this)
                .Create<StatusEffectMystical>("Mystic")
                .WithCanBeBoosted(false)
                .WithText("<keyword=mhcdc9.wildfrost.sync.mystic>")
                .WithType("")
                .WithConstraints(Extensions.IsItem(), Extensions.IsPlay(),Extensions.NotOnSlot(), Extensions.TargetsBoard())
                .FreeModify<StatusEffectApplyX>(
                (data) =>
                {
                    data.effectToApply = Get<StatusEffectData>("Temporary Zoomlin");
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                })
                );

            effects.Add(new StatusEffectDataBuilder(this)
                .CreateTempTrait("Temporary Smackback", Get<TraitData>("Smackback"))
                .WithConstraints(Extensions.DoesAttack())
                );

            effects.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Attack", "<keyword=mhcdc9.wildfrost.sync.sync>: <+{a}><keyword=attack>", "", "Ongoing Increase Attack", boostable: true)
                .WithConstraints(Extensions.DoesDamage())
                );

            effects.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Effect", "<keyword=mhcdc9.wildfrost.sync.sync>: +{a} to effects", "", "Ongoing Increase Effects")
                .WithConstraints(Extensions.CanBeBoosted())
                );

            effects.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Frenzy", "<keyword=mhcdc9.wildfrost.sync.sync>: <x{a}><keyword=frenzy>", "", "MultiHit", boostable: true)
                .WithConstraints()
                );

            effects.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Barrage", "<keyword=mhcdc9.wildfrost.sync.sync>: <keyword=barrage>", "", "Temporary Barrage")
                .WithConstraints(Extensions.DoesAttack(), Extensions.NotTrait("Barrage"), Extensions.TargetsBoard())
                );

            effects.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Zoomlin", "<keyword=mhcdc9.wildfrost.sync.sync>: <keyword=zoomlin>", "", "Temporary Zoomlin")
                .WithConstraints(Extensions.NotTrait("Zoomlin"))
                );

            effects.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Smackback", "<keyword=mhcdc9.wildfrost.sync.sync>: <keyword=smackback>", "", "mhcdc9.wildfrost.sync.Temporary Smackback")
                .WithConstraints(Extensions.DoesAttack(), Extensions.NotTrait("Smackback"))
                );

            effects.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Heal", "<keyword=mhcdc9.wildfrost.sync.sync>: Restore <{a}><keyword=health>", "", "Heal", boostable: true, ongoing: false)
                .WithConstraints(Extensions.HasHealth())
                );

            effects.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Counter", "<keyword=mhcdc9.wildfrost.sync.sync>: Count down <keyword=counter> by <{a}>", "", "Reduce Counter", boostable: true, ongoing: false)
                .WithConstraints(Extensions.HasCounter())
                );

            effects.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Mystic", "<keyword=mhcdc9.wildfrost.sync.sync>: <keyword=mhcdc9.wildfrost.sync.mystic>", "", "mhcdc9.wildfrost.sync.Mystic")
                .WithConstraints(Extensions.IsPlay(), Extensions.NotOnSlot(), Extensions.TargetsBoard())
                );

            effects.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Nothing", "<keyword=mhcdc9.wildfrost.sync.sync>: Do nothing?", "", "", ongoing: false)
                .WithConstraints(Extensions.IsPlay())
                );
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
            var typeName = typeof(Y).Name;
            switch (typeName)
            {
                case nameof(StatusEffectData):
                    return effects.Cast<T>().ToList();
                case nameof(KeywordData):
                    return keywords.Cast<T>().ToList();
                default:
                    return null;
            }
        }

        public async Task SyncStartingInventory()
        {
            if (References.PlayerData?.inventory?.deck != null)
            {
                foreach (CardData data in References.PlayerData.inventory.deck)
                {
                    if (Dead.Random.Range(0f, 1f) < itemSyncChance && data.cardType.name == "Item")
                    {
                        Extensions.TryAddSync(data, ItemSyncEffects);
                    }
                }
            }
        }

        internal void ApplySyncItem(Entity entity)
        {
            if (entity.data.cardType.name == "Item")
            {
                Debug.Log($"[Sync] Entity Offered: {entity.data.title}");
                if (Dead.Random.Range(0f, 1f) < itemSyncChance)
                {
                    Extensions.TryAddSync(entity, ItemSyncEffects);
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

        public async Task MystStartingInventory()
        {
            if (References.PlayerData?.inventory?.deck != null)
            {
                foreach (CardData data in References.PlayerData.inventory.deck)
                {
                    if (Dead.Random.Range(0f, 1f) < itemMystChance && data.cardType.name == "Item")
                    {
                        Extensions.TryAddMyst(data);
                    }
                }
            }
        }

        internal void ApplyMystItem(Entity entity)
        {
            if (entity.data.cardType.name == "Item")
            {
                Debug.Log($"[Myst] Entity Offered: {entity.data.title}");
                if (Dead.Random.Range(0f, 1f) < itemMystChance)
                {
                    Extensions.TryAddMyst(entity);
                }
            }
        }

        internal static void SYNC_Handler(Friend friend, string message)
        {
            int combo = int.Parse(message);
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
            Net.SendMessageToAllOthers("SYNC", $"{syncCombo + 1}");
            //sentSyncMessage = true; 
        }
    }

    [HarmonyPatch(typeof(ScriptBattleSetUp), "SetUpEnemyWaves")]
    internal static class AddSyncToEnemies
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


        static void Postfix(Character enemy)
        {
            BattleWaveManager component = enemy.GetComponent<BattleWaveManager>();
            foreach (Wave wave in component.list)
            {
                int points = SyncMain.enemyPerWave;
                foreach (CardData data in wave.units.InRandomOrder())
                {
                    if (points > 0 && Extensions.TryAddSync(data, EnemySyncEffects))
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
                Net.SendMessage("SYNC", Net.self, "1");
            }

        }
    }
}
