﻿using Deadpan.Enums.Engine.Components.Modding;
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

        private List<StatusEffectDataBuilder> effects = new List<StatusEffectDataBuilder>();
        private List<KeywordDataBuilder> keywords = new List<KeywordDataBuilder>();
        public SyncMain(string modDirectory) : base(modDirectory)
        {
        }

        public override string GUID => "mhcdc9.wildfrost.sync";

        public override string[] Depends => new string[0];

        public override string Title => "Sync Test";

        public override string Description => "Can you deal with a problem that spans two boards?";

        public CardData.StatusEffectStacks SStack(string name, int count) => new CardData.StatusEffectStacks(Get<StatusEffectData>(name), count);



        protected override void Load()
        {
            Instance = this;
            CreateStatuses();
            Events.OnBattlePreTurnStart += CheckSync;
            Events.OnEntityOffered += ApplySyncItem;
            Events.OnCampaignGenerated += SyncStartingInventory;
            Net.HandlerRoutines.Add("SYNC", SYNC_Handler);
            base.Load();
            CreateModifierData();
        }

        protected override void Unload()
        {
            Events.OnBattlePreTurnStart -= CheckSync;
            Events.OnEntityOffered -= ApplySyncItem;
            Events.OnCampaignGenerated -= SyncStartingInventory;
            Net.HandlerRoutines.Remove("SYNC");
            base.Unload();
        }

        public void CreateModifierData()
        {
            GameModifierDataBuilder syncModifier = new GameModifierDataBuilder()
                .Create("SyncModifier");
        }

        public void CreateStatuses()
        {
            keywords.Add(Extensions.CreateBasicKeyword(this, "sync", "Sync", "Gain an effect as long as conditions are met..."));

            effects.Add( new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Attack", "<keyword=mhcdc9.wildfrost.sync.sync>: <+{a}><keyword=attack>", "", Get<StatusEffectData>("Ongoing Increase Attack"))
                .WithConstraints(Extensions.DoesDamage())
                );

            effects.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Effect", "<keyword=mhcdc9.wildfrost.sync.sync>: +{a} to effects", "", Get<StatusEffectData>("Ongoing Increase Effects"))
                .WithConstraints(Extensions.CanBeBoosted())
                );

            effects.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Frenzy", "<keyword=mhcdc9.wildfrost.sync.sync>: <+{a}><keyword=frenzy>", "", Get<StatusEffectData>("MultiHit"))
                .WithConstraints()   
                );

            effects.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Barrage", "<keyword=mhcdc9.wildfrost.sync.sync>: <keyword=barrage>", "", Get<StatusEffectData>("Temporary Barrage"))
                .WithConstraints(Extensions.DoesAttack())
                );

            effects.Add( new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Heal", "<keyword=mhcdc9.wildfrost.sync.sync>: Restore <{a}><keyword=health>", "", Get<StatusEffectData>("Heal"), ongoing:false)
                .WithConstraints(Extensions.HasHealth())
                );

            effects.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Counter", "<keyword=mhcdc9.wildfrost.sync.sync>: Count down <keyword=counter> by <{a}>", "", Get<StatusEffectData>("Reduce Counter"), ongoing: false)
                .WithConstraints(Extensions.HasCounter())
                );

            effects.Add(new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Nothing", "<keyword=mhcdc9.wildfrost.sync.sync>: Do nothing?", "", null, ongoing:false)
                );
        }

        static (string, int)[] ItemSyncEffects = new (string, int)[]
        {
            ("Sync Attack", 1),
            ("Sync Attack", 2),
            ("Sync Effect", 1),
            ("Sync Frenzy", 1),
            ("Sync Barrage", 1),
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
                if (Dead.Random.Range(0f,1f) < itemSyncChance)
                {
                    Extensions.TryAddSync(entity, ItemSyncEffects);
                }
            }
        }

        /*internal void ApplySync()
        {
            List<Entity> enemies = Battle.GetCards(Battle.instance.enemy);
            foreach (Entity e in enemies)
            {
                if (Dead.Random.Range(0f, 1f) < enemySyncChance)
                {
                    TryAddSyncEffectEnemy(e);
                }
            }
        }*/

        /*
        internal void TryAddSyncEffectEnemy(Entity e)
        {
            if (e == null)
                return;
            foreach ((string, int) stack in EnemySyncEffects.InRandomOrder())
            {
                StatusEffectData effect = Get<StatusEffectData>(stack.Item1).InstantiateKeepName();
                if (effect.CanPlayOn(e))
                {
                    effect.Apply(stack.Item2, e, null);
                    StatusEffectSystem.activeEffects.Add(effect);
                    e.display.promptUpdateDescription = true;
                    e.PromptUpdate();
                    break;
                }
            }
        }
        */

        internal static void CheckSync(int turnCount)
        {
            sentSyncMessage = false;
            UnityEngine.Debug.Log($"[Sync] You can sync again.");
            PerformSync();
        }

        internal static void SYNC_Handler(Friend friend, string message)
        {
            int combo = int.Parse(message);
            if ( combo != 0)
            {
                syncNextTurn = Math.Max(syncNextTurn + 1, combo);
            }
            else
            {

                syncNextTurn = 0;
            }
            if (Battle.instance != null && Battle.instance.phase == Battle.Phase.Play)
            {
                PerformSync();
            }
        }

        internal static void PerformSync()
        {
            if (sync && syncNextTurn == 0)
            {
                sync = false;
                ActionSync action = new ActionSync(0);
                ActionQueue.Add(action);
            }
            if (syncNextTurn > 0)
            {
                sync = true;
                ActionSync action = new ActionSync(syncNextTurn);
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
            ("Sync Counter", 1)
        };


        static void Postfix(Character enemy)
        {
            BattleWaveManager component = enemy.GetComponent<BattleWaveManager>();
            foreach(Wave wave in component.list)
            {
                int points = SyncMain.enemyPerWave;
                foreach(CardData data in wave.units.InRandomOrder())
                {
                    if(points > 0 && Extensions.TryAddSync(data,EnemySyncEffects))
                    {
                        points--;
                    }
                }
            }
        }




    }
}
