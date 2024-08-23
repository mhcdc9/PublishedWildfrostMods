using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization;

namespace Backflip
{
    public class Backflip : WildfrostMod
    {
        public static AnimationCurve rotateCurve;
        public static AnimationCurve jumpCurve;

        public static Dictionary<ulong, int> flipCounter = new Dictionary<ulong, int>();

        public override string GUID => "mhcdc9.wildfrost.backflip";

        public override string[] Depends => new string[0];

        public override string Title => "The Backflip Mod";

        public override string Description => "This mod amplifies the coolness of all cards by letting them perform backflips after they defeated their enemies. " +
            "Instead of the usual \"No Target To Attack\" animation, cards will perform a backflip instead. " +
            "Eack card has their own backflip counter so you can tell who is leading the team in style. \n\n" +
            "Credits to KDeveloper for volunteering to refine the backflip animation.";

        public Backflip(string modDirectory) : base(modDirectory) { }

        private void MakeBackflipCurves()
        {
            rotateCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.1f, 0f),
                new Keyframe(0.2f, 45f),
                new Keyframe(0.3f, 90f),
                new Keyframe(0.4f, 135f),
                new Keyframe(0.5f, 180f),
                new Keyframe(0.6f, 225f),
                new Keyframe(0.7f, 270f),
                new Keyframe(0.8f, 315f),
                new Keyframe(0.9f, 360f),
                new Keyframe(1f, 360f));
            rotateCurve.keys[0].outTangent = (float)-Math.Tan(Math.PI / 6);
            rotateCurve.keys[10].inTangent = (float)-Math.Tan(Math.PI / 6);

            jumpCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.25f, 0.707f),
                new Keyframe(0.5f, 1f),
                new Keyframe(0.75f, 0.707f),
                new Keyframe(1f, 0f));
            jumpCurve.keys[0].outTangent = (float)Math.Tan(Math.PI / 3);
            jumpCurve.keys[1].outTangent = (float)Math.Tan(Math.PI / 6);
            jumpCurve.keys[3].inTangent = (float)Math.Tan(Math.PI / 6);
            jumpCurve.keys[4].inTangent = (float)Math.Tan(Math.PI / 3);
        }



        private void ResetFlips(Campaign.Result _, CampaignStats __, PlayerData ___)
        {
            flipCounter.Clear();
        }

        public override void Load()
        {
            MakeBackflipCurves();
            base.Load();
            Events.OnCampaignEnd += ResetFlips;
        }

        public override void Unload()
        {
            base.Unload();
            Events.OnCampaignEnd -= ResetFlips;
        }

        [HarmonyPatch(typeof(NoTargetTextSystem), "_Run", new Type[]
        {
            typeof(Entity),
            typeof(NoTargetType),
            typeof(object[]),
        })]
        class PatchNoTargetDance
        {
            internal static Vector3 jumpOffset = new Vector3(0, 2f, 0);
            internal static Vector3 flipOffset = new Vector3(0, 0, 1f);
            internal static float duration = 0.6667f;

            static IEnumerator Etcetera(NoTargetTextSystem __instance, Entity entity, string s)
            {
                yield return Sequences.WaitForAnimationEnd(entity);
                int direction = (entity?.owner == References.Player) ? 1 : -1;
                TMP_Text textElement = Traverse.Create(__instance).Field("textElement").GetValue<TMP_Text>();
                entity.curveAnimator.Move(jumpOffset, Backflip.jumpCurve, 0f, duration);
                entity.curveAnimator.Rotate(direction * flipOffset, Backflip.rotateCurve, duration);
                textElement.text = s;
                Traverse.Create(__instance).Method("PopText", new Type[1] { typeof(Vector3) }, new object[1] { entity.transform.position }).GetValue();
                yield return new WaitForSeconds(0.4f);
            }

            static bool Prefix(ref IEnumerator __result, NoTargetTextSystem __instance, ref Vector2 ___shakeDurationRange, ref Vector2 ___shakeAmount, Entity entity)
            {
                if (Backflip.flipCounter.TryGetValue(entity.data.id, out int count))
                {
                    count++;
                }
                else
                {
                    count = 1;
                }
                __result = Etcetera(__instance, entity, $"Flips: {count}");
                Backflip.flipCounter[entity.data.id] = count;
                StatsSystem.instance.stats.Add("backflips", 1);
                return false;
            }
        }

        [HarmonyPatch(typeof(StatsPanel), "Awake")]
        class PatchInBackflipStat
        {
            public static string BackflipKey = "mhcdc9.wildfrost.backflip.stat_desc";
            public static int priority = 1;

            static void Prefix(StatsPanel __instance)
            {
                GameStatData stat = ScriptableObject.CreateInstance<GameStatData>();
                stat.name = "Backflips";
                stat.type = GameStatData.Type.Count;
                stat.statName = "backflips";
                stat.priority = priority;

                StringTable collection = LocalizationHelper.GetCollection("UI Text", new LocaleIdentifier(SystemLanguage.English));
                collection.SetString(BackflipKey, "Backflips Performed: {0}");
                stat.stringKey = collection.GetString(BackflipKey);

                __instance.stats = __instance.stats.AddItem(stat).ToArray();
            }

            static void Postfix(StatsPanel __instance)
            {
                foreach(GameStatData data in __instance.stats)
                {
                    Debug.Log($"{data.name}: {data.priority}");
                }
            }
        }
    }
}
