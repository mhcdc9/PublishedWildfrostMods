using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using Detours.Examples;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using MonoMod.RuntimeDetour.Platforms;
using Detours.Misc;
using WildfrostHopeMod;
using WildfrostHopeMod.Configs;

namespace Detours
{
    public class DetourMain : WildfrostMod
    {
        [ConfigManagerTitle("Detour Frequency")]
        [ConfigManagerDesc("Determines how many generic detours you see in a run. Setting this config to 0 will remove all detours.")]
        [ConfigSlider(0f,1f)]
        [ConfigItem(0.3f, "", "Detour Chance")]
        public float detourChance = 0.3f;

        [ConfigManagerTitle("Repeat Detours?")]
        [ConfigManagerDesc("Determines whether generic detours can repeat multiple times in a run.")]
        [ConfigItem(false, "", "Repeat Detours")]
        public bool replenish = false;

        [ConfigManagerTitle("Include Proof-of-Concepts")]
        [ConfigManagerDesc("Determines if the proof-of concept detours are added to the pool (Warning: not recommended for a normal run)")]
        [ConfigItem(false, "", "Include Examples")]
        public bool addTestDetours = false;

        [ConfigManagerTitle("# of Storylines")]
        [ConfigManagerDesc("Determines the maximum amount of storylines (a curated sequence of detours) that can be added in a single run.")]
        [ConfigItem(3, "", "# of Storylines")]
        public int storylines = 3;
        public override string GUID => "mhcdc9.wildfrost.detours";

        public override string[] Depends => new string[0];

        public override string Title => "Detour Framework";

        public override string Description => "Inspired by Faster Than Light/Slay The Spire events, detours are small events that occur before a proper map event. These detours can be a group of small encounters or an overarching storyline. \n\n" +
            "This mod is primarily a framework, but does include a couple of test detours (including FTL's giant alien spiders). To access them, turn on \"Include Proof-of-Concepts\" in the mod configs and start a new run. \n\n" +
            "If you have the Another Console mod active, there are two new commands to help find and test detours: [detour start] and [detour frame]. \n\n" +
            "For more information, please contact the developer on Steam or Discord (@Michael C).";

        public static DetourMain instance;

        public CardData.StatusEffectStacks SStack(string name, int amount) => new CardData.StatusEffectStacks(TryGet<StatusEffectData>(name), amount);
        internal T TryGet<T>(string name) where T : DataFile
        {
            T data;
            if (typeof(StatusEffectData).IsAssignableFrom(typeof(T)))
                data = base.Get<StatusEffectData>(name) as T;
            else
                data = base.Get<T>(name);

            if (data == null)
                throw new Exception($"TryGet Error: Could not find a [{typeof(T).Name}] with the name [{name}] or [{Extensions.PrefixGUID(name, this)}]");

            return data;
        }

        public DetourMain(string modDirectory) : base(modDirectory) { }

        public override void Load()
        {
            instance = this;
            Commands.FindAnotherConsoleMod();
            Events.OnSceneLoaded += SceneLoaded;
            Events.OnCampaignGenerated += InsertDetours;
            Events.OnCampaignLoaded += CampaignLoaded;
            Events.OnModLoaded += Commands.CheckAnotherConsoleMod;
            ConfigManager.GetConfigSection(this).OnConfigChanged += ConfigChanged;
            PackHelper.Initialize(this);
            base.Load();
        }

        //Holds the detours and storylines in a way irrespecitve of reference order (unnecessart for this mod; essential for all others)
        internal class PackHelper
        {
            public static PackHelper instance;

            public WildfrostMod mod;
            public DetourPack examples;
            public Storyline exampleStory;
            
            public PackHelper(DetourMain mod)
            {
                this.mod = mod;
            }

            public static void Initialize(DetourMain mod)
            {
                if (instance == null)
                {
                    instance = new PackHelper(mod);
                }
                if (mod.addTestDetours)
                {
                    instance.AddDetours();
                }
            }

            public void AddDetours()
            {
                if (examples == null)
                {
                    examples = new DetourPack(mod, "Proof Of Concepts")
                {
                    new IllusionOfChoice("Illusion Of Choice", mod),
                    new CraneMachine("Skill Crane", mod),
                    new AlienSpiders("Giant Ice Spiders", mod),
                    new HiLowGame("Quiz Show", mod),
                    new CardTrader("Card Trader", mod)
                };
                    exampleStory = new MokoStoryline(mod, "The Path of the Moko");
                }
                examples.Register();
                exampleStory.Register();
            }

            public void RemoveDetours()
            {
                examples.Unregister();
                exampleStory.Unregister();
            }
        }
        

        public override void Unload()
        {
            Events.OnSceneLoaded -= SceneLoaded;
            Events.OnCampaignGenerated -= InsertDetours;
            Events.OnCampaignLoaded -= CampaignLoaded;
            Events.OnModLoaded -= Commands.CheckAnotherConsoleMod;
            ConfigManager.GetConfigSection(this).OnConfigChanged -= ConfigChanged;
            base.Unload();
        }

        private void ConfigChanged(ConfigItem item, object value)
        {
            if (item.fieldName == "addTestDetours")
            {
                bool b = (bool)value;
                if (b)
                {
                    PackHelper.instance.AddDetours();
                }
                else
                {
                    PackHelper.instance.RemoveDetours();
                }
            }
        }

        private void CampaignLoaded()
        {
            if (SceneManager.IsLoaded("UI"))
            {
                Storyline._storyNode = Campaign.instance.nodes[0];
            }
        }

        private Task InsertDetours()
        {
            IEnumerable<CampaignNode> nodes = Campaign.instance.nodes.Where((n) => n.type.canEnter).InRandomOrder();
            int count = (int)(nodes.Count() * detourChance);
            DetourSystem.Populate();
            DetourSystem.SelectStorylines();
            List<Detour> detours = DetourSystem.activeEvents.Clone(); 
            Detour detour;
            foreach (CampaignNode node in nodes.Take(count))
            {
                Debug.Log($"[Detours] {node.id}: {node.type.name}");
                detour = detours.FirstOrDefault(d => (d.allowedBeforeBattle || !node.type.isBattle) && d.CheckAllowed(node));
                if (detour != null)
                {
                    node.data.Add(DetourSystem.detourTitle, detour.QualifiedName);
                    detour.Setup(node);
                    Debug.Log($"[Detour] Assigned detour [{detour.name}]");
                    detours.Remove(detour);
                }
                else if (replenish)
                {
                    detours.AddRange(DetourSystem.activeEvents.Clone());
                }
            }
            return Task.CompletedTask;
        }

        private void SceneLoaded(Scene scene)
        {
            if (scene.name == "UI")
            {
                DetourHolder.CreateInstance();
            }
        }



        [HarmonyPatch(typeof(Campaign), "EnterNode", new Type[] {typeof(CampaignNode), typeof(bool)})]
        class PatchEvents
        {
            public static IEnumerator Postfix(IEnumerator __result, CampaignNode node)
            {
                
                Debug.Log("[Small Events] Pre Node Run");
                if (!Transition.Running && DetourSystem.HasActiveStoryline(node))
                {
                    yield return Sequences.Wait(0.5f);
                    yield return DetourSystem.StartStoryline(node);
                }
                else if (!Transition.Running && DetourSystem.HasActiveDetour(node))
                {
                    yield return Sequences.Wait(0.5f);
                    yield return DetourSystem.StartDetour(node);
                }
                if (!DetourHolder.skip)
                {
                    yield return __result;
                }
                else
                {
                    yield return Sequences.Wait(0.25f);
                    node.SetCleared();
                    References.Map.Continue();
                }
                DetourHolder.skip = false;
            }
        }
    }
}
