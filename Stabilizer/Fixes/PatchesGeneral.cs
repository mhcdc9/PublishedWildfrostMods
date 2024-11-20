using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Deadpan.Enums.Engine.Components.Modding;
using FMOD;
using HarmonyLib;
using D = UnityEngine.Debug;

namespace Stabilizer.Fixes
{
    internal static class PatchesGeneral
    {
        internal static T Get<T>(string name) where T : DataFile
        {
            return Stabilizer.Instance.Get<T>(name);
        }
        internal static void Log(string s) => D.Log($"[Stabilizer] {s}");
        internal static void Wrn(string s) => D.LogError($"[Stabilizer] {s}");

        //Fixes various journal stuff and tribe flags correctly show on inspect.
        [HarmonyPatch(typeof(References), nameof(References.Classes), MethodType.Getter)]
        class FixClassesGetter
        {
            static void Postfix(ref ClassData[] __result) => __result = AddressableLoader.GetGroup<ClassData>("ClassData").ToArray();
        }

        #region TargetConstraintPatches
        [HarmonyPatch()]
        class SkipStatusCheck
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                return typeof(TargetConstraintHasStatus).GetMethods().Where(m => m.Name == "Check");
            }
            static bool Prefix(TargetConstraintHasStatus __instance, ref bool __result)
            {
                if (__instance.status == null)
                {
                    Wrn("Missing Status on Target Constraint!");
                    __result = true;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch()]
        class SkipTraitCheck
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                return typeof(TargetConstraintHasTrait).GetMethods().Where(m => m.Name == "Check");
            }

            static bool Prefix(TargetConstraintHasTrait __instance, ref bool __result)
            {
                if (__instance.trait == null)
                {
                    Wrn("Missing Trait on Target Constraint!");
                    __result = true;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch()]
        class SkipCardCheck
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                return typeof(TargetConstraintIsSpecificCard).GetMethods().Where(m => m.Name == "Check");
            }

            static void Prefix(TargetConstraintIsSpecificCard __instance, ref bool __result)
            {
                __instance.allowedCards = __instance.allowedCards.Where(x => x != null).ToArray();
            }
        }

        [HarmonyPatch(typeof(OverallStatsSystem), "CampaignEnd")]
        class PreventEndRunCrash
        {
            static ClassData dummyTribe;
            static void Prefix(ref PlayerData playerData)
            {
                if (playerData.classData?.id == null)
                {
                    if (dummyTribe == null)
                    {
                        dummyTribe = Get<ClassData>("Basic").InstantiateKeepName();
                        dummyTribe.id = "???";
                    }
                    playerData.classData = dummyTribe;
                }
            }
        }
        #endregion


        [HarmonyPatch(typeof(WildfrostMod.DebugLoggerTextWriter), nameof(WildfrostMod.DebugLoggerTextWriter.WriteLine))]
        internal class PatchHarmony
        {
            static bool Prefix() { Postfix(); return false; }
            static void Postfix() => HarmonyLib.Tools.Logger.ChannelFilter = HarmonyLib.Tools.Logger.LogChannel.Warn | HarmonyLib.Tools.Logger.LogChannel.Error;
        }


    }
}
