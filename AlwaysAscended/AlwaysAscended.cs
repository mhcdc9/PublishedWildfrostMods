using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Targets;

namespace AlwaysAscended
{
    public class AlwaysAscended : WildfrostMod
    {
        public AlwaysAscended(string modDirectory) : base(modDirectory)
        {
        }
        public override string GUID => "mhcdc9.wildfrost.alwaysascended";

        public override string[] Depends => new string[0];

        public override string Title => "Always Ascended";

        public override string Description => "The allure of power was just too great for the heroic party.";

        [HarmonyPatch(typeof(FinalBossDeckGenerationSystem), "CheckResult", new Type[]
        {
            typeof(Campaign.Result)
        })]
        internal static class PatchTrueVictoryException
        {
            public static bool Prefix(ref bool __result, Campaign.Result result)
            {
                __result = (result == Campaign.Result.Win);
                return false;
            }
        }

        [HarmonyPatch(typeof(FinalBossDeckGenerationSystem), "CheckTrueWin", new Type[]
        {
            typeof(Campaign.Result)
        })]
        internal static class PatchCheckTrueWin
        {
            public static void Postfix(ref bool __result)
            {
                __result = false;
            }
        }
    }
}
