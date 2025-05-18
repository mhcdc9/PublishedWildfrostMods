using MultiplayerBase.UI;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Color = UnityEngine.Color;
using Image = UnityEngine.UI.Image;
using UnityEngine.UI;

namespace MultiplayerBase.Matchmaking
{
    internal class LobbyView : MonoBehaviour
    {
        MatchmakingDashboard Dashboard => MatchmakingDashboard.instance;
        static Vector3 defaultPosition = new Vector3(0f, 1.3f, 0f);
        static Vector2 dim = new Vector2(5.5f, 6.5f);
        static Vector2 innerDim = new Vector2(5.3f, 6.3f);

        public int index = -1;
        public Button[] lobbyButtons = new Button[0];

        public int pageIndex = 0;
        public int numberOfPages = 0;

        public GameObject buttonGroup;

        public GameObject navGroup;
        public Button leftButton;
        public Button rightButton;
        public TextMeshProUGUI pageText;

        TweenUI exitTween;
        public static LobbyView Create(Transform transform)
        {
            GameObject obj = new GameObject("Lobby View");
            obj.SetActive(false);
            obj.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);
            obj.GetComponent<RectTransform>().sizeDelta = dim;
            obj.transform.Translate(defaultPosition);
            
            TweenUI tween = obj.AddComponent<TweenUI>();
            tween.target = obj;
            tween.property = TweenUI.Property.Move;
            tween.ease = LeanTweenType.easeOutBounce;
            tween.fireOnEnable = true;
            tween.duration = 0.75f;
            tween.to = defaultPosition;
            tween.hasFrom = true;
            tween.from = defaultPosition + 9*Vector3.up;
            LobbyView lv = obj.AddComponent<LobbyView>();
            lv.transform.SetParent(transform);

            tween = obj.AddComponent<TweenUI>();
            tween.target = obj;
            tween.property = TweenUI.Property.Move;
            tween.ease = LeanTweenType.easeOutQuart;
            tween.disableAfter = true;
            tween.duration = 0.5f;
            tween.to = defaultPosition + 11 * Vector3.up;

            lv.buttonGroup = HelperUI.VerticalGroup("Vertical Group", lv.transform, innerDim, 0.1f);
            lv.buttonGroup.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            lv.buttonGroup.transform.localPosition = new Vector3(0, -0.2f, 0);
            lv.exitTween = tween;

            lv.CreateNavButtons();

            return lv;
        }

        public void CreateNavButtons()
        {
            navGroup = HelperUI.HorizontalGroup("Nav Group", transform, innerDim, 0.1f);
            navGroup.transform.localPosition = new Vector3(0, -2.7f, 0);

            leftButton = HelperUI.ButtonTemplate(navGroup.transform, new Vector2(1.5f, 0.7f), Vector2.zero, "<", Color.white);
            leftButton.onClick.AddListener(PageDown);
            leftButton.transform.AddLayoutElement(new Vector2(1.5f, 0.7f));

            GameObject textObject = new GameObject("Page Number");
            textObject.transform.SetParent(navGroup.transform);
            textObject.transform.AddLayoutElement(new Vector2(1.8f, 0.7f));
            (textObject.transform as RectTransform).sizeDelta = new Vector2(1.8f, 0.7f);
            pageText = textObject.AddComponent<TextMeshProUGUI>();
            pageText.alignment = TextAlignmentOptions.Center;
            pageText.fontSize = 0.5f;

            rightButton = HelperUI.ButtonTemplate(navGroup.transform, new Vector2(1.5f, 0.7f), Vector2.zero, ">", Color.white);
            rightButton.onClick.AddListener(PageUp);
            rightButton.transform.AddLayoutElement(new Vector2(1.5f, 0.7f));


        }

        Vector2 elementDim = new Vector2(5, 0.9f);

        public void CreateLobbyView(Lobby[] lobbies)
        {
            index = -1;
            gameObject.SetActive(true);
            Dashboard.joinLobbyButton.interactable = false;
            for (int i = lobbyButtons.Length - 1; i >= 0; i--)
            {
                lobbyButtons[i].gameObject.Destroy();
            }
            lobbyButtons = new Button[lobbies.Length == 0 ? 5 : (lobbies.Length+4)/5*5];
            for (int i = 0; i < lobbies.Length; i++)
            {
                int j = i;
                lobbyButtons[j] = HelperUI.ButtonTemplate(buttonGroup.transform, elementDim, Vector2.zero, $"{lobbies[j].GetData("name")}", Color.white); //new Vector3(0, 3 - 1.5f * j, 0)
                lobbyButtons[j].transform.AddLayoutElement(elementDim);
                lobbyButtons[j].GetComponentInChildren<TextMeshProUGUI>().fontSize = 0.5f;
                lobbyButtons[j].onClick.AddListener(() => SelectLobby(j));
            }
            for (int i = lobbies.Length; i < lobbyButtons.Length; i++)
            {
                int j = i;
                lobbyButtons[j] = HelperUI.ButtonTemplate(buttonGroup.transform, elementDim, Vector2.zero, i == 0 ? "No Lobbies Found :(" : "____", Color.white);
                lobbyButtons[j].transform.AddLayoutElement(elementDim);
                lobbyButtons[j].GetComponentInChildren<TextMeshProUGUI>().fontSize = 0.5f;
                lobbyButtons[j].interactable = false;
            }
            numberOfPages = lobbyButtons.Length/5;
            GoToPage(0);
            for(int i=0; i<5; i++)
            {
                lobbyButtons[i].gameObject.SetActive(true);
            }
        }

        public void PageUp()
        {
            SfxSystem.OneShot("event:/sfx/ui/menu_click_sub");
            GoToPage(pageIndex+1);
        }

        public void PageDown()
        {
            SfxSystem.OneShot("event:/sfx/ui/menu_click_sub");
            GoToPage(pageIndex-1);
        }

        public void GoToPage(int page)
        {
            pageIndex = page;
            for(int i=0; i<buttonGroup.transform.childCount; i++)
            {
                buttonGroup.transform.GetChild(i).gameObject.SetActive(i / 5 == pageIndex);
            }
            leftButton.interactable = (pageIndex > 0);
            rightButton.interactable = (pageIndex < numberOfPages-1);
            int displayedPage = lobbyButtons.Length == 0 ? 0 : pageIndex+1;
            pageText.text = $"{displayedPage} of {numberOfPages}";
        }

        public void SelectLobby(int newIndex)
        {
            Debug.Log($"[Multiplayer] {newIndex}");
            SfxSystem.OneShot("event:/sfx/ui/menu_click_sub");
            if (index != -1)
            {
                lobbyButtons[index].GetComponent<Image>().color = Color.white;
            }
            if (newIndex == index)
            {
                index = -1;
                Dashboard.ButtonOff(Dashboard.joinLobbyButton);
                return;
            }
            index = newIndex;
            lobbyButtons[index].GetComponent<Image>().color = Color.green;
            Dashboard.modView.OpenModView(MatchmakingDashboard.lobbyList[index], false);
            Dashboard.memberView.OpenMemberView(MatchmakingDashboard.lobbyList[index], false, false);
            Dashboard.ButtonOn(Dashboard.joinLobbyButton);
        }

        public void ExitLobbyView(bool disable = true)
        {
            exitTween.disableAfter = disable;
            exitTween.Fire();
        }
    }
}
