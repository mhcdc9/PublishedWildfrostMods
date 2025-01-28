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

namespace Detours
{
    public class DetourMain : WildfrostMod
    {
        [ConfigItem(0.7f, "", "Detour Chance")]
        public float detourChance = 0.7f;

        [ConfigItem(true, "", "Repeat Detours")]
        public bool replenish = true;

        [ConfigItem(false, "", "Include Examples")]
        public bool addTestDetours = false;

        [ConfigItem(3, "", "# of Storylines")]
        public int storylines = 3;
        public override string GUID => "mhcdc9.wildfrost.detours";

        public override string[] Depends => new string[0];

        public override string Title => "Detours";

        public override string Description => "Add a framework for populating and creating \"detours\": smaller events that can occur before map events.";

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
            AddDetours();
            base.Load();
        }

        public static DetourPack examples;
        public static Storyline exampleStory;
        public void AddDetours()
        {
            if (examples == null)
            {
                examples = new DetourPack(this, "Proof Of Concepts")
                {
                    new IllusionOfChoice("Illusion Of Choice", this),
                    new CraneMachine("Skill Crane", this),
                    new AlienSpiders("Giant Ice Spiders", this),
                    new HiLowGame("Quiz Show", this),
                    new CardTrader("Card Trader", this)
                };
                exampleStory = new MokoStoryline(this, "The Path of the Moko");
            }
            examples.Register();
            exampleStory.Register();
        }

        public override void Unload()
        {
            Events.OnSceneLoaded -= SceneLoaded;
            Events.OnCampaignGenerated -= InsertDetours;
            Events.OnCampaignLoaded -= CampaignLoaded;
            Events.OnModLoaded -= Commands.CheckAnotherConsoleMod;
            base.Unload();
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
