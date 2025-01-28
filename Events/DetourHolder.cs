using Detours.Misc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Detours
{
    internal class DetourHolder : MonoBehaviour
    {
        public static bool stop = false;
        public static DetourHolder instance;

        public static Detour current;

        public static bool skip = false;

        private RectTransform gridGroup;
        private CardContainerGrid grid;

        public static void CreateInstance()
        {
            Debug.LogWarning("[Detours] Starting Examples");
            stop = false;
            GameObject panel = new GameObject("Panel", new Type[] { typeof(Image), typeof(DetourHolder) });
            panel.SetActive(false);
            panel.transform.SetParent(GameObject.Find("Canvas/Padding/HUD/DeckpackLayout").transform);
            panel.transform.SetSiblingIndex(0);
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(30f, 20f);
            panel.GetComponent<Image>().color = new Color(0, 0, 0, 0f);

            instance = panel.GetComponent<DetourHolder>();
            instance.CreateCardGrid(panel);
        }

        public void CreateCardGrid(GameObject panel)
        {
            gridGroup = UI.WithBox(panel.GetComponent<RectTransform>(), Vector3.zero, Vector2.zero, new Color(0,0,0,0.7f));
            gridGroup.gameObject.SetActive(false);
            GameObject gridObject = new GameObject("Card Grid", new Type[] { typeof(RectTransform), typeof(LayoutElement), typeof(UINavigationLayer)});
            gridObject.transform.SetParent(gridGroup, false);
            gridObject.SetActive(false);
            gridObject.GetComponent<RectTransform>().sizeDelta = 10 * Vector2.one;
            CardControllerSelectCard cc = gridObject.AddComponent<CardControllerSelectCard>();
            cc.owner = References.Player;
            cc.pressEvent = new UnityEventEntity();
            cc.hoverEvent = new UnityEventEntity();
            cc.unHoverEvent = new UnityEventEntity();

            grid = gridObject.AddComponent<CardContainerGrid>();
            grid.owner = References.Player;
            grid.holder = gridObject.GetComponent<RectTransform>();
            grid._cc = cc;
            grid.onAdd = new UnityEventEntity();
            grid.onRemove = new UnityEventEntity();

            Scroller scroller = grid.gameObject.AddComponent<Scroller>();
            scroller.bounds = grid.GetComponent<RectTransform>();
            grid.gameObject.AddComponent<ScrollToNavigation>().scroller = scroller;
            grid.gameObject.AddComponent<TouchScroller>().scroller = scroller;
        }

        public static void StartShowCardGrid(CardData[] cards, UnityAction<Entity> callback)
        {
            References.instance.StartCoroutine(ShowCardGrid(cards, callback));
        }

        public static bool waitForGridEnd = false;

        public static IEnumerator ShowCardGrid(CardData[] data, UnityAction<Entity> callback)
        {
            waitForGridEnd = false;
            instance.gridGroup.SetAsLastSibling();
            instance.gridGroup.gameObject.SetActive(true);
            CardContainerGrid grid = instance.grid;
            Routine.Clump clumpy = new Routine.Clump();
            foreach(CardData cardData in data)
            {
                Card card = CardManager.Get(cardData, grid.cc, grid.owner, false, true);
                card.FlipDown();
                grid.Add(card.entity);
                clumpy.Add(card.UpdateData());
            }
            grid.SetSize();
            yield return clumpy.WaitForEnd();
            CardControllerSelectCard cc = grid.cc as CardControllerSelectCard;
            cc.pressEvent.AddListener(callback);
            foreach (Entity entity in grid.entities)
            {
                entity.flipper.FlipUpInstant();
            }
            grid.SetChildPositions();
            grid.gameObject.SetActive(true);

            yield return new WaitUntil(() => waitForGridEnd);

            cc.pressEvent.RemoveListener(callback);
            grid.DestroyAll();
            grid.gameObject.SetActive(false);
            instance.gridGroup.gameObject.SetActive(false);
        }

        public static void HideCardGrid()
        {
            waitForGridEnd = true;
        }

        internal static IEnumerator StartDetour(CampaignNode node, Detour detour, string startFrame = "START")
        {
            instance.gameObject.SetActive(true);
            current = detour;
            Coroutine hide = Campaign.instance.StartCoroutine(instance.HideInDeckView());
            instance.Fade(0.7f, 0.5f);
            yield return Sequences.Wait(0.25f);
            yield return detour.Run(node, startFrame);
            instance.Fade(0f, 0.5f);
            yield return Sequences.Wait(0.25f);
            Campaign.instance.StopCoroutine(hide);
            instance.gameObject.SetActive(false);
        }

        public void Fade(float endAmount, float duration)
        {
            Color from = GetComponent<Image>().color;
            Color to = new Color(from.r,from.g,from.b,endAmount);
            LeanTween.value(gameObject, (c) => GetComponent<Image>().color = c, from, to, duration);
        }

        public IEnumerator HideInDeckView()
        {
            GameObject obj = GameObject.Find("Canvas/Padding/PlayerDisplay/DeckDisplay");
            while (true)
            {
                yield return new WaitUntil(() => obj.activeSelf);
                yield return new WaitForSeconds(0.1f);
                gameObject.SetActive(false);
                yield return new WaitUntil(() => !obj.activeSelf);
                gameObject.SetActive(true);
                if (current is DetourBasic meb)
                {
                    meb.Update();
                }
            }
        }
    }
}
