using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace GamemodeAndClasses
{
    public class MainModClass : WildfrostMod
    {
        public class GameModeManager : MonoBehaviour
        {
            private FloatingText floatingText;
            private string scene;
            internal void Town()
            {
                FloatingTextManager floatingTextManager = FindObjectOfType<FloatingTextManager>();
                floatingText = floatingTextManager.CreatePrefab();
                foreach(Building building in FindObjectsOfType<Building>())
                {
                    if (building.name == "Gate(Clone)")
                    {
                        floatingText.transform.SetParent(building.transform.parent, false);
                        floatingText.transform.Translate(new Vector3(1.83f, -3.5f, 0));
                        floatingText.textAsset.alignment = TextAlignmentOptions.Left;
                        break;
                    }
                }
                if (index >= gameModes.Count)
                {
                    index = 0;
                }
                floatingText.SetText($"<color=#888888>[Q] {displayedNames[index]}</color>");
                scene = "Town";
            }

            internal void CharacterSelect()
            {
                FloatingTextManager floatingTextManager = FindObjectOfType<FloatingTextManager>();
                floatingText = floatingTextManager.CreatePrefab();
                CharacterSelectScreen cs = FindObjectOfType<CharacterSelectScreen>();
                TMP_Text title = cs.GetComponentInChildren<TMP_Text>();
                floatingText.transform.SetParent(title.transform, false);
                floatingText.transform.Translate(new Vector3(0, -1.45f, 0));
                if (index >= gameModes.Count)
                {
                    index = 0;
                }
                floatingText.SetText(displayedNames[index]);
                scene = "CharacterSelect";
            }

            private void Update()
            {
                if (Input.GetKeyDown(KeyCode.Q) && scene == "Town")
                {
                    index++;
                    index %= gameModes.Count;
                    floatingText.SetText($"<color=#888888>[Q] {displayedNames[index]}</color>");
                }
            }
        }

        public static event UnityAction<GameMode> OnGateClicked;

        public static void InvokeOnGateClicked(GameMode gameMode)
        {
            OnGateClicked?.Invoke(gameMode);
        }

        public static List<string> gameModes = new List<string> { "GameModeNormal"};
        public static List<string> displayedNames = new List<string> { "Standard Mode"};
        public static int index = 0;
        internal static GameObject gmc;
        public override string GUID => "mhcdc9.wildfrost.gameandclass";

        public override string[] Depends => new string[0];

        public override string Title => "Game Mode and Classes Helper";

        public override string Description => "A mod containing two builder extensions and ways to cycle through game modes";

        public MainModClass(string ModDirectory) : base(ModDirectory) 
        {

        }

        private List<ClassDataBuilderExt> classData;
        private List<GameModeBuilderExt> modes;

        private RewardPool reward(string name) => Extensions.GetRewardPool(name);
        private CardData card(string name) => Get<CardData>(name);

        private void CreateModAssets()
        {
            ClassData basic = Get<ClassData>("Basic");
            ClassData magic = Get<ClassData>("Magic");
            ClassData clunk = Get<ClassData>("Clunk");
            classData = new List<ClassDataBuilderExt>();

            //Example code (may not reflect the Chimera class in the future)
            classData.Add(
                new ClassDataBuilderExt(this)
                .CreateExt("Chimera")
                .AddCardsInDeck(
                    card("Gearhammer"),
                    card("Gearhammer"),
                    card("Gearhammer"),
                    card("Gearhammer"),
                    card("SnowStick"),
                    card("SnowStick"),
                    card("SunRod"),
                    card("Voidstone"),
                    card("JunjunMask")
                ).WithLeaders(clunk.leaders.Concat(magic.leaders.Concat(basic.leaders)).ToArray())
                .WithFlag(clunk.flag)
                .WithSelectSfxEvent(clunk.selectSfxEvent)
                .WithCharacterPrefab(clunk.characterPrefab)
                .WithRewardPools(
                    reward("GeneralUnitPool"),
                    reward("GeneralItemPool"),
                    reward("GeneralCharmPool"),
                    reward("GeneralModifierPool"),
                    reward("SnowUnitPool"),
                    reward("SnowItemPool"),
                    reward("SnowCharmPool"),
                    reward("BasicUnitPool"),
                    reward("BasicItemPool"),
                    reward("BasicCharmPool"),
                    reward("MagicUnitPool"),
                    reward("MagicItemPool"),
                    reward("MagicCharmPool"),
                    reward("ClunkUnitPool"),
                    reward("ClunkItemPool"),
                    reward("ClunkCharmPool")
                ) as ClassDataBuilderExt
                );

            modes = new List<GameModeBuilderExt>();

            modes.Add(
                new GameModeBuilderExt(this)
                .CreateGameMode("GameModeGauntlet", "Boss Gauntlet", saveFileSuffix: "Gauntlet")
                .NewGenerator("ScbbccbbsbbirsrbsBrsbsbsbrbsbrrbsBrbsbrrFT\n" +
                              "     r  u   rr  c rr r r r s rr + r   rr  \n" +
                              "000000111111111222223333334444455556666678\n" +
                              "000000000000000000111111111111111122222222")
                );
        }

        public override void Load()
        {
            CreateModAssets();
            Events.OnSceneChanged += SceneChanged;
            base.Load();
            GameMode gm = Get<GameMode>("GameModeNormal");
            gm.classes = new ClassData[4] { gm.classes[0], gm.classes[1], gm.classes[2], Get<ClassData>("Chimera") };
            /*gm.generator = gm.generator.InstantiateKeepName();
            gm.generator.presets = new TextAsset[1]
            {
                new TextAsset("SbrrcbirrsBrrsbirrbrrsBrbrrrFT\n" +
                              " b rrb rr B rrb rrb rrB b rr  \n" +
                              "000001111122223333444455666678\n" +
                              "000000000001111111111112222222")
            };
            AddressableLoader.AddToGroup<GameMode>("GameMode", gm);*/
            gmc = new GameObject("GameModeManager");
            gmc.SetActive(false);
            gmc.AddComponent<GameModeManager>();
            UnityEngine.Object.DontDestroyOnLoad(gmc);
            
        }

        public override List<T> AddAssets<T, Y>()
        {
            var typeName = typeof(Y).Name;
            switch (typeName)
            {
                case nameof(ClassData):
                    return classData.Cast<T>().ToList();
                case nameof(GameMode):
                    return modes.Cast<T>().ToList();
                default:
                    return null;
            }
        }


        private void SceneChanged(Scene scene)
        {
            switch(scene.name)
            {
                case "Town":
                    gmc.GetComponent<GameModeManager>().Town();
                    gmc.SetActive(true);
                    break;
                case "CharacterSelect":
                    gmc.GetComponent<GameModeManager>().CharacterSelect();
                    gmc.SetActive(true);
                    break;
                default:
                    gmc.SetActive(false); 
                    break;
            }
        }

        public override void Unload()
        {
            gmc.Destroy();
            Events.OnSceneChanged -= SceneChanged;
            base.Unload();
        }
    }

    [HarmonyPatch(typeof(Menu), "StartGameOrContinue", new Type[0])]
    internal static class GateOverride
    {
        internal static bool Prefix(Menu __instance)
        {
            GameMode gm = AddressableLoader.Get<GameMode>("GameMode",MainModClass.gameModes[MainModClass.index]);
            if (gm == null)
            {
                Debug.LogWarning($"[GameMode] Could not find GameMode {MainModClass.gameModes[MainModClass.index]}. Reverting to GameModeNormal");
                MainModClass.index = 0;
                return true;
            }
            Debug.Log($"[GameMode] {gm!=null}");
            if (gm.classes == null || gm.classes.Length == 0)
            {
                gm.classes = AddressableLoader.Get<GameMode>("GameMode", "GameModeNormal").classes;
            }
            if (gm.generator == null)
            {
                gm.generator = AddressableLoader.Get<GameMode>("GameMode", "GameModeNormal").generator;
            }
            if (gm.populator == null)
            {
                gm.populator = AddressableLoader.Get<GameMode>("GameMode", "GameModeNormal").populator;
            }
           MainModClass.InvokeOnGateClicked(gm);
            __instance.StartGameOrContinue(MainModClass.gameModes[MainModClass.index]);
            return false;
        }
    }

    /*
    [HarmonyPatch(typeof(TownHallFlagSetter), "SetupFlags", new Type[0])]
    internal static class FlagSkip
    {
        internal static bool Prefix(TownHallFlagSetter __instance)
        {
            foreach (GameObject flag in __instance.flags)
            {
                flag.SetActive(true);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(InjuredCompanionEventSystem), "GetMostRecentRun", new Type[0])]
    internal static class InjuredCompanionSkip
    {
        internal static bool Prefix(ref RunHistory __result)
        {
            __result = null;
            return false;
        }
    }
    */
}
