using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using HarmonyLib;
using UnityEngine.Events;
using MultiplayerBase.UI;

namespace MultiplayerBase.Handlers
{
    //SequenceBeta2/BossRewardScreen/Layout/
    public class HandlerEvent : MonoBehaviour
    {
        public static HandlerEvent instance;

        public static event UnityAction<BossRewardData.Data> OnSelectBlessing;

        public Dictionary<int, GameObject> pingables = new Dictionary<int, GameObject>();
        private List<Friend> watchers = new List<Friend>();
        private GameObject indicatorGroup;
        int nextID = 0;

        protected void Start()
        {
            instance = this;
            transform.SetParent(GameObject.Find("CameraContainer/CameraMover/MinibossZoomer/CameraPositioner/CameraPointer/Animator/Rumbler/Shaker/InspectSystem").transform);
            transform.SetAsFirstSibling();
            transform.position = Vector3.zero;
            indicatorGroup = new GameObject("Indicator Group");
            indicatorGroup.transform.SetParent(transform);
            HandlerSystem.HandlerRoutines.Add("EVE", HandleMessage);
            Events.OnSceneChanged += ErasePingablesAndWatchers;
            OnSelectBlessing += SelectBlessing;
        }
        public void AskForData(Friend friend)
        {
            HandlerSystem.SendMessage("EVE", friend, "ASK");
        }

        public void SendData(Friend friend)
        {
            if (!watchers.Contains(friend))
            {
                watchers.Add(friend);
            }
            string s;
            string flag = "T";
            int i = 0;
            EventRoutine eventRoutine = GameObject.FindObjectOfType<EventRoutine>();
            if (eventRoutine != null)
            {
                Debug.Log("[Multiplayer] Found Event Routine");
                foreach(CardContainer container in eventRoutine.gameObject.GetComponentsInChildren<CardContainer>())
                {
                    Debug.Log($"[Multiplayer] {container.name}");
                    Entity[] entities = container.ToArray();
                    for(int j=entities.Length-1; j>=0; j--)
                    {
                        Entity entity = entities[j];
                        s = $"DISP!{i}!{flag}!{CardEncoder.Encode(entity.data, entity.data.id)}";
                        HandlerSystem.SendMessage("EVE", friend, s);
                        flag = "F";
                    }
                    i++;
                }
            }
        }

        public void SendRewardData(Friend friend)
        {
            if (!watchers.Contains(friend))
            {
                watchers.Add(friend);
            }
            string s;
            BossRewardSelect[] rewards = FindObjectsOfType<BossRewardSelect>();
            string flag = "T";
            for(int i=rewards.Length-1; i>=0; i--)
            {
                BossRewardSelect reward = rewards[i];
                int id = FindPingableID(reward.gameObject);
                if (reward is BossRewardSelectModifier)
                {
                    s = $"DISP2!{id}!{flag}!MODI!{reward.popUpName}";
                }
                else
                {
                    s = $"DISP2!{id}!{flag}!UPGR!{reward.popUpName}";
                }
                flag = "F";
                HandlerSystem.SendMessage("EVE", friend, s);
            }
        }

        public void HandleMessage(Friend friend, string message)
        {
            string[] messages = message.Split(new char[] { '!' });
            Debug.Log($"[Multiplayer] {message}");

            switch (messages[0])//0 -> Action
            {
                case "ASK":
                    SendData(friend);
                    break;
                case "DISP":
                    StartCoroutine(DisplayCard(friend, messages));
                    break;
                case "DISP2":
                    StartCoroutine(DisplayNoncard(friend, messages));
                    break;
                case "PING":
                    StartCoroutine(PingNoncard(friend, messages));
                    break;
                case "SELECT2":
                    StartCoroutine(SelectNoncard(friend, messages));
                    break;
            }
        }

        //SELECT2!name
        public IEnumerator SelectNoncard(Friend friend, string[] messages)
        {
            foreach (NoncardReward ncr in HandlerInspect.instance.charmLane)
            {
                if (ncr.name == messages[1] && NoncardViewer.FindDecoration(ncr).Item1.Id.Value == friend.Id.Value)
                {
                    CreateIndicator(ncr.GetComponent<RectTransform>());
                    yield break;
                }
            }
        }

        private void CreateIndicator(RectTransform rect)
        {
            GameObject obj = HelperUI.Background(indicatorGroup.transform, new Color(0.5f, 1f, 0.5f, 0.5f));
            obj.transform.localScale = Vector3.one;
            obj.GetComponent<RectTransform>().sizeDelta = 1.05f * rect.sizeDelta;
            obj.transform.position = rect.position;
        }


        private float pingDuration = 0.25f;
        //PING!Friend!ID!Sound
        public IEnumerator PingNoncard(Friend friend, string[] messages)
        {
            int id = int.Parse(messages[2]); //2->ID
            if (ulong.Parse(messages[1]) != HandlerSystem.self.Id.Value)//1 -> Friend
            {
                yield break;
            }
            if (pingables.ContainsKey(id) && pingables[id] != null)
            {
                if (pingables[id] == null)
                {
                    pingables.Remove(id);
                    yield break;
                }
                Debug.Log($"[Multpilayer] Pinging {pingables[id].name}");
                SfxSystem.OneShot("event:/sfx/modifiers/mod_bell_ringing");
                LeanTween.cancel(pingables[id]);
                pingables[id].transform.localScale = 1.4f*Vector3.one;
                LeanTween.scale(pingables[id], Vector3.one, pingDuration);
            }
        }

        public IEnumerator DisplayCard(Friend friend, string[] messages)
        {
            int index = int.Parse(messages[1]);
            bool flag = (messages[2] == "T");
            messages[1] = "DISP";
            messages[2] = friend.Id.ToString();
            yield return HandlerInspect.instance.DispCard(friend, messages.Skip(1).ToArray(), index, flag);
        }

        public IEnumerator DisplayNoncard(Friend friend, string[] messages)
        {
            bool flag = (messages[2] == "T");
            messages[2] = friend.Id.ToString();
            yield return HandlerInspect.instance.DispNoncard(friend, messages.Skip(1).ToArray(), flag);
        }

        public int FindPingableID(GameObject obj)
        {
            foreach(int key in pingables.Keys)
            {
                if (pingables[key] == obj)
                {
                    return key;
                }
            }
            return AssignNewID(obj);
        }

        public int AssignNewID(GameObject obj)
        {
            
            for (int i=0; i<100; i++)
            {
                nextID = (nextID + 1) % 100;
                if (!pingables.ContainsKey(nextID))
                {
                    pingables.Add(nextID, obj);
                    return nextID;
                }
            }
            throw new Exception("Michael: Congratulations! I either made a typo somewhere or you put 100 pingable objects on the screen at once. Wow!"); 
        }

        public void ErasePingablesAndWatchers(Scene scene)
        {
            pingables.Clear();
        }

        public static void InvokeOnSelectBlessing(BossRewardData.Data data)
        {
            OnSelectBlessing?.Invoke(data);
        }

        private void SelectBlessing(BossRewardData.Data data)
        {
            string dataName = GetNameOfReward(data);
            foreach(Friend friend in watchers)
            {
                HandlerSystem.SendMessage("EVE", friend, $"SELECT2!{dataName}");
            }
        }

        private static string GetNameOfReward(BossRewardData.Data data)
        {
            if (data is BossRewardDataCrown.Data crown)
            {
                return crown.upgradeDataName;
            }
            else if (data is BossRewardDataRandomCharm.Data charm)
            {
                return charm.upgradeName;
            }
            else if (data is BossRewardDataModifier.Data modifier)
            {
                return modifier.modifierName;
            }
            return null;
        }

        internal void HideIndicators()
        {
            indicatorGroup.gameObject.SetActive(false);
        }

        internal void ShowIndicators()
        {
            indicatorGroup.gameObject.SetActive(true);
        }

        internal void ClearIndicators()
        {
            indicatorGroup.transform.DestroyAllChildren();
        }
    }

    [HarmonyPatch(typeof(GainBlessingSequence2), "Select")]
    static class PatchOnSelectBlessingEvent
    {
        static void Prefix(BossRewardData.Data reward)
        {
            HandlerEvent.InvokeOnSelectBlessing(reward);
        }
    }
}
