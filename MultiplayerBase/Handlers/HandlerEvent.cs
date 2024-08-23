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

        public Dictionary<int, (GameObject,Vector3)> pingables = new Dictionary<int, (GameObject, Vector3)>();
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
            Events.OnShopItemPurchase += ItemPurchase;
            Events.OnEntityChosen += EntityChosen;
            OnSelectBlessing += SelectBlessing;
        }
        public void AskForData(Friend friend)
        {
            HandlerSystem.SendMessage("EVE", friend, "ASK");
        }

        public void SendData(Friend friend)
        {
            AddToWatchers(friend);
            string s;
            string flag = "T";
            int i = 0;
            EventRoutine eventRoutine = GameObject.FindObjectOfType<EventRoutine>();
            if (eventRoutine != null)
            {
                foreach(ShopItem item in eventRoutine.GetComponentsInChildren<ShopItem>())
                {
                    Debug.Log($"[Multiplayer] {item.name}");
                    if(item.GetComponent<CardCharm>() != null)
                    {
                        int id = FindPingableID(item.gameObject);
                        s = HandlerSystem.ConcatMessage(true,"DISP2",$"{id}",$"{flag}","UPGR",$"{item.GetComponent<CardCharm>().data.name}");
                        HandlerSystem.SendMessage("EVE", friend, s);
                        flag = "F";
                    }
                    else if(item.GetComponent<CrownHolderShop>() != null)
                    {
                        int id = FindPingableID(item.gameObject);
                        s = HandlerSystem.ConcatMessage(true, "DISP2", $"{id}", $"{flag}", "UPGR", $"{item.GetComponent<CrownHolderShop>().GetCrownData().name}");
                        HandlerSystem.SendMessage("EVE", friend, s);
                        flag = "F";
                    }
                    else if(item.GetComponent<CharmMachine>() != null && eventRoutine is ShopRoutine shop)
                    {
                        List<string> charms = shop.data.Get<ShopRoutine.Data>("shopData").charms;
                        if (charms.Count > 0)
                        {
                            int id = FindPingableID(item.gameObject);
                            s = HandlerSystem.ConcatMessage(true, "DISP2", $"{id}", $"{flag}", "MISC", "CharmMachine");
                            HandlerSystem.SendMessage("EVE", friend, s);
                            flag = "F";
                            s = HandlerSystem.ConcatMessage(true, "DISP2", $"{id}", $"{flag}", "MISC", "CharmMachine");
                            HandlerSystem.SendMessage("EVE", friend, s);
                            HandlerSystem.SendMessage("EVE", friend, s);
                        }
                        

                    }
                }
                Debug.Log("[Multiplayer] Found Event Routine");
                foreach(CardContainer container in eventRoutine.gameObject.GetComponentsInChildren<CardContainer>())
                {
                    Debug.Log($"[Multiplayer] {container.name}");
                    Entity[] entities = container.ToArray() ?? new Entity[0];
                    for(int j=entities.Length-1; j>=0; j--)
                    {
                        Entity entity = entities[j];
                        s = HandlerSystem.ConcatMessage(false, "DISP",$"{i}",$"{flag}",CardEncoder.Encode(entity.data, entity.data.id));
                        HandlerSystem.SendMessage("EVE", friend, s);
                        flag = "F";
                    }
                    i++;
                }
            }
        }

        public void SendRewardData(Friend friend)
        {
            AddToWatchers(friend);
            string s;
            BossRewardSelect[] rewards = FindObjectsOfType<BossRewardSelect>();
            string flag = "T";
            for(int i=rewards.Length-1; i>=0; i--)
            {
                BossRewardSelect reward = rewards[i];
                int id = FindPingableID(reward.gameObject);
                if (reward is BossRewardSelectModifier)
                {
                    s = HandlerSystem.ConcatMessage(true, "DISP2",$"{id}",$"{flag}","MODI",reward.popUpName);
                }
                else
                {
                    s = HandlerSystem.ConcatMessage(true, "DISP2", $"{id}", $"{flag}", "UPGR", reward.popUpName);
                }
                flag = "F";
                HandlerSystem.SendMessage("EVE", friend, s);
            }
        }

        private void AddToWatchers(Friend friend)
        {
            if (!watchers.Contains(friend))
            {
                watchers.Add(friend);
                string s = "Watchers: ";
                foreach(Friend f in watchers)
                {
                    s += f.Name + ", ";
                }
                MultiplayerMain.textElement.text = s.Substring(0, s.Length - 2);
            }
        }

        private void SendToWatchers(params string[] content)
        {
            string s = HandlerSystem.ConcatMessage(true, content);
            foreach(Friend f in watchers)
            {
                HandlerSystem.SendMessage("EVE", f, s);
            }
        }

        public void HandleMessage(Friend friend, string message)
        {
            string[] messages = HandlerSystem.DecodeMessages(message);
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
                case "SELECT":
                    StartCoroutine(SelectCard(friend, messages));
                    break;
                case "SELECT2":
                    StartCoroutine(SelectNoncard(friend, messages));
                    break;
            }
        }

        //SELECT!id
        public IEnumerator SelectCard(Friend friend, string[] messages)
        {
            foreach (OtherCardViewer ocv in HandlerInspect.instance.lanes)
            {
                foreach(Entity entity in ocv.entities)
                {
                    (Friend, ulong) pair = ocv.Find(entity);
                    if (ulong.Parse(messages[1]) == pair.Item2 && friend.Id.Value == pair.Item1.Id.Value)
                    {
                        CreateIndicator(entity.GetComponentInChildren<RectTransform>(), 0.6f);
                        yield break;
                    }
                }
            }
        }

        //SELECT2!name or SELECT2!name!Type!newName
        public IEnumerator SelectNoncard(Friend friend, string[] messages)
        {
            foreach (NoncardReward ncr in HandlerInspect.instance.charmLane)
            {
                if (ncr.name == messages[1] && NoncardViewer.FindDecoration(ncr).Item1.Id.Value == friend.Id.Value)
                {
                    CreateIndicator(ncr.GetComponent<RectTransform>(),1.05f);
                    if (messages.Length > 2)
                    {
                        if (messages[2] == "UPGR")
                        {
                            ncr.UpdateUpgrade(messages[3]);
                        }
                    }
                    yield break;
                }
            }
        }

        private void CreateIndicator(RectTransform rect, float factor)
        {
            GameObject obj = HelperUI.Background(indicatorGroup.transform, new Color(0.5f, 1f, 0.5f, 0.5f));
            obj.transform.localScale = Vector3.one;
            obj.GetComponent<RectTransform>().sizeDelta = factor * rect.sizeDelta;
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
            if (pingables.ContainsKey(id) && pingables[id].Item1 != null)
            {
                if (pingables[id].Item1 == null)
                {
                    pingables.Remove(id);
                    yield break;
                }
                Debug.Log($"[Multpilayer] Pinging {pingables[id].Item1.name}");
                SfxSystem.OneShot("event:/sfx/modifiers/mod_bell_ringing");
                LeanTween.cancel(pingables[id].Item1);
                pingables[id].Item1.transform.localScale = 1.4f * pingables[id].Item2;
                LeanTween.scale(pingables[id].Item1, pingables[id].Item2, pingDuration);
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
                if (pingables[key].Item1 == obj)
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
                    pingables.Add(nextID, (obj,obj.transform.localScale));
                    return nextID;
                }
            }
            throw new Exception("Michael: Congratulations! I either made a typo somewhere or you put 100 pingable objects on the screen at once. Wow!"); 
        }

        public void ErasePingablesAndWatchers(Scene scene)
        {
            pingables.Clear();
        }

        public static void InvokeSelectBlessing(BossRewardData.Data data)
        {
            OnSelectBlessing?.Invoke(data);
        }

        private void SelectBlessing(BossRewardData.Data data)
        {
            string dataName = GetNameOfReward(data);
            SendToWatchers($"SELECT2!{dataName}");
        }

        private void ItemPurchase(ShopItem item)
        {
            if (item.GetComponent<CardCharm>() != null)
            {
                SendToWatchers("SELECT2",item.GetComponent<CardCharm>().data.name);
            }
            else if (item.GetComponent<CrownHolderShop>() != null)
            {
                SendToWatchers("SELECT2",item.GetComponent<CrownHolderShop>().GetCrownData().name);
            }
            else if (item.GetComponent<CharmMachine>() != null)
            {
                SendToWatchers("SELECT2","CharmMachine", "UPGR", References.PlayerData.inventory.upgrades.Last().name);
            }
            else if (item.GetComponent<Entity>() != null)
            {
                SendToWatchers($"SELECT",item.GetComponent<Entity>().data.id.ToString());
            }
        }

        private void EntityChosen(Entity entity)
        {
            SendToWatchers($"SELECT", entity.data.id.ToString());
        }

        private static string GetNameOfReward(BossRewardData.Data data)
        {
            if (data is BossRewardDataCrown.Data crown)
            {
                string crownName = crown.upgradeDataName.IsNullOrWhitespace() ? "Crown" : crown.upgradeDataName;
                return crownName;
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
            HandlerEvent.InvokeSelectBlessing(reward);
        }
    }
}
