using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D = UnityEngine.Debug;

namespace Stabilizer.Fixes
{
    internal static class UtilMethods
    {
        
        internal static void Log(string s) => D.Log($"[Stabilizer] {s}");
        internal static void Err(string s) => D.LogError($"[Stabilizer] {s}");

        internal static T Get<T>(string name) where T : DataFile
        {
            return Stabilizer.Instance.Get<T>(name);
        }

        //Fixes missing images if leader cards aren't scriptable images.
        internal static void FixImage(Entity entity)
        {
            if (entity.display is Card card && !card.hasScriptableImage)
            {
                card.mainImage.gameObject.SetActive(true);
            }
        }

        //When leaving the mod menu, clean is called if any mod has been unloaded
        internal static void Clean()
        {
            CleanGameModesAndRewardPools();
            CleanTraitOverrides();
            CleanBossSwaps();

        }

        internal static void CleanTraitOverrides()
        {
            List<TraitData> list = AddressableLoader.GetGroup<TraitData>("TraitData");
            Log(list.Count.ToString());
            list.Do(t =>
            {
                t.overrides = RemoveNulls(t.overrides);
            });
            Log("Cleaned [Trait Overrides]");
        }

        internal static void CleanGameModesAndRewardPools()
        {
            GameMode gameMode = Get<GameMode>("GameModeNormal");
            gameMode.classes = RemoveNulls(gameMode.classes);
            foreach (ClassData tribe in gameMode.classes)
            {
                tribe.rewardPools.Do(r => r.list.RemoveAllWhere(x => x == null));
            }
            Log("Cleaned [Reward Pools]");
        }

        internal static void CleanBossSwaps()
        {
            BattleData data = Get<BattleData>("Final Boss");
            if (data.generationScript is BattleGenerationScriptFinalBoss script)
            {
                //ReplaceCards
                FinalBossGenerationSettings settings = script.settings;
                settings.replaceCards = settings.replaceCards.Where(
                rc =>
                {
                    bool flag1 = (rc.card != null);
                    rc.options = RemoveNulls(rc.options);
                    bool flag2 = (rc.options.Length > 0);
                    return flag1 && flag2;
                }).ToArray();
                Log("Cleaned [ReplaceCards]");

                //IgnoreUpgrades
                settings.ignoreUpgrades = RemoveNulls(settings.ignoreUpgrades);
                Log("Cleaned [IgnoreUpgrades]");

                //IgnoreTraits
                settings.ignoreTraits = RemoveNulls(settings.ignoreTraits);
                Log("Cleaned [IgnoreTraits]");

                //EffectSwapper
                settings.effectSwappers = settings.effectSwappers.Where(
                    es =>
                    {
                        bool flag1 = (es.effect != null);
                        es.replaceWithOptions = RemoveNulls(es.replaceWithOptions);
                        bool flag2 = (es.replaceWithOptions.Length > 0);
                        return flag1 && (flag2 || es.replaceWithAttackEffect != null);
                    }).ToArray();
                Log("Cleaned [EffectSwappers]");

                //CardModifiers
                settings.cardModifiers = settings.cardModifiers.Where(cm => cm.card != null).ToArray();
                Log("Cleaned [CardModifiers]");

                //EnemyOptions
                settings.enemyOptions = settings.enemyOptions.Where(
                    eo =>
                    {
                        bool flag1 = (eo.enemy != null);
                        eo.fromCards = RemoveNulls(eo.fromCards);
                        bool flag2 = (eo.fromCards.Length > 0);
                        return flag1 && flag2;
                    }).ToArray();
                Log("Cleaned [EnemyOptions]");
            }
        }

        internal static T[] RemoveNulls<T>(T[] array) where T:DataFile
        {
            return array.Where(x => x != null).ToArray();
        }
    }


}
