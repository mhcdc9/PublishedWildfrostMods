using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerBase.Handlers
{
    public class HandlerEvent : MonoBehaviour
    {
        public static HandlerEvent instance;

        protected void Start()
        {
            instance = this;
            transform.SetParent(GameObject.Find("CameraContainer/CameraMover/MinibossZoomer/CameraPositioner/CameraPointer/Animator/Rumbler/Shaker/InspectSystem").transform);
            transform.SetAsFirstSibling();
            transform.position = Vector3.zero;
            HandlerSystem.HandlerRoutines.Add("EVE", HandleMessage);
        }
        public void AskForData(Friend friend)
        {
            HandlerSystem.SendMessage("EVE", friend, "ASK");
        }

        public void SendData(Friend friend)
        {
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
                        s = $"DISP!{i}!{flag}!{HandlerInspect.EncodeEntity(entity, entity.data.id)}";
                        HandlerSystem.SendMessage("EVE", friend, s);
                        flag = "F";
                    }
                    i++;
                }
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
            }
        }

        public IEnumerator DisplayCard(Friend friend, string[] messages)
        {
            int index = int.Parse(messages[1]);
            bool flag = (messages[2] == "T");
            messages[1] = "DISP";
            messages[2] = friend.Name;
            yield return HandlerInspect.instance.DispCard(friend, messages.Skip(1).ToArray(), index, flag);
        }
    }
}
