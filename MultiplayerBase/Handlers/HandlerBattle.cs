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
using HarmonyLib;

namespace MultiplayerBase.Handlers
{
    public class HandlerBattle : MonoBehaviour
    {

        public static UnityAction<Friend> OnFetch; //Probably unused?

        public static HandlerBattle instance;
        public static readonly List<PlayAction> actions = new List<PlayAction>();
        public static Friend? friend = null;

        public static List<Friend> watchers = new List<Friend>();

        GameObject background;
        Fader fader; //Some component attached to background that I can attach coroutines to :)

        //Use Blcoking for your PlayActions
        public bool Blocking => background != null && background.activeSelf;

        internal bool ignoreFurtherMessages;

        private CardControllerSelectCard cc => HandlerInspect.instance.cc;
        private CardControllerBattle cb;

        public CardControllerBattle CB => cb;

        int lanes = 2;
        internal OtherCardViewer[] playerLanes;
        internal OtherCardViewer[] enemyLanes;

        internal OtherCardViewer invisContainer;

        Vector3 defaultPosition = new Vector3(0, 0, -8f);
        Vector3 viewerPosition = new Vector3(0, 0, 2);

        //static Button refreshButton;
        //static Button fetchButton;

        MarkerManager marks;
        public int updateTasks = 0;


        #region INIT
        protected void OnEnable()
        {
            Events.OnEntityMove += EntityMove;
            Events.OnEntityKilled += EntityKilled;
            Events.OnEntityPreTrigger += EntityTrigger;
            Events.OnBattlePreTurnStart += PreTurn;
        }

        protected void OnDisable()
        {
            if (Blocking)
            {
                CloseBattleViewer();
            }
            Events.OnEntityMove -= EntityMove;
            Events.OnEntityKilled -= EntityKilled;
            Events.OnEntityPreTrigger -= EntityTrigger;
            Events.OnBattlePreTurnStart -= PreTurn;
        }

        protected void Awake()
        {

            //Events.OnSceneUnload += DisableController;
            
            instance = this;

            //refreshButton = Dashboard.buttons[1];
            //refreshButton.onClick.AddListener(QueueActions);

            //fetchButton = Dashboard.buttons[2];
            //fetchButton.onClick.AddListener(Fetch);

            marks = gameObject.AddComponent<MarkerManager>();

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
        #endregion INIT 

        public void HandleMessage(Friend friend, string message)
        {
            string[] messages = HandlerSystem.DecodeMessages(message);
            Debug.Log($"[Multiplayer] {message}");

            switch (messages[0])
            {
                case "ASK":
                    SendData(friend, messages);
                    return;
                case "PLAY":
                    PlayCard(friend, messages);
                    break;
                case "UNSUB":
                    watchers.Remove(friend);
                    break;
            }

            if (!Blocking || ignoreFurtherMessages || friend.Id != HandlerBattle.friend?.Id) { return; }

            updateTasks++;

            switch (messages[0])//0 -> Action
            {

                //   "ASK"
                //  See Above.
                case "ENEMY":
                    fader.StartCoroutine(PlaceCard(friend, messages, enemyLanes));
                    break;
                case "PLAYER":
                    fader.StartCoroutine(PlaceCard(friend, messages, playerLanes));
                    break;
                case "BOARD":
                    fader.StartCoroutine(UpdateBoard(friend, messages));
                    break;
                case "MARK":
                    fader.StartCoroutine(MarkCard(friend, messages)); //Not really an IEnumerator
                    break;
                case "UPDATE":
                    fader.StartCoroutine(UpdateCard(friend, messages));
                    break;
                default:
                    updateTasks--;
                    break;

            }
        }

        public bool Queue(PlayAction p)
        {
            if (Battle.instance == null || Battle.instance.phase != Battle.Phase.End)
            {
                return false;
            }

            ActionQueue.Add(p);
            return true;
        }

        #region REALTIME UPDATES
        private void PreTurn(int _)
        {
            SendCardUpdates();
            SendBoardPositions("ENEMY");
            SendBoardPositions("PLAYER");
        }

        private void SendCardUpdates()
        {
            if (Blocking)
            {
                return;
            }

            List<string> list = SendCards("UPDATE! PLAYER", References.Player);
            for(int i=0; i<list.Count; i++)
            {
                foreach(Friend friend in watchers)
                {
                    HandlerSystem.SendMessage("BAT", friend, list[i]);
                }
            }

            list = SendCards("UPDATE! ENEMY", Battle.GetOpponent(References.Player));
            for (int i = 0; i < list.Count; i++)
            {
                foreach (Friend friend in watchers)
                {
                    HandlerSystem.SendMessage("BAT", friend, list[i]);
                }
            }
        }

        private void EntityTrigger(ref Trigger trigger)
        {
            if (trigger?.entity != null)
            {
                CreateEffect(trigger.entity, trigger.entity.containers, "counter down");
            }
        }

        private void EntityKilled(Entity entity, DeathType _)
        {
            CreateEffect(entity, entity.preContainers, "death");
        }

        public void CreateEffect(Entity entity, CardContainer[] containers, string type)
        {
            if (containers == null || containers.Length == 0 || Battle.instance == null || !Battle.IsOnBoard(containers[0]) || watchers.Count == 0)
            {
                return;
            }

            string side = (containers[0].owner == References.Player) ? "PLAYER" : "ENEMY";
            string message = HandlerSystem.ConcatMessage(true, "MARK", side, entity.data.id.ToString(), type);

            foreach (Friend friend in watchers)
            {
                HandlerSystem.SendMessage("BAT", friend, message);
            }
        }

        private void EntityMove(Entity entity)
        {
            if (Blocking || Battle.instance == null || watchers.Count == 0) { return; }

            bool flag = false;

            foreach(CardContainer container in entity.preContainers)
            {
                if (Battle.IsOnBoard(container))
                {
                    flag = true;
                    break;
                }
            }
            foreach (CardContainer container in entity.containers)
            {
                if (Battle.IsOnBoard(container))
                {
                    flag = true;
                    break;
                }
            }
            if (flag)
            {
                string side = (entity.owner == References.Player) ? "PLAYER" : "ENEMY";
                StartCoroutine(DelaySend(delay, side));
            }

        }

        public static float delay = 0.1f;

        private IEnumerator DelaySend(float f, string s)
        {
            yield return new WaitForSeconds(f);
            SendBoardPositions(s);
        }

        private void SendBoardPositions(string side)
        {
            List<string> positions = new List<string>();
            List<CardSlotLane> lanes = null;
            switch(side)
            {
                case "PLAYER":
                    lanes = Battle.instance.GetRows(References.Player).GetRange(0,2).Cast<CardSlotLane>().ToList();
                    break;
                case "ENEMY":
                    lanes = Battle.instance.GetRows(Battle.GetOpponent(References.Player)).GetRange(0, 2).Cast<CardSlotLane>().ToList();
                    break;
                default:
                    return;
            }

            List<List<CardSlot>> slots = lanes.Select((l) => l.slots).ToList();
            for (int i = 0; i < Math.Max(slots[0].Count, slots[1].Count); i++)
            {
                for (int j=0; j<2; j++)
                {
                    if (i < slots[j].Count && slots[j][i].Count != 0)
                    {
                        positions.Add(slots[j][i][0].data.id.ToString());
                    }
                    else
                    {
                        positions.Add("");
                    }
                }
            }

            string s = HandlerSystem.ConcatMessage(true, positions.ToArray());
            s = HandlerSystem.ConcatMessage(false, "BOARD", side, s);

            foreach(Friend friend in watchers)
            {
                HandlerSystem.SendMessage("BAT", friend, s);
            }
        }
        #endregion REALTIME UPDATES

        #region BATTLEVIEWER OBJECTS
        private void CreateBattleViewer()
        {
            background = HelperUI.Background(transform, new Color(1f, 1f, 1f, 0.75f));
            background.SetActive(false);
            fader = background.AddComponent<Fader>();
            fader.onEnable = true;
            fader.gradient = new Gradient();
            fader.ease = LeanTweenType.easeOutQuad;
            GradientColorKey[] colors = new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            };
            GradientAlphaKey[] alphas = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.75f, 1f)
            };
            fader.gradient.SetKeys(colors, alphas);

            cb = background.AddComponent<CardControllerMultiplayerBattle>();
            playerLanes = new OtherCardViewer[lanes];
            for (int i = 0; i < playerLanes.Length; i++)
            {
                playerLanes[i] = HelperUI.OtherCardViewer($"Other Player Row {i + 1}", background.transform, cc);
                playerLanes[i].transform.localPosition = new Vector3(-0.47f, 0.26f - 0.43f * i, 0);
                playerLanes[i].BattleCardViewer = true;
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
                enemyLanes[i] = HelperUI.OtherCardViewer($"Other Enemy Row {i + 1}", background.transform, cc);
                enemyLanes[i].transform.localPosition = new Vector3(0.47f, 0.26f - 0.43f * i, 0);
                enemyLanes[i].BattleCardViewer = true;
                enemyLanes[i].gap = new Vector3(1f, 0, 0);
                enemyLanes[i].SetSize(3, 0.6667f);
                enemyLanes[i].gameObject.AddComponent<UINavigationItem>();
                cc.hoverEvent.AddListener(enemyLanes[i].Hover);
                cc.unHoverEvent.AddListener(enemyLanes[i].Unhover);
                enemyLanes[i].AssignController(cb);
            }

            invisContainer = HelperUI.OtherCardViewer($"Invis Row", background.transform, cc);
            invisContainer.transform.localPosition = new Vector3(0f, 10f, 0);
            invisContainer.BattleCardViewer = true;
            invisContainer.dir = 1;
            invisContainer.gap = new Vector3(1f, 0, 0);
            invisContainer.SetSize(3, 0.6667f);
            invisContainer.AssignController(cb);
        }

        //BattleViewer should not be opened when interesting things can happen.
        private void OpenBattleViewer(Friend friend)
        {
            if (!ActionQueue.Empty)
            {
                return;
            }
            ignoreFurtherMessages = false;
            updateTasks = 0;
            if (Battle.instance != null)
            {
                if (Battle.instance.phase == Battle.Phase.Battle || References.Player.endTurn)
                {
                    return;
                }
                foreach(Entity entity in Battle.GetCardsOnBoard(References.Player))
                {
                    entity.silenceCount++;
                }
                foreach (Entity entity in Battle.GetCardsOnBoard(Battle.GetOpponent(References.Player)))
                {
                    entity.silenceCount++;
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
                background.transform.localScale = new Vector3(10f, 10f, 1);
                background.transform.position = new Vector3(0, 1.5f, 2.7f);
            }
            Clear();
            AskForData(friend);
            
            background.SetActive(true);

            if (Battle.instance != null)
            {
                background.transform.localPosition = defaultPosition;
                LeanTween.moveLocal(background, viewerPosition, 0.75f).setEase(LeanTweenType.easeInOutQuart);
            }

            for (int i = 0; i < 2; i++)
            {
                playerLanes[i].transform.localPosition = new Vector3(-0.47f, 0.26f - 0.43f * i, 0);
                enemyLanes[i].transform.localPosition = new Vector3(0.47f, 0.26f - 0.43f * i, 0);
            }

            ActionQueue.Stack(new ActionBattleViewer());
            //StartCoroutine(PopulateRows());
            HandlerBattle.friend = friend;
            MultEvents.InvokeBattleViewerOpen(friend);
        }

        public void CloseBattleViewer()
        {
            if (ignoreFurtherMessages)
            {
                return;
            }

            if (Battle.instance != null)
            {
                if (!(NavigationState.PeekCurrentState() is NavigationStateBattle))
                {
                    return;
                }
                foreach (Entity entity in Battle.GetCardsOnBoard(References.Player))
                {
                    entity.silenceCount--;
                }
                foreach (Entity entity in Battle.GetCardsOnBoard(Battle.GetOpponent(References.Player)))
                {
                    entity.silenceCount--;
                }
                RemoveRowsFromBattle();
            }
            ignoreFurtherMessages = true;
            if (friend is Friend f)
            {
                HandlerSystem.SendMessage("BAT", f, "UNSUB");
                MultEvents.InvokeBattleViewerClose(f);
            }

            fader.StartCoroutine(FadeOut());
        }

        internal IEnumerator FadeOut()
        {
            marks.ClearMarkers("PLAYER");
            marks.ClearMarkers("ENEMY");
            fader.Out(0.45f);
            for (int i=0; i<2; i++)
            {
                LeanTween.moveLocalY(playerLanes[i].gameObject, 1f, 0.45f).setEaseOutCubic();
                LeanTween.moveLocalY(enemyLanes[i].gameObject, 1f, 0.45f).setEaseOutCubic();
            }

            yield return Sequences.Wait(0.5f);

            background.GetComponent<Fader>().StopAllCoroutines();
            background.SetActive(false);
            background.transform.SetParent(transform);
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
            invisContainer.owner = playerOwner;
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
            invisContainer.ClearAndDestroyAllImmediately();
        }

        #endregion BATTLEVIEWER OBJECTS

        public void AskForData(Friend friend)
        {
            HandlerSystem.SendMessage("BAT", friend, HandlerSystem.ConcatMessage(false, "ASK", "PLAYER", "ENEMY"));
        }

        //Bat|Player!rowIndex!index!id!cardStuff
        public void SendData(Friend friend, string[] messages)
        {
            if (!watchers.Contains(friend))
            {
                watchers.Add(friend);
            }
            string s;
            for (int i = 1; i < messages.Length; i++)
            {
                s = $"";
                switch (messages[i])
                {
                    case "PLAYER":
                        List<string> list = SendCards("PLAYER", References.Player);
                        for(int j=0; j<list.Count; j++)
                        {
                            HandlerSystem.SendMessage("BAT", friend, list[j]);
                        }
                        break;
                    case "ENEMY":
                        list = SendCards("ENEMY", Battle.GetOpponent(References.Player));
                        for (int j = 0; j < list.Count; j++)
                        {
                            HandlerSystem.SendMessage("BAT", friend, list[j]);
                        }
                        break;
                    case "SINGLE":
                        for (int j = 0; j < 2; j++)
                        {
                            if (Battle.instance.rows[References.Player][j] is CardSlotLane lane)
                            {
                                List<CardSlot> slots = lane.slots;
                                for (int k = 0; k < slots.Count; k++)
                                {
                                    if (slots[k].Count != 0)
                                    {
                                        Entity entity = slots[k][0];
                                        if (entity != null && i + 1 < messages.Length && ulong.TryParse(messages[i+1],out ulong result) && entity.data.id == result)
                                        {
                                            s = HandlerSystem.ConcatMessage(false, "UPDATE", "PLAYER", $"{j}", $"{k}", entity.data.id.ToString(), CardEncoder.Encode(entity));
                                            HandlerSystem.SendMessage("BAT", friend, s);
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                        for (int j = 0; j < 2; j++)
                        {
                            if (Battle.instance.rows[Battle.GetOpponent(References.Player)][j] is CardSlotLane lane)
                            {
                                List<CardSlot> slots = lane.slots;
                                for (int k = 0; k < slots.Count; k++)
                                {
                                    Entity entity = slots[k][0];
                                    if (entity != null && i + 1 < messages.Length && ulong.TryParse(messages[i + 1], out ulong result) && entity.data.id == result)
                                    {
                                        s = HandlerSystem.ConcatMessage(false, "UPDATE", "ENEMY", $"{j}", $"{k}", entity.data.id.ToString(), CardEncoder.Encode(entity));
                                        HandlerSystem.SendMessage("BAT", friend, s);
                                        return;
                                    }
                                }
                            }
                        }
                        return;
                    case "INFO":
                        OnFetch?.Invoke(friend);
                        break;
                    default:
                        break;
                }
            }
        }

        private List<string> SendCards(string prefix, Character owner)
        {
            List<string> strings = new List<string>();
            for (int j = 0; j < 2; j++)
            {
                if (Battle.instance.rows[owner][j] is CardSlotLane lane)
                {
                    List<CardSlot> slots = lane.slots;
                    for (int k = 0; k < slots.Count; k++)
                    {
                        if (slots[k].Count != 0)
                        {
                            Entity entity = slots[k][0];
                            strings.Add(HandlerSystem.ConcatMessage(false, prefix, $"{j}", $"{k}", entity.data.id.ToString(), CardEncoder.Encode(entity)));
                        }
                    }
                }
            }
            return strings;
        }

        //MARK! PLAYER/ENEMY! ID! TYPE
        public IEnumerator MarkCard(Friend friend, string[] messages)
        {
            updateTasks--;
            if (!DetermineSide(messages[1], out OtherCardViewer[] ocvs) || !ulong.TryParse(messages[2], out ulong id))
            {
                yield break;
            }

            foreach(OtherCardViewer ocv in ocvs)
            {
                Entity entity = ocv.Find(friend, id);
                if (entity != null)
                {
                    marks.CreateMarker(messages[1], entity.transform.position, messages[3]);
                    yield break;
                }
            }
        }
        
        public bool DetermineSide(string side, out OtherCardViewer[] ocvs)
        {
            ocvs = null;
            switch(side)
            {
                case "PLAYER":
                    ocvs = playerLanes;
                    return true;
                case "ENEMY":
                    ocvs = enemyLanes;
                    return true;
            }
            return false;
        }

        //BOARD! PLAYER/ENEMY! POS1! POS2! POS3! POS4! ...
        public IEnumerator UpdateBoard(Friend friend, string[] messages)
        {
            if (!DetermineSide(messages[1], out OtherCardViewer[] ocvs))
            {
                updateTasks--;
                yield break;
            }

            marks.ClearMarkers(messages[1]);

            Dictionary<ulong, Entity> dictionary = CollectCardsFromOCVs(ocvs);
            InspectSystem inspect = GameObject.FindObjectOfType<InspectSystem>();

            List<Entity> placed = new List<Entity>();
            for (int i = 2; i<messages.Length; i++)
            {
                if(ulong.TryParse(messages[i], out ulong result))
                {
                    int laneIndex = i % 2;
                    int index = (i - laneIndex - 2) / 2;
                    if (dictionary.ContainsKey(result))
                    {
                        Debug.Log($"[Multiplayer] Contains Key {result}");
                        ocvs[laneIndex].Insert(index, dictionary[result], friend, result);
                        /*if (dictionary[result].height == 2)
                        {
                            ocvs[1].Insert(index, dictionary[result], friend, result);
                            i++;
                        }*/
                        if (inspect?.inspect != dictionary[result])
                        {
                            ocvs[laneIndex].TweenChildPosition(dictionary[result]);
                        }
                        placed.Add(dictionary[result]);
                        //dictionary.Remove(result);
                    }
                    else
                    {
                        HandlerSystem.SendMessage("BAT", friend, HandlerSystem.ConcatMessage(true, "ASK", "SINGLE", result.ToString()));
                    }
                }
            }

            List<Entity> entities = dictionary.Values.ToList();
            for(int i = entities.Count - 1; i>=0; i--)
            {
                if (placed.Contains(entities[i]))
                {
                    continue;
                }
                if (inspect != null && inspect.inspect ==  entities[i])
                {
                    entities[i].actualContainers.Clear();
                    entities[i].actualContainers.Add(invisContainer);
                }
                else
                {
                    CardManager.ReturnToPool(entities[i]);
                }
            }
            updateTasks--;
        }

        public Dictionary<ulong, Entity> CollectCardsFromOCVs(OtherCardViewer[] ocvs)
        {
            Dictionary<ulong, Entity> dictionary = new Dictionary<ulong, Entity>();
            foreach(OtherCardViewer ocv in ocvs)
            {
                for(int i = ocv.Count-1; i>=0; i--)
                {
                    Entity entity = ocv[i];
                    if (entity != null)
                    {
                        (Friend, ulong) pair = ocv.Find(entity);
                        dictionary[pair.Item2] = entity;
                    }
                    ocv.Remove(entity);
                }
            }
            return dictionary;
        }

        //PLAY! TargetMode Target! id! {Entity}
        public void PlayCard(Friend friend, string[] messages)
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
                case "SLT":
                    id = ulong.Parse(targets[1]);
                    if (FindContainerID(id) is CardSlotLane lane)
                    {
                        int position = int.Parse(targets[2]);
                        if (position >= 0 && position < lane.slots.Count)
                        {
                            container = lane.slots[position];
                            action = new ActionPlayOtherCard(messages.Skip(3).ToArray(), friend, null, container);
                        }
                        else
                        {
                            container = lane.slots[lane.slots.Count - 1];
                            action = new ActionPlayOtherCard(messages.Skip(3).ToArray(), friend, null, container);
                        }
                             
                    }
                    break;
            }
            if (action != null)
            {
                Queue(action);
            }
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

        public IEnumerator PlaceCard(Friend friend, string[] messages, OtherCardViewer[] ocvs)
        {
            if (AlreadyCreated(friend, ulong.Parse(messages[3]), ocvs))
            {
                updateTasks--;
                yield break;
            }
            OtherCardViewer ocv = ocvs[int.Parse(messages[1])];
            Entity entity = CardEncoder.DecodeEntity1(cb, ocv.owner, messages.Skip(4).ToArray());
            ocv.Insert(int.Parse(messages[2]), entity, friend, ulong.Parse(messages[3]));
            if (entity.height > 1)
            {
                ocvs[1].Insert(int.Parse(messages[2]), entity, friend, ulong.Parse(messages[3]));
            }
            ocv.SetChildPosition(entity);
            yield return CardEncoder.DecodeEntity2(entity, messages.Skip(4).ToArray());
            entity.flipper.FlipUp(force: true);
            updateTasks--;
        }

        //UPDATE! PLAYER/ENEMY! rowIndex! index! id! otherCardStuff
        public IEnumerator UpdateCard(Friend friend, string[] messages)
        {
            if (!DetermineSide(messages[1], out OtherCardViewer[] ocvs))
            {
                updateTasks--;
                yield break;
            }

            Entity entity = AlreadyCreated(friend, ulong.Parse(messages[4]), ocvs);
            if (entity == null)
            {
                yield return PlaceCard(friend, messages.Skip(1).ToArray(), ocvs);
                //Events.InvokeEntityCreated(entity);
                updateTasks--;
                yield break;
            }

            CardEncoder.DecodeData(messages.Skip(5).ToArray(), entity.data);
            yield return CardEncoder.DecodeEntity2(entity, messages.Skip(5).ToArray());
            entity.owner = ocvs[0].owner;
            entity.PromptUpdate();
            //entity.flipper.FlipUp(force: true);
            updateTasks--;
        }

        public Entity AlreadyCreated(Friend friend, ulong id, OtherCardViewer[] rows)
        {
            Entity entity = rows[0].Find(friend, id) ?? rows[1].Find(friend, id);
            return entity;
        }

        public IEnumerator BattleRoutine()
        {

            Battle.Phase phase = Battle.instance.phase;
            while (true)
            {
                yield return new WaitUntil(() => (Battle.instance == null || phase != Battle.instance.phase));
                if (Battle.instance == null)
                {
                    watchers.Clear();
                    yield break;
                }
                //Debug.Log("[Battle Handler] Phase Change: " + Battle.instance.phase);
                /*
                phase = Battle.instance.phase;
                switch (phase)
                {
                    case Battle.Phase.Battle:
                        break;
                    case Battle.Phase.Play:
                        break;
                    case Battle.Phase.End:

                        break;
                }
                */
            }
        }
        

        public static Vector3 FindPositionForBosses(OtherCardViewer viewer, Entity entity)
        {
            if (instance == null) { return Vector3.zero; }

            if (instance.playerLanes.Contains(viewer))
            {
                Vector3 first = instance.playerLanes[0].GetChildPosition(entity) + instance.playerLanes[0].transform.position;
                Vector3 second = instance.playerLanes[1].GetChildPosition(entity) + instance.playerLanes[1].transform.position;
                return (first + second) / 2;
            }

            if (instance.enemyLanes.Contains(viewer))
            {
                Vector3 first = instance.enemyLanes[0].GetChildPosition(entity) + instance.enemyLanes[0].transform.position;
                Vector3 second = instance.enemyLanes[1].GetChildPosition(entity) + instance.enemyLanes[1].transform.position;
                return (first + second) / 2;
            }

            return Vector3.zero;
        }

        /*
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
        */

        public static List<CardContainer> GetContainers()
        {
            return instance.playerLanes.Concat(instance.enemyLanes).Select((a) => (CardContainer)a).ToList();
        }

        [HarmonyPatch(typeof(Card), "GetDescription", new Type[]
        {
            typeof(Entity)
        })]
        class PatchSuppressEffects
        {
            static void Prefix(Entity entity, ref bool __state)
            {
                if (entity.silenceCount >= 100)
                {
                    entity.silenceCount -= 100;
                    __state = true;
                }
            }

            static void Postfix(Entity entity, bool __state)
            {
                if (__state)
                {
                    entity.silenceCount += 100;
                }
            }
        }

    }

    
}
