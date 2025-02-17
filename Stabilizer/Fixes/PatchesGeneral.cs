using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Deadpan.Enums.Engine.Components.Modding;
using FMOD;
using HarmonyLib;
using TMPro;
using UnityEngine;
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
        #endregion


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
                playerData.inventory.upgrades = playerData.inventory.upgrades.Where(x => x != null).ToList();
            }
        }
        

        //This is internal so that main can reference it.
        [HarmonyPatch(typeof(WildfrostMod.DebugLoggerTextWriter), nameof(WildfrostMod.DebugLoggerTextWriter.WriteLine))]
        internal class PatchHarmony
        {
            static bool Prefix() { Postfix(); return false; }
            static void Postfix() => HarmonyLib.Tools.Logger.ChannelFilter = HarmonyLib.Tools.Logger.LogChannel.Warn | HarmonyLib.Tools.Logger.LogChannel.Error;
        }

        [HarmonyPatch(typeof(Text), nameof(Text.ToKeyword))]
        class PatchKeywordText
        {
            static KeywordData Postfix(KeywordData __result)
            {
                return __result ?? Stabilizer.missingKeyword;
            }
        }


        #region ModAdded

        [HarmonyPatch]
        internal class PatchModAdded
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(InspectSystem), nameof(InspectSystem.CreatePopups))]
            static void OnInspect(InspectSystem __instance)
            {
                WildfrostMod mod = Stabilizer.Instance.Get<CardData>(__instance.inspect.data.name)?.ModAdded;
                if (mod != null)
                {
                    CreatePopup(mod, __instance.inspect, __instance.popUpPrefab, __instance.leftPopGroup, __instance.popups);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(CardInspector), nameof(CardInspector.CreatePopups))]
            internal static void OnCardInspect(CardInspector __instance, Entity inspect)
            {
                WildfrostMod mod = Stabilizer.Instance.Get<CardData>(inspect.data.name)?.ModAdded;
                if (mod != null)
                {
                    PatchModAdded.CreatePopup(mod, inspect, __instance.popUpPrefab, __instance.leftPopGroup, __instance.popups, true);
                }
            }

            static void CreatePopup(WildfrostMod mod, Entity inspect, CardPopUpPanel popUpPrefab, RectTransform leftPopGroup, List<Tooltip> popups, bool ignoreTimeScale = false)
            {
                CardPopUpPanel cardPopUpPanel = GameObject.Instantiate(popUpPrefab, leftPopGroup);
                cardPopUpPanel.ignoreTimeScale = ignoreTimeScale;
                cardPopUpPanel.gameObject.name = "Mod Added";
                popups.Add(cardPopUpPanel);
                cardPopUpPanel.Set("<size=0.2>Mod Added</size>", Color.white, $"<size=0.3>{mod.Title}</size>", new Color(1, 0.79f, 0.34f));
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(JournalCharm), nameof(JournalCharm.Hover))]
            static void OnJournalCharmHover(JournalCharm __instance)
            {
                WildfrostMod mod = __instance.upgradeData?.ModAdded;
                if (mod != null)
                {
                    if (!__instance.hover)
                    {
                        CardPopUp.AssignTo(__instance.rectTransform, __instance.popUpOffset.x, __instance.popUpOffset.y);
                    }
                    CardPopUp.AddPanel("Mod Added", "<color=#FFFFFF><size=0.2>Mod Added</size></color>", $"<color=#FFCA57><size=0.3>{mod.Title}</size></color>");
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(JournalCharm), nameof(JournalCharm.UnHover))]
            [HarmonyPatch(typeof(CardCharmInteraction), nameof(CardCharmInteraction.HideDescription))]
            static void OnJounalCharmUnHover()
            {
                CardPopUp.RemovePanel("Mod Added");
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(CardCharmInteraction), nameof(CardCharmInteraction.PopUpDescription))]
            static void OnCharmHover(CardCharmInteraction __instance)
            {
                WildfrostMod mod = Stabilizer.Instance.Get<CardUpgradeData>(__instance.upgradeDisplay?.data?.name ?? "")?.ModAdded;
                if (mod != null)
                {
                    CardPopUp.AddPanel("Mod Added", "<color=#FFFFFF><size=0.2>Mod Added</size></color>", $"<color=#FFCA57><size=0.3>{mod.Title}</size></color>");
                }
            }
        }

        [HarmonyPatch(typeof(JournalCharmManager), nameof(JournalCharmManager.LoadCharmData))]
        class PatchCharmConflict
        {
            static bool Prefix(ref List<KeyValuePair<string, CardUpgradeData>> __result)
            {
                __result = (from a in (from a in AddressableLoader.GetGroup<CardUpgradeData>("CardUpgradeData")
                                   where a.type == CardUpgradeData.Type.Charm && a.tier >= -2
                                   select a).ToDictionary((CardUpgradeData a) => (a?.ModAdded?.GUID ?? "!!!") + a.title, (CardUpgradeData a) => a)
                        orderby a.Value.tier >= 0 descending, a.Key
                        select a).ToList();

                return false;
            }
        }

        #endregion ModAdded


    }
}
