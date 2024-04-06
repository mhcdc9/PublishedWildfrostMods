using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static CampaignGenerator;
using static MapPath;
using static Text;

namespace TestMod
{
    internal class Test : WildfrostMod
    {
        public Test(string modDirectory) : base(modDirectory)
        {
            CreateModAssets();
        }

        public override string GUID => "mhcdc9.wildfrost.test";

        public override string[] Depends => new string[0];

        public override string Title => "Test Mod";

        public override string Description => "The Snow Knight learns battle tactics overnight.";

        private bool preLoaded = false;

        private GameObject button;

        private List<StatusEffectDataBuilder> effects;
        private void CreateModAssets()
        {
            effects = new List<StatusEffectDataBuilder>();

            effects.Add(
                new StatusEffectDataBuilder(this)
                .Create<StatusEffectXActsLikeShell>("Snow Acts Like Shell For Allies")
                .WithType("")
                .WithText("<keyword=snow> acts like <keyword=shell> on self and allies")
                .WithCanBeBoosted(false)
                .FreeModify(delegate (StatusEffectXActsLikeShell data)
                {
                    data.targetType = "snow";
                    data.imagePath = this.ImagePath("Shnell.png");
                })
                );

            string[] a = ((ScriptUpgradeMinibosses)(Get<GameModifierData>("BossesHaveCharms")
                .startScripts[0])).profiles[5].cardDataNames.Append("SplitBoss1").ToArray();

            a = a.Append("SplitBoss2").ToArray();

            ((ScriptUpgradeMinibosses)(Get<GameModifierData>("BossesHaveCharms")
                .startScripts[0])).profiles[5].cardDataNames = a;

            preLoaded = true;
            CardData card = null;
        }

        public override void Load()
        {
            //if (!preLoaded) { CreateModAssets(); }
            Events.OnCardDataCreated += Shnell;
            base.Load();
            //button = DefaultControls.CreateButton(new DefaultControls.Resources());
            GameObject gameObject = CardManager.cardIcons["lumin"];
            Debug.Log($"[Test] Found Lumin.");
            gameObject.AddComponent<ButtonHelper>();
            gameObject.GetComponent<ButtonHelper>().targetGraphic = gameObject.GetComponent<Image>();

        }

        [HarmonyPatch(typeof(Text), "ProcessTag", new Type[]
        {
            typeof(string),
            typeof(string),
            typeof(int),
            typeof(float),
            typeof(Text.ColourProfileHex)
        })]
        internal static class PreprocessTags
        {
            internal static void Prefix(ref string __result, string text, ref string tag, int effectBonus, float effectFactor, ColourProfileHex profile)
            {
                string[] array = tag.Split('=');
                Debug.Log("[Patch] My patch has been acknowledged: " + array[0]);
                if (array[0].Trim() == "patch")
                {
                    tag = "<sprite name=snow>";
                }
            }
        }



        public override void Unload()
        {
            Events.OnCardDataCreated -= Shnell;
            base.Unload();
        }


        private void SceneLoaded(Scene scene)
        {
            if (scene.name != "Campaign")
                return;

            CombineCardSystem.Combo combo = new CombineCardSystem.Combo
            {
                cardNames = new string[3] { "Woodhead", "Junk", "ScrapPile" },
                resultingCardName = "Junkhead"
            };

            CombineCardSystem combineCardSystem = GameObject.FindObjectOfType<CombineCardSystem>();
            combineCardSystem.enabled = true;
            combineCardSystem.combos = combineCardSystem.combos.AddItem(combo).ToArray(); //Note: combos is a private variable.
        }

        private void Shnell(CardData data)
        {
            if (data.name == "SnowKnight")
            {
                data.startWithEffects = data.startWithEffects.AddItem(new CardData.StatusEffectStacks(Get<StatusEffectData>("Snow Acts Like Shell For Allies"), 1)).ToArray();
            }
        }

        public override List<T> AddAssets<T, Y>()
        {
            var typeName = typeof(Y).Name;
            switch (typeName)
            {
                case nameof(StatusEffectData):
                    return effects.Cast<T>().ToList();
                default:
                    return null;
            }
        }
    }
}
