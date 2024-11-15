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

        public override string Title => "Mod Stabilizer";

        public override string Description => "The goal is to stabilize things";

        public static Stabilizer Instance;

        internal static GameObject prefabHolder;

        public static bool NeedsCleaning = false;

        [ConfigItem(true, comment: "", forceTitle: "StartAsTileView")]
        public static bool startAsTileView = true;

        [ConfigItem(6, comment: "", forceTitle: "TilesPerRow")]
        public static int tilesPerRow = 6;
        public Stabilizer(string modDirectory) : base(modDirectory) 
        {
            Instance = this;

            
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

            Events.OnEntityCreated += UtilMethods.FixImage;
            Events.OnModUnloaded += PrepareCleaning;
            Events.OnSceneLoaded += SceneLoaded;
            Events.OnSceneUnload += SceneUnloaded;
        }

        public override void Unload()
        {
            base.Unload();

            Events.OnEntityCreated -= UtilMethods.FixImage;
            Events.OnModUnloaded -= PrepareCleaning;
            Events.OnSceneLoaded -= SceneLoaded;
            Events.OnSceneUnload -= SceneUnloaded;
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
    }
}
