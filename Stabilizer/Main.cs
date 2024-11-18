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
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Stabilizer.TileView;
using Stabilizer.Fixes;
using Patches = Stabilizer.Fixes.Patches;
using System.IO;
using Stabilizer.Compatibility;
using System.Reflection;
using ConfigItemAttribute = Deadpan.Enums.Engine.Components.Modding.WildfrostMod.ConfigItemAttribute;

namespace Stabilizer
{
    public class Stabilizer : WildfrostMod
    {
        /*
        Canvas/SafeArea/Menu/Panel/Positioner/Scroll View/Viewport/Content/
        Mod
        (RectTransform)
        (ModHolder)
        > Icon
        > Background
        > Text
        > Buttons
          (Horizontal Layout Group)
        >> LoadToggle
        >> OpenFolder
        >> PublishToWorkshop

        Buttons
        > Animator
        >> Button
           (Button)
           (EventTrigger)

        SmoothScrollRect on ScrollView
        */

        //Canvas/Safe Area/Menu/ButtonLayout/ModsButton/Animator/Button
        public override string GUID => "!mhcdc9.wildfrost.stabilizer";

        public override string[] Depends => new string[0];

        public override string Title => "Mod Stabilizer v0.7";

        public override string Description => "[WIP] This mod enhances the experience of playing and dealing with multiple mods. Curently, it replaces the mod menu with an optional tile view with a search bar and a marker system." +
            " It also runs various procedures so that most mods no longer require a full restart after unloading them. Future plans is to add an infrastructure to allow mods to better interact with each other (or at least not conflict)." +
            " Bugs and suggestion may be sent to @Michael C on the Wildfrost Discord. Enjoy!\n\n\n" +
            "Current Features:\n" +
            "- Mod Tile View\n" +
            "- Reward pool cleaning\n" +
            "- Final boss swapper cleaning\n" +
            "- Various once-of patches" +
            "- Failsafes on TargetConstraints that check for cards, statuses, or traits" +
            "- Mod Inspect Customization (See the modding forum post: Mod Project: Crossover & Compatibility for more details)\n\n" +
            "Planned Featurs:\n" +
            "- Allow modders to define the archetypes of their moddeed tribe\n" +
            "- Allow other modders to place cards in other modded tribes (adhering to the archetypes given above)\n" +
            "- Deal with CampaignNodeType conflicts if they become an issue\n" +
            "- Whatever else is suggested, I guess.";


        public static Stabilizer Instance;

        internal static GameObject prefabHolder;

        public static bool NeedsCleaning = false;

        [ConfigItem(true, comment: "", forceTitle: "StartAsTileView")]
        public bool startAsTileView = true;

        [ConfigItem(6, comment: "", forceTitle: "TilesPerRow")]
        public int tilesPerRow = 5;
        public Stabilizer(string modDirectory) : base(modDirectory) 
        {
            Instance = this;

            HarmonyInstance.PatchAll(typeof(Patches.PatchHarmony));

        }


        public override void Load()
        {
            base.Load();

            prefabHolder = new GameObject(GUID);
            GameObject.DontDestroyOnLoad(prefabHolder);
            prefabHolder.SetActive(false);

            if (MarkerManager.markers.Count == 0)
            {
                MarkerManager.LoadDictionary();
            }

            Bootstrap.Mods.Where(m => m.HasLoaded).Do(m => Frisk(m));
            Events.OnModLoaded += Frisk;
            Events.OnModUnloaded += Defrisk;

            Events.OnEntityCreated += UtilMethods.FixImage;
            Events.OnModUnloaded += PrepareCleaning;
            Events.OnSceneLoaded += SceneLoaded;
            Events.OnSceneUnload += SceneUnloaded;
        }

        public override void Unload()
        {
            base.Unload();

            Events.OnModLoaded -= Frisk;
            Events.OnModUnloaded -= Defrisk;

            Events.OnEntityCreated -= UtilMethods.FixImage;
            Events.OnModUnloaded -= PrepareCleaning;
            Events.OnSceneLoaded -= SceneLoaded;
            Events.OnSceneUnload -= SceneUnloaded;
        }

        Dictionary<WildfrostMod, Delegate> OnModInspectDelegates = new Dictionary<WildfrostMod, Delegate>();
        internal void Frisk(WildfrostMod mod)
        {
            Type type = CompFinder.FindCompClass(mod);
            MethodInfo method = CompFinder.FindCompMethod(mod, "Event_OnModInspect");
            if (method != null)
            {
                EventInfo ev = typeof(StabilizerEvents).GetEvent("OnModInspect");
                Delegate d = Delegate.CreateDelegate(ev.EventHandlerType, method);
                ev.AddEventHandler(null, d);
                OnModInspectDelegates[mod] = d;
            }
        }

        internal void Defrisk(WildfrostMod mod)
        {
            if (OnModInspectDelegates.ContainsKey(mod))
            {
                EventInfo ev = typeof(StabilizerEvents).GetEvent("OnModInspect");
                ev.RemoveEventHandler(null, OnModInspectDelegates[mod]);
            }
            
        }

        internal static ModTile MakeTile(Transform t, WildfrostMod mod, float size = 1.5f, bool active = true)
        {
            GameObject obj = GameObject.Instantiate(ModTile.GetPrefab(), t);
            ModTile tile = obj.GetComponent<ModTile>();
            tile.SetMod(mod);
            tile.SetSize(size);
            obj.SetActive(active);
            return tile;
        }

        private void PrepareCleaning(WildfrostMod _)
        {
            NeedsCleaning = true;
        }

        private void SceneLoaded(Scene scene)
        {
            if (scene.name == "MainMenu")
            {
                GameObject obj = GameObject.Find("Canvas/Safe Area/Menu/ButtonLayout/ModsButton/Animator/Button");
                Button button = obj.GetComponent<Button>();
                button.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
                button.onClick.AddListener(LoadModScene);
            }
        }

        private void SceneUnloaded(Scene scene)
        {
            if (scene.name == "Mods" && NeedsCleaning)
            {
                UtilMethods.Clean();
                NeedsCleaning = false;
            }
        }

        internal static void LoadModScene()
        {
            TileViewManager.properOpen = true;
            References.instance.StartCoroutine(SceneManager.Load("Mods",SceneType.Temporary));
        }

        internal void SaveConfigs()
        {
            ConfigStorage configs = FromConfigs();
            (ConfigItemAttribute, FieldInfo)[] store = configs.Store;
            foreach (var data in store)
            {
                data.Item1.defaultValue = data.Item2.GetValue(this).ToString();
            }
            configs.WriteToFile(Path.Combine(ModDirectory, "config.cfg"));
        }
    }
}
