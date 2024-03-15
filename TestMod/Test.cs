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
using static CampaignGenerator;
using static MapPath;
using static Text;

namespace TestMod
{
    internal class Test : WildfrostMod
    {
        public Test(string modDirectory) : base(modDirectory)
        {
        }

        public override string GUID => "mhcdc9.wildfrost.test";

        public override string[] Depends => new string[0];

        public override string Title => "Test Mod";

        public override string Description => "The Snow Knight learns battle tactics overnight.";

        private bool preLoaded = false;

        private string[] dord = { "Hmm" };

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
                .FreeModify(delegate (StatusEffectData data)
                {
                    ((StatusEffectXActsLikeShell)data).targetType = "snow";
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
            if (!preLoaded) { CreateModAssets(); }
            Events.OnCardDataCreated += Shnell;
            Events.OnSceneLoaded += SceneLoaded;
            Events.OnCampaignGenerated += CampaignGenerated;
            NoTargetTextSystem.instance.shakeDurationRange = new Vector2(0.1f, 0.13f);
            base.Load();
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
            Events.OnSceneLoaded -= SceneLoaded;
            Events.OnCampaignGenerated -= CampaignGenerated;
            base.Unload();
        }

        private async Task CampaignGenerated()
        {
            Debug.Log("Task Started");
            for (int i=0; i< References.Campaign.nodes.Count; i++)
            {
                CampaignNode node = References.Campaign.nodes[i];
                if (node.tier <= 2 && node.type.name == "CampaignNodeItem")
                {
                    Debug.Log("Treasure Found!");
                    SaveCollection<string> collection = (SaveCollection<string>) node.data["cards"];
                    collection.Add("Yuki");
                    for(int j=0; j<collection.Count; j++)
                    {
                        Debug.Log(collection[j]);
                    }
                    Debug.Log("Yuki Added.");
                    node.data["cards"] = collection;
                }
            }
            Debug.Log("Task Done");
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
