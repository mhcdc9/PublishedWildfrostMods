using Newtonsoft.Json.Linq;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using MultiplayerBase.UI;

namespace MultiplayerBase.Handlers
{
    public class HandlerBattle : MonoBehaviour
    {
        public static UnityAction<Friend> OnFetch;

        public static HandlerBattle instance;
        public static readonly List<PlayAction> actions = new List<PlayAction>();

        GameObject background;

        private CardControllerSelectCard cc;
        int lanes = 2;
        OtherCardViewer[] playerLanes;
        OtherCardViewer[] enemyLanes;

        Vector3 defaultPosition = new Vector3(0, 0, -8);
        Vector3 viewerPosition = new Vector3(0, 0, 2);

        static Button refreshButton;
        static Button fetchButton;

        protected void Start()
        {
            //Events.OnSceneUnload += DisableController;
            
            instance = this;

            refreshButton = Dashboard.buttons[1];
            refreshButton.onClick.AddListener(QueueActions);

            fetchButton = Dashboard.buttons[2];
            fetchButton.onClick.AddListener(Fetch);

            cc = gameObject.AddComponent<CardControllerSelectCard>();
            cc.pressEvent = new UnityEventEntity();
            cc.hoverEvent = new UnityEventEntity();
            cc.unHoverEvent = new UnityEventEntity();
            cc.pressEvent.AddListener(HandlerInspect.SelectPing);

            transform.SetParent(GameObject.Find("CameraContainer/CameraMover/MinibossZoomer/CameraPositioner/CameraPointer/Animator/Rumbler/Shaker/InspectSystem").transform);
            transform.SetAsFirstSibling();
            transform.position = defaultPosition;
            CreateBattleViewer();
            HandlerSystem.HandlerRoutines.Add("BAT", HandleMessage);
        }

        private void CreateBattleViewer()
        {
            background = HelperUI.Background(transform, new Color(1f, 1f, 1f, 0.75f));

            playerLanes = new OtherCardViewer[lanes];
            for (int i = 0; i < playerLanes.Length; i++)
            {
                playerLanes[i] = HelperUI.OtherCardViewer($"Player Row {i + 1}", background.transform, cc);
                playerLanes[i].transform.localPosition = new Vector3(-0.47f, 0.26f - 0.43f * i, 0);
                //0.47, -0.17
                //0.47, 0.26
                playerLanes[i].dir = 1;
                playerLanes[i].gap = new Vector3(1f, 0, 0);
                playerLanes[i].SetSize(3, 0.6667f);
                cc.hoverEvent.AddListener(playerLanes[i].Hover);
                cc.unHoverEvent.AddListener(playerLanes[i].Unhover);
            }

            enemyLanes = new OtherCardViewer[lanes];
            for (int i = 0; i < enemyLanes.Length; i++)
            {
                enemyLanes[i] = HelperUI.OtherCardViewer($"Enemy Row {i + 1}", background.transform, cc);
                enemyLanes[i].transform.localPosition = new Vector3(0.47f, 0.26f - 0.43f * i, 0);
                enemyLanes[i].gap = new Vector3(1f, 0, 0);
                enemyLanes[i].SetSize(3, 0.6667f);
                cc.hoverEvent.AddListener(enemyLanes[i].Hover);
                cc.unHoverEvent.AddListener(enemyLanes[i].Unhover);
            }
            background.SetActive(false);
        }

        public void ToggleViewer(Friend friend)
        {
            if (background == null)
            {
                CreateBattleViewer();
            }
            if (background.activeSelf)
            {
                background.SetActive(false);
                background.transform.SetParent(transform);
                Clear();
            }
            else
            {
                Clear();
                AskForData(friend);
                if (Battle.instance != null)
                {
                    background.transform.SetParent(GameObject.Find("Battle/Canvas/CardController/Board/Canvas").transform);
                }
                background.transform.localPosition = defaultPosition;
                background.SetActive(true);
                LeanTween.moveLocal(background, viewerPosition, 0.75f).setEase(LeanTweenType.easeInOutQuart);
                //StartCoroutine(PopulateRows());
            }
        }

        public void Clear()
        {
            foreach(OtherCardViewer ocv in playerLanes)
            {
                ocv.ClearAndDestroyAllImmediately();
            }
            foreach (OtherCardViewer ocv in enemyLanes)
            {
                ocv.ClearAndDestroyAllImmediately();
            }
        }

        public void AskForData(Friend friend)
        {
            HandlerSystem.SendMessage("BAT", friend, "ASK!PLAYER!ENEMY!");
        }

        public void SendData(Friend friend, string[] messages)
        {
            string s;
            for (int i = 1; i < messages.Length; i++)
            {
                s = $"";
                switch (messages[i])
                {
                    case "PLAYER":
                        for(int j = 0; j < Battle.instance.rows[References.Player].Count; j++)
                        {
                            Entity[] entities = Battle.instance.rows[References.Player][j].ToArray();
                            for (int k=0; k<entities.Length; k++)
                            {
                                s = $"PLAYER!{j}!" + HandlerInspect.EncodeEntity(entities[k], entities[k].data.id);
                                HandlerSystem.SendMessage("BAT", friend, s);
                            }
                        }
                        break;
                    case "ENEMY":
                        for (int j = 0; j < Battle.instance.rows[Battle.GetOpponent(References.Player)].Count; j++)
                        {
                            Entity[] entities = Battle.instance.rows[Battle.GetOpponent(References.Player)][j].ToArray();
                            for (int k = 0; k < entities.Length; k++)
                            {
                                s = $"ENEMY!{j}!" + HandlerInspect.EncodeEntity(entities[k], entities[k].data.id);
                                HandlerSystem.SendMessage("BAT", friend, s);
                            }
                        }
                        break;
                    case "INFO":
                        OnFetch?.Invoke(friend);
                        break;
                    default:
                        break;
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
                    SendData(friend, messages);
                    break;
                case "ENEMY":
                    StartCoroutine(PlaceEnemyCard(friend, messages));
                    break;
                case "PLAYER":
                    StartCoroutine(PlacePlayerCard(friend, messages));
                    break;
            }
        }

        public IEnumerator PlacePlayerCard(Friend friend, string[] messages)
        {
            Card card = HandlerInspect.CreateDisplayCard(cc, messages.Skip(3).ToArray());
            OtherCardViewer ocv = playerLanes[int.Parse(messages[1])];
            ocv.Add(card.entity,friend,ulong.Parse(messages[2]));
            ocv.SetChildPosition(card.entity);
            yield return card.UpdateData();
            card.entity.flipper.FlipUp(force: true);
        }

        public IEnumerator PlaceEnemyCard(Friend friend, string[] messages)
        {
            Card card = HandlerInspect.CreateDisplayCard(cc, messages.Skip(3).ToArray());
            OtherCardViewer ocv = enemyLanes[int.Parse(messages[1])];
            ocv.Add(card.entity, friend, ulong.Parse(messages[2]));
            ocv.SetChildPosition(card.entity);
            yield return card.UpdateData();
            card.entity.flipper.FlipUp(force: true);
        }

        public void CreateController()
        {
            if (cc == null)
            {
                cc = gameObject.AddComponent<CardControllerSelectCard>();
                cc.pressEvent = new UnityEventEntity();
                cc.hoverEvent = new UnityEventEntity();
                cc.unHoverEvent = new UnityEventEntity();
                foreach (OtherCardViewer ocv in playerLanes) 
                {
                    ocv.AssignController(cc);
                }
                foreach (OtherCardViewer ocv in enemyLanes)
                {
                    ocv.AssignController(cc);
                }
            }
        }

        public IEnumerator BattleRoutine()
        {

            Battle.Phase phase = Battle.instance.phase;
            while (true)
            {
                yield return new WaitUntil(() => (Battle.instance == null || phase != Battle.instance.phase));
                if (Battle.instance == null)
                {
                    yield break;
                }
                Debug.Log("[Battle Handler] Phase Change: " + Battle.instance.phase);
                phase = Battle.instance.phase;
                switch (phase)
                {
                    case Battle.Phase.Battle:
                        QueueActions();
                        break;
                    case Battle.Phase.Play:
                        break;
                    case Battle.Phase.End:

                        break;
                }
            }
        }

        public static bool TryAction(PlayAction action)
        {
            if (Battle.instance == null)
            {
                return false;
            }
            if (Battle.instance.phase == Battle.Phase.Battle)
            {
                ActionQueue.Add(action);
            }
            else
            {
                actions.Add(action);
                refreshButton.GetComponentInChildren<TextMeshProUGUI>().text = actions.Count().ToString();
            }
            return true;
        }

        private static void QueueActions()
        {
            if (Battle.instance == null)
            {
                return;
            }
            foreach (PlayAction action in actions)
            {
                ActionQueue.Add(action);
            }
            actions.Clear();
            refreshButton.GetComponentInChildren<TextMeshProUGUI>().text = actions.Count().ToString();
        }

        public void Fetch()
        {
            if (background != null && background.activeSelf)
            {
                HandlerSystem.SendMessageToAllOthers("BAT", "ASK!INFO!PLAYER!ENEMY!");
            }
            else
            {
                HandlerSystem.SendMessageToAllOthers("BAT", "ASK!INFO!");
            }
        }
    }
}
