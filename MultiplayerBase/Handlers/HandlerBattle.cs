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
using MultiplayerBase.Battles;
using Steamworks.Data;
using Color = UnityEngine.Color;

namespace MultiplayerBase.Handlers
{
    public class HandlerBattle : MonoBehaviour
    {
        public static event UnityAction<Friend> OnBattleViewerOpen;
        public static event UnityAction<Friend> OnBattleViewerClose;
        public static event UnityAction<Friend, Entity> OnPlayOtherCard;
        public static event UnityAction<Friend, Entity> OnPostPlayOtherCard;
        public static event UnityAction<Friend, Entity> OnSendCardToPlay;
        public static event UnityAction<Friend, Entity> OnPostSendCardToPlay;

        public static UnityAction<Friend> OnFetch;

        public static HandlerBattle instance;
        public static readonly List<PlayAction> actions = new List<PlayAction>();
        public static Friend friend;

        GameObject background;

        //Use Blcoking for your PlayActions
        public bool Blocking => background != null && background.activeSelf;

        private CardControllerSelectCard cc => HandlerInspect.instance.cc;
        private CardControllerBattle cb;

        public CardControllerBattle CB => cb;

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

            /*
            cc.pressEvent = new UnityEventEntity();
            cc.hoverEvent = new UnityEventEntity();
            cc.unHoverEvent = new UnityEventEntity();
            cc.pressEvent.AddListener(HandlerInspect.SelectPing);
            */

            transform.SetParent(GameObject.Find("CameraContainer/CameraMover/MinibossZoomer/CameraPositioner/CameraPointer/Animator/Rumbler/Shaker/InspectSystem").transform);
            transform.SetAsFirstSibling();
            transform.position = defaultPosition;
            CreateBattleViewer();
            HandlerSystem.HandlerRoutines.Add("BAT", HandleMessage);
        }

        private void CreateBattleViewer()
        {
            background = HelperUI.Background(transform, new Color(1f, 1f, 1f, 0.75f));
            cb = background.AddComponent<CardControllerMultiplayerBattle>();
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
                playerLanes[i].gameObject.AddComponent<UINavigationItem>();
                cc.hoverEvent.AddListener(playerLanes[i].Hover);
                cc.unHoverEvent.AddListener(playerLanes[i].Unhover);
                playerLanes[i].AssignController(cb);
            }

            enemyLanes = new OtherCardViewer[lanes];
            for (int i = 0; i < enemyLanes.Length; i++)
            {
                enemyLanes[i] = HelperUI.OtherCardViewer($"Enemy Row {i + 1}", background.transform, cc);
                enemyLanes[i].transform.localPosition = new Vector3(0.47f, 0.26f - 0.43f * i, 0);
                enemyLanes[i].gap = new Vector3(1f, 0, 0);
                enemyLanes[i].SetSize(3, 0.6667f);
                enemyLanes[i].gameObject.AddComponent<UINavigationItem>();
                cc.hoverEvent.AddListener(enemyLanes[i].Hover);
                cc.unHoverEvent.AddListener(enemyLanes[i].Unhover);
                enemyLanes[i].AssignController(cb);
            }
            background.SetActive(false);
        }

        //BattleViewer should not be opened when interesting things can happen.
        private void OpenBattleViewer(Friend friend)
        {
            if (!ActionQueue.Empty)
            {
                return;
            }
            if (Battle.instance != null)
            {
                if (Battle.instance.phase == Battle.Phase.Battle || References.Player.endTurn)
                {
                    return;
                }
                SetOwners(References.Player, Battle.GetOpponent(References.Player));
                background.transform.SetParent(GameObject.Find("Battle/Canvas/CardController/Board/Canvas").transform);
                background.transform.localScale = new Vector3(10f, 10f, 1);
                cb.owner = References.Player;
                cb.useOnHandAnchor = ((CardControllerBattle)References.Battle.playerCardController).useOnHandAnchor;
                AddRowsToBattle();
            }
            else
            {
                SetOwners(HandlerSystem.playerDummy, HandlerSystem.enemyDummy);
                background.transform.localScale = new Vector3(3.5f, 3.5f, 1);
            }
            Clear();
            AskForData(friend);
            background.transform.localPosition = defaultPosition;
            background.SetActive(true);
            LeanTween.moveLocal(background, viewerPosition, 0.75f).setEase(LeanTweenType.easeInOutQuart);
            //StartCoroutine(PopulateRows());
            HandlerBattle.friend = friend;
            InvokeOnBattleViewerOpen(friend);
    }

        public void CloseBattleViewer()
        {
            if (Battle.instance != null)
            {
                if (!(NavigationState.PeekCurrentState() is NavigationStateBattle))
                {
                    return;
                }
                RemoveRowsFromBattle();
            }
            background.SetActive(false);
            background.transform.SetParent(transform);
            Clear();
            InvokeOnBattleViewerClose(friend);
        }

        public void ToggleViewer(Friend friend)
        {
            if (background == null)
            {
                CreateBattleViewer();
            }
            if (Blocking)
            {
                CloseBattleViewer();
            }
            else
            {
                OpenBattleViewer(friend);
            }
        }

        public void AddRowsToBattle()
        {
            
            if (!Battle.instance.rows[References.Player].Contains(playerLanes[0]))
            {
                Battle.instance.rows[References.Player].AddRange(playerLanes.Select((row) => (CardContainer)row));
                Battle.instance.rows[Battle.GetOpponent(References.Player)].AddRange(enemyLanes.Select((row) => (CardContainer)row));
            }
            Battle.instance.playerCardController.enabled = false;
            References.Player.handContainer.AssignController(cb);
        }

        private void SetOwners(Character playerOwner, Character enemyOwner)
        {
            foreach(OtherCardViewer ocv in playerLanes)
            {
                ocv.owner = playerOwner;
            }
            foreach(OtherCardViewer ocv in  enemyLanes)
            {
                ocv.owner = enemyOwner;
            }
        }

        public void RemoveRowsFromBattle()
        {

            foreach(CardContainer lane in playerLanes)
            {
                Battle.instance.rows[References.Player].Remove(lane);
            }
            foreach (CardContainer lane in enemyLanes)
            {
                Battle.instance.rows[Battle.GetOpponent(References.Player)].Remove(lane);
            }
            Battle.instance.playerCardController.enabled = true;
            References.Player.handContainer.AssignController(References.Battle.playerCardController);
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
                                //s = $"PLAYER!{j}!" + HandlerInspect.EncodeEntity(entities[k], entities[k].data.id);
                                s = $"PLAYER!{j}!" + CardEncoder.Encode(entities[k], entities[k].data.id);

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
                                //s = $"ENEMY!{j}!" + HandlerInspect.EncodeEntity(entities[k], entities[k].data.id);
                                s = $"ENEMY!{j}!" + CardEncoder.Encode(entities[k], entities[k].data.id);
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
                case "PLAY":
                    if(Battle.instance != null)
                    {
                        StartCoroutine(PlayCard(friend, messages));
                    }
                    break;
            }
        }

        public IEnumerator PlayCard(Friend friend, string[] messages)
        {
            string[] targets = messages[1].Split(' ');
            Debug.Log($"[Multiplayer] {targets[0]}");
            PlayAction action = null;
            switch (targets[0])
            {
                case "NON":
                    action = new ActionPlayOtherCard(messages.Skip(3).ToArray(), friend, null, null);
                    break;
                case "ENT":
                    ulong id = ulong.Parse(targets[1]);
                    foreach (Entity entity in UnityEngine.Object.FindObjectsOfType<Entity>())
                    {
                        if (entity?.data?.id == id && Battle.IsOnBoard(entity))
                        {
                            action = new ActionPlayOtherCard(messages.Skip(3).ToArray(), friend, entity, null);
                            break;
                        }
                    }
                    break;
                case "ROW":
                    id = ulong.Parse(targets[1]);
                    CardContainer container = FindContainerID(id);
                    if (container != null)
                    {
                        action = new ActionPlayOtherCard(messages.Skip(3).ToArray(), friend, null, container);
                    }
                    break;
            }
            if (action != null)
            {
                yield return new WaitUntil(() => !Blocking);
                ActionQueue.Add(action);
            }
            yield break;
        }

        public CardContainer FindContainerID(ulong id)
        {
            switch (id)
            {
                case 0:
                    return References.Battle.GetRow(References.Player,0);
                case 1:
                    return References.Battle.GetRow(References.Player, 1);
                case 2:
                    return References.Battle.GetRow(Battle.GetOpponent(References.Player), 0);
                case 3:
                    return References.Battle.GetRow(Battle.GetOpponent(References.Player), 1);
            }
            return null;
        }

        public ulong ConvertToID(CardContainer container)
        {
            if (container == playerLanes[0])
            {
                return 0;
            }
            else if (container == playerLanes[1])
            {
                return 1;
            }
            else if (container == enemyLanes[0])
            {
                return 2;
            }
            else if (container == enemyLanes[1])
            {
                return 3;
            }
            return 42;
        }

        public IEnumerator PlacePlayerCard(Friend friend, string[] messages)
        {
            OtherCardViewer ocv = playerLanes[int.Parse(messages[1])];
            Entity entity = CardEncoder.DecodeEntity1(cb, ocv.owner, messages.Skip(3).ToArray());
            ocv.Add(entity,friend,ulong.Parse(messages[2]));
            ocv.SetChildPosition(entity);
            yield return CardEncoder.DecodeEntity2(entity, messages.Skip(3).ToArray());
            entity.flipper.FlipUp(force: true);
        }

        public IEnumerator PlaceEnemyCard(Friend friend, string[] messages)
        {
            OtherCardViewer ocv = enemyLanes[int.Parse(messages[1])];
            Entity entity = CardEncoder.DecodeEntity1(cb, ocv.owner, messages.Skip(3).ToArray());
            ocv.Add(entity, friend, ulong.Parse(messages[2]));
            ocv.SetChildPosition(entity);
            yield return CardEncoder.DecodeEntity2(entity, messages.Skip(3).ToArray());
            entity.flipper.FlipUp(force: true);
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

        public static List<CardContainer> GetContainers()
        {
            return instance.playerLanes.Concat(instance.enemyLanes).Select((a) => (CardContainer)a).ToList();
        }

        public static void InvokeOnBattleViewerOpen(Friend friend)
        {
            OnBattleViewerOpen?.Invoke(friend);
        }

        public static void InvokeOnBattleViewerClose(Friend friend)
        {
            OnBattleViewerClose?.Invoke(friend);
        }

        public static void InvokeOnPlayOtherCard(Friend friend, Entity entity)
        {
            OnPlayOtherCard?.Invoke(friend, entity);
        }

        public static void InvokeOnPostPlayOtherCard(Friend friend, Entity entity)
        {
            OnPostPlayOtherCard?.Invoke(friend, entity);
        }

        public static void InvokeOnSendCardToPlay(Friend friend, Entity entity)
        {
            OnSendCardToPlay?.Invoke(friend, entity);
        }

        public static void InvokeOnPostSendCardToPlay(Friend friend, Entity entity)
        {
            OnPostSendCardToPlay?.Invoke(friend, entity);
        }
    }
}
