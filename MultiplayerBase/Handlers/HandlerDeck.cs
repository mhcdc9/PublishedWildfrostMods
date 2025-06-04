using HarmonyLib;
using MultiplayerBase.UI;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UIElements;

namespace MultiplayerBase.Handlers
{
    //Canvas/Padding/PlayerDisplay/DeckDisplay/Container/Scroll View/Viewport/Content/ActiveCards/Title
    //Canvas/Padding/PlayerDisplay/DeckDisplay/Container/Scroll View/Viewport/Content/ActiveCards/CompanionGrid
    //Canvas/Padding/PlayerDisplay/DeckDisplay/Container/Scroll View/Viewport/Content/ActiveCards/ItemGrid
    //Canvas/Padding/PlayerDisplay/DeckDisplay/Container/Scroll View/Viewport/Content/ReserveCards/Grid
    //SmoothScrollRect
    public class HandlerDeck : MonoBehaviour
    {
        public static HandlerDeck instance;
        public static DeckDisplaySequence deckDisplay;

        public static Friend? friend = null;

        public static bool active = false;

        public GameObject background;

        private float delay = 0.4f;

        public void Awake()
        {
            instance = this;

            transform.SetParent(GameObject.Find("CameraContainer/CameraMover/MinibossZoomer/CameraPositioner/CameraPointer/Animator/Rumbler/Shaker/InspectSystem").transform, false);
            transform.SetAsFirstSibling();

            background = HelperUI.Background(transform, new Color(0f, 0f, 0f, 0.6f));
            background.SetActive(false);
            Fader fader = background.AddComponent<Fader>();
            fader.onEnable = true;
            fader.gradient = new Gradient();
            fader.ease = LeanTweenType.easeOutQuad;
            GradientColorKey[] colors = new GradientColorKey[]
            {
                new GradientColorKey(Color.black, 0f),
                new GradientColorKey(Color.black, 0.25f)
            };
            GradientAlphaKey[] alphas = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.25f)
            };
            fader.gradient.SetKeys(colors, alphas);

            HandlerSystem.HandlerRoutines["DEC"] = HandleMessage;
        }

        public void OnEnable()
        {

        }

        public void OnDisable()
        {
            CloseDeckViewer();
            background.SetActive(false);
        }

        public IEnumerator Transition(bool clear = true, IEnumerator interlude = null)
        {
            background.SetActive(true);
            yield return Sequences.Wait(0.25f);

            if (clear)
            {
                deckDisplay.activeCardsGroup.Clear();
                deckDisplay.activeCardsGroup.transform.GetAllChildren().Where(t => t.name == "Title")
                    .Do(t =>
                    {
                        TextMeshProUGUI text = t.GetComponent<TextMeshProUGUI>();
                        if (text == null)
                        {
                            return;
                        }
                        if (friend is Friend current)
                        {
                            string s = (friend?.Name?.ToUpper() + "'S DECK");
                            text.SetText(s);
                        }
                        else
                        {
                            text.SetText(t.GetComponent<LocalizeStringEvent>().StringReference.GetLocalizedString());
                        }
                    });
                deckDisplay.reserveCardsGroup.Clear();
                deckDisplay.crownHolder.Clear();
                deckDisplay.charmHolder.Clear();
            }

            yield return (interlude ?? Sequences.Wait(delay));
            deckDisplay.activeCardsGroup.UpdatePositions();
            deckDisplay.reserveCardsGroup.UpdatePositions();
            deckDisplay.crownHolder.SetPositions();
            deckDisplay.charmHolder.SetPositions();
                

            background.GetComponent<Fader>().Out(0.25f);
            yield return Sequences.Wait(0.25f);

            background.SetActive(false);
        }

        public IEnumerator ReturnToNormal()
        {
            if (References.PlayerData?.inventory == null || !DeckDisplayActive())
            {
                yield break;
            }
            Routine.Clump clumpy = new Routine.Clump();
            foreach (CardUpgradeData upgrade in References.PlayerData.inventory.upgrades)
            {
                SetUpgrade(upgrade, true);
            }

            foreach (CardData card in References.PlayerData.inventory.deck)
            {
                clumpy.Add(deckDisplay.activeCardsGroup.CreateCard(card));
            }

            foreach (CardData card in References.PlayerData.inventory.reserve)
            {
                clumpy.Add(deckDisplay.reserveCardsGroup.CreateCard(card));
            }

        }

        private bool listeningForEnd = false;
        public IEnumerator ListenForEnd()
        {
            yield return new WaitUntil(() => !DeckDisplayActive());
            background.SetActive(false);
            CloseDeckViewer();
            
        }

        //UPGRADES! [NAME1]! [NAME2]! ...
        //DECK! [ID1]! [CARD1]! [ID2]! [CARD2]! ...
        //RESERVE! [ID1]! [CARD1]! [ID2]! [CARD2]! ...
        public void SendData(Friend f)
        {
            string upgradeString = "UPGRADES";
            string deckString = "DECK";
            string reserveString = "RESERVE";
            if (References.PlayerData?.inventory != null)
            {
                foreach (CardUpgradeData upgrade in References.PlayerData.inventory.upgrades)
                {
                    if (upgrade != null)
                    {
                        upgradeString = HandlerSystem.AppendTo(upgradeString, upgrade.name);
                    }
                }
                HandlerSystem.SendMessage("DEC", f, upgradeString);

                for(int i=0; i < References.PlayerData.inventory.deck.Count && i < 50; i++)
                {
                    CardData card = References.PlayerData.inventory.deck[i];
                    if (card != null)
                    {
                        string c = HandlerSystem.ConcatMessage(true, card.id.ToString(), CardEncoder.Encode(card));
                        deckString = HandlerSystem.ConcatMessage(false, deckString, c);
                    }
                }
                HandlerSystem.SendMessage("DEC",f, deckString);

                for (int i = 0; i < References.PlayerData.inventory.reserve.Count && i < 50; i++)
                {
                    CardData card = References.PlayerData.inventory.reserve[i];
                    if (card != null)
                    {
                        string c = HandlerSystem.ConcatMessage(true, card.id.ToString(), CardEncoder.Encode(card));
                        reserveString = HandlerSystem.ConcatMessage(false, reserveString, c);
                    }
                }
                HandlerSystem.SendMessage("DEC", f, reserveString);

            }
        }

        public static bool DeckDisplayActive()
        {
            if (deckDisplay == null)
            {
                deckDisplay = FindObjectOfType<DeckDisplaySequence>();
            }
            if (deckDisplay == null || !deckDisplay.isActiveAndEnabled)
            {
                return false;
            }
            return true;
        }

        //ASK
        public void OpenDeckViewer(Friend f)
        {
            if (!DeckDisplayActive() || active)
            {
                return;
            }
            if (friend is Friend current && current.Id != f.Id)
            {
                return;
            }

            StartCoroutine(Transition(true, AskForData(f) ));

            if (!listeningForEnd)
            {
                StartCoroutine(ListenForEnd());
                listeningForEnd = true;
            }

            friend = f;
            active = true;

            //Presumably prevent all of the ways this can go wrong...
        }

        public IEnumerator AskForData(Friend f)
        {
            HandlerSystem.SendMessage("DEC", f, "ASK", "Please wait...");
            yield return Sequences.Wait(delay);
        }

        public void CloseDeckViewer()
        {
            if (!active)
            {
                return;
            }

            StopAllCoroutines();
            active = false;
            friend = null;
            listeningForEnd = false;

            if (DeckDisplayActive())
            {
                StartCoroutine(Transition(true, ReturnToNormal()));
            }
        }

        public void HandleMessage(Friend f, string message)
        {
            Debug.Log($"[Multiplayer] {message}");
            string[] messages = HandlerSystem.DecodeMessages(message);
            if (messages[0] == "ASK")
            {
                SendData(f);
                return;
            }


            if (!DeckDisplayActive())
            {
                return;
            }

            
            switch(messages[0])
            {
                case "UPGRADES":
                    CreateCharms(f, messages);
                    break;
                case "DECK":
                    StartCoroutine(CreateCards(f, messages, deckDisplay.activeCardsGroup));
                    break;
                case "RESERVE":
                    StartCoroutine(CreateCards(f, messages, deckDisplay.reserveCardsGroup));
                    break;
            }
        }

        //DECK! [ID1]! [CARD1]! [ID2]! [CARD2]! ...
        //RESERVE! [ID1]! [CARD1]! [ID2]! [CARD2]! ...
        public IEnumerator CreateCards(Friend f, string[] messages, DeckDisplayGroup group)
        {
            if (friend is Friend current && current.Id != f.Id)
            {
                yield break;
            }

            CardData data;
            Routine.Clump clumpy = new Routine.Clump();
            for (int i = 1; i < messages.Length; i += 2)
            {
                data = CardEncoder.DecodeData(HandlerSystem.DecodeMessages(messages[i + 1]));
                clumpy.Add(group.CreateCard(data));
            }
            yield return clumpy.WaitForEnd();
            group.UpdatePositions();
        }

        //UPGRADES! [NAME1]! [NAME2]! ...
        public void CreateCharms(Friend f, string[] messages)
        {
            if (friend is Friend current && current.Id != f.Id)
            {
                return;
            }

            CardUpgradeData upgrade;
            for(int i = 1; i < messages.Length; i++)
            {
                upgrade = MultiplayerMain.instance.Get<CardUpgradeData>(messages[i]).Clone();
                SetUpgrade(upgrade, false);
            }
            deckDisplay.charmHolder.SetPositions();
            deckDisplay.crownHolder.SetPositions();
        }

        public void SetUpgrade(CardUpgradeData upgrade, bool canDrag)
        {
            if (upgrade == null)
            {
                return;
            }
            switch (upgrade.type)
            {
                case CardUpgradeData.Type.Charm:
                    UpgradeDisplay display = deckDisplay.charmHolder.Create(upgrade);
                    CardCharmInteraction interaction = display.GetComponent<CardCharmInteraction>();
                    if (interaction != null)
                    {
                        interaction.canDrag = canDrag;
                    }
                    break;
                case CardUpgradeData.Type.Crown:
                    display = deckDisplay.crownHolder.Create(upgrade);
                    interaction = display.GetComponent<CardCharmInteraction>();
                    if (interaction != null)
                    {
                        interaction.canDrag = canDrag;
                    }
                    break;
            }
        }
    }
}
