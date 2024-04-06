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
            public GameObject video = null;
            public GameObject myScroller = null;
            private void Update()
            {
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
                if (Input.GetKeyDown(KeyCode.Equals))
                {
                    StartCoroutine(GoToCharacterSelect());
                }

                if (Input.GetKeyDown(KeyCode.B))
                {

                    GameObject scroller = null;
                    foreach (SmoothScrollRect j in UnityEngine.Object.FindObjectsOfType<SmoothScrollRect>())
                    {
                        if (j.gameObject.name == "Scroll View" && j.transform.parent.gameObject.name == "Challenges")
                        {
                            scroller = j.gameObject;
                            GameObject challenges = j.transform.parent.gameObject;
                            Debug.Log("[AnotherTribe] Scroller.");
                            myScroller = scroller.InstantiateKeepName();
                            scroller.SetActive(false);
                            SmoothScrollRect smoothScroll = myScroller.GetComponent<SmoothScrollRect>();
                            smoothScroll.viewport.gameObject.SetActive(false);
                            myScroller.transform.SetParent(challenges.transform);
                            if (video != null)
                            {
                                Debug.Log("[AnotherTribe] Made it this far");
                                GameObject g2 = new GameObject();
                                smoothScroll.viewport = g2.AddComponent<RectTransform>();
                                g2.transform.SetParent(myScroller.transform);
                                GameObject g = video.InstantiateKeepName();
                                Debug.Log("[AnotherTribe] Made it over");
                                g.SetActive(true);
                                g.transform.SetParent(g2.transform);
                            }
                            break;
                        }
                    }

                    /*if (framerate != null)
                    {
                        GameObject framerate2 = framerate.InstantiateKeepName();
                        framerate2.transform.parent = video.transform;
                        framerate2.GetComponentInChildren<TextMeshProUGUI>().text = "Not Max FPS";
                        framerate2.GetComponent<SetSettingInt>().key = "SpecialCardFrames";
                    }*/
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

            private IEnumerator GoToCampaign()
            {
                yield return Transition.To("Campaign");
                Transition.End();
            }
            private IEnumerator GoToCharacterSelect()
            {
                yield return Transition.To("CharacterSelect");
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
            List<CardData> list =  AddressableLoader.GetGroup<CardData>("CardData");
            for(int i=0; i< list.Count; i++)
            {
                /*string attackEffects = "";
                foreach(CardData.StatusEffectStacks s in list[i].attackEffects)
                {
                    attackEffects += s.data.name;
                    if (s.data.stackable)
                    {
                        attackEffects += string.Format("({0})", s.count);
                    }
                    attackEffects += ", ";
                }
                string startingEffects = "";
                foreach (CardData.StatusEffectStacks s in list[i].startWithEffects)
                {
                    startingEffects += s.data.name;
                    if (s.data.stackable)
                    {
                        startingEffects += string.Format("({0})", s.count);
                    }
                    startingEffects += ", ";
                }
                Debug.Log(string.Format("{0}||{1}|",attackEffects,startingEffects));*/
                string startingEffects = "";
                foreach (CardData.TraitStacks s in list[i].traits)
                {
                    startingEffects += s.data.name;
                    startingEffects += ", ";
                }
                Debug.Log(string.Format("{0}|", startingEffects));
            }
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
