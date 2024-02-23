using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnotherTribe
{
    public class AnotherTribe : WildfrostMod
    {
        public class GameModeCycler : MonoBehaviour
        {
            public static string[] gameModes = new string[] { "GameModeNormal" };
            public static GameMode currentGameMode;
            public static int index = 0;
            private void Update()
            {
                if (Input.GetKeyDown(KeyCode.L))
                {
                    StartCoroutine(Hijack());
                }
                if (Input.GetKeyDown(KeyCode.K))
                {
                    if (SceneManager.ActiveSceneKey != "Town")
                        return;
                    Campaign.Data = new CampaignData(AddressableLoader.Get<GameMode>("GameMode", "GameModeChimera"));
                    Debug.Log("[Another Tribe] Starting Campaign Skip.");
                    ClassData cd = AddressableLoader.Get<ClassData>("ClassData", "Chimera").InstantiateKeepName();
                    Debug.Log("[Another Tribe] " + cd.name);
                    //CardData drek = AddressableLoader.Get<CardData>("CardData", "Leader3_vim").Clone();
                    //Debug.Log("[Another Tribe] " + drek.name);
                    Inventory inventory = cd.startingInventory.Clone();
                    //inventory.deck.Add(drek);
                    Debug.Log("[Another Tribe] Inventory Complete.");
                    References.PlayerData = new PlayerData(cd, inventory);
                    StartCoroutine(GoToCampaign());
                }
                if (Input.GetKeyDown(KeyCode.Asterisk))
                {
                    GameObject video = null;
                    foreach (JournalPageMenu j in UnityEngine.Object.FindObjectsOfType<JournalPageMenu>())
                    {
                        if (j.gameObject.name == "VideoSettings")
                        {
                            video = j.gameObject;
                            Debug.Log("[AnotherTribe] Found VideoSettings.");
                            break;
                        }
                    }
                    GameObject framerate = null;
                    foreach (Transform t in video.GetComponentsInChildren<Transform>())
                    {
                        if (t.gameObject.name == "Target Frame Rate")
                        {
                            Debug.Log("[AnotherTribe] Found it.");
                            framerate = t.gameObject;
                            break;
                        }
                    }
                    if (framerate != null)
                    {
                        GameObject framerate2 = framerate.InstantiateKeepName();
                        framerate2.transform.parent = video.transform;
                        framerate2.GetComponentInChildren<TextMeshProUGUI>().text = "Not Max FPS";
                        framerate2.GetComponent<SetSettingInt>().key = "SpecialCardFrames";
                    }
                }

                if (Input.GetKeyDown(KeyCode.M))
                {
                    Debug.Log("[AnotherTribe] Button Pressed.");
                    SelectTribe selectTribe = FindObjectOfType<SelectTribe>();
                    if ((object)selectTribe != null && selectTribe.GetComponentsInChildren<Transform>().Length > 1)
                    {
                        CampaignData cData = Campaign.Data;
                        if (cData != null)
                        {
                            index = (index + 1) % gameModes.Length;
                            currentGameMode = AddressableLoader.Get<GameMode>("GameMode", gameModes[index]);
                            Campaign.Data = new CampaignData(gameModes[index]);
                            StartCoroutine(Do(selectTribe));
                        }
                    }
                }
            }

            private IEnumerator Hijack()
            {
                yield return SceneManager.WaitUntilUnloaded("CardFramesUnlocked");
                yield return SceneManager.Load("CardFramesUnlocked", SceneType.Temporary);
                CardFramesUnlockedSequence sequence = GameObject.FindObjectOfType<CardFramesUnlockedSequence>();
                Debug.Log("[Another Tribe] " + sequence.name);
                TextMeshProUGUI titleObject = sequence.GetComponentInChildren<TextMeshProUGUI>(true);
                titleObject.text = "<size=0.55>What? <#ff0>Magikarp</color> has\n evolved into <#ff0>Gyarados</color>!";
                //titleObject.text = "<size=0.55>What? <#ff0>X</color> Pokemon have evolved!";
                yield return sequence.StartCoroutine("CreateCards", new string[] { "Sword", "Noodle", "Witch", "Snowcracker", "Crowbar" });
            }

            private IEnumerator GoToCampaign()
            {
                yield return Transition.To("Campaign");
                Transition.End();
            }

            private IEnumerator Do(SelectTribe tribe)
            {
                tribe.SetAvailableTribes(currentGameMode.classes.ToList());
                tribe.StopCoroutine("SelectRoutine");
                foreach(Transform t in tribe.gameObject.GetComponentsInChildren<Transform>())
                {
                    if (t == tribe.gameObject.transform)
                    {
                        continue;
                    }
                    t.gameObject.Destroy();
                }
                tribe.Run();
                yield return null;
            }
        }


        public AnotherTribe(string modDirectory) : base(modDirectory)
        {
        }

        public GameModeCycler gmc = new GameModeCycler();

        public override void Load()
        {
            base.Load();
            Events.OnSceneLoaded += HookScene;
            Events.OnSceneChanged += ChangeScene;
            ClassData chimera = ScriptableObject.CreateInstance<ClassData>();
            chimera.ModAdded = this;
            chimera.requiresUnlock = null;
            chimera.name = "Chimera";
            chimera.startingInventory = References.Classes[0].startingInventory;
            chimera.leaders = References.Classes[1].leaders;
            chimera.characterPrefab = References.Classes[2].characterPrefab;
            chimera.rewardPools = References.Classes[0].rewardPools;
            chimera.selectSfxEvent = References.Classes[1].selectSfxEvent;
            chimera.flag = References.Classes[2].flag;
            AddressableLoader.AddToGroup<ClassData>("ClassData", chimera);
            GameMode gameMode = Get<GameMode>("GameModeNormal").InstantiateKeepName();
            gameMode.name = "GameModeChimera";
            //gameMode.doSave = false;
            gameMode.doSave = false;
            gameMode.saveFileName = "Chimera";
            gameMode.mainGameMode = false;
            //gameMode.classes = gameMode.classes.AddItem(chimera).ToArray();
            gameMode.classes[0] = chimera;
            AddressableLoader.AddToGroup<GameMode>("GameMode", gameMode);
            CampaignData data = Campaign.Data;
            if (data != null )
            {
                Debug.Log("[Another Tribe] Data already exists. Changing now.");
                data.GameMode = gameMode;
            }
            else
            {
                Debug.Log("[Another Tribe] Data does not exist. Bummer.");
            }
            gmc = new GameObject("GameMode").AddComponent<GameModeCycler>();
            UnityEngine.Object.DontDestroyOnLoad(gmc);
            GameModeCycler.gameModes = GameModeCycler.gameModes.AddItem("GameModeChimera").ToArray();
            //References.Classes[0] = chimera;
        }

        private void ChangeScene(Scene arg0)
        {
            Debug.Log("[AnotherTribe] Changed to Scene " + arg0.name);
        }

        private void HookScene(Scene arg0)
        {
            Debug.Log("[AnotherTribe] Loaded Scene " + arg0.name);
        }

        public override void Unload()
        {
            base.Unload();
            Events.OnSceneLoaded -= HookScene;
            Events.OnSceneChanged -= ChangeScene;
            gmc.Destroy();
        }


        public override string GUID => "mhcdc9.wildfrost.newtribe";

        public override string[] Depends => new string[0];

        public override string Title => "Another Tribe";

        public override string Description => "Attempt to add another tribe.";
    }
}
