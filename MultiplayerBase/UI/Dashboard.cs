using JetBrains.Annotations;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using MultiplayerBase.Handlers;
using UnityEngine.SceneManagement;
using TMPro;
using Image = UnityEngine.UI.Image;

namespace MultiplayerBase.UI
{
    /* The Dashbaord class deals with managing the icons of other players (see the FriendIcon class).
     * 
     * Useful methods:
     * - SelectFriend
     */
    public class Dashboard : MonoBehaviour
    {
        public static Dashboard instance;

        GameObject background;
        Vector3 buttonPosition = new Vector3(7, 5, 0);
        Vector3 friendIconPosition = new Vector3(-10, 4.5f, 0);
        public static GameObject buttonGroup;
        public static List<Button> buttons = new List<Button>();
        public static GameObject friendIconGroup;
        public static bool friendsHidden = false;
        public static Button visibleButton;
        public static Dictionary<Friend, FriendIcon> friendIcons = new Dictionary<Friend, FriendIcon>();

        //public List<TextMeshProUGUI> texts = new List<TextMeshProUGUI>();

        public float iconSize => MultiplayerMain.instance._iconSize;

        public void Awake()
        {
            instance = this;

            transform.SetParent(GameObject.Find("CameraContainer/CameraMover/MinibossZoomer/CameraPositioner/CameraPointer/Animator/Rumbler/Shaker/InspectSystem").transform);
            background = HelperUI.Background(transform, new Color(0f, 0f, 0f, .75f));
            gameObject.AddComponent<RectTransform>();
            background.SetActive(false);
            gameObject.AddComponent<WorldSpaceCanvasSafeArea>().parent = transform.parent.GetComponent<RectTransform>();
            Fader fader = background.AddComponent<Fader>();
            fader._graphic = background.GetComponent<Image>();
            fader.onEnable = true;
            fader.dur = 0.4f;
            fader.gradient = new Gradient();
            GradientColorKey[] colors = new GradientColorKey[]
            {
                new GradientColorKey(Color.black, 0f),
                new GradientColorKey(Color.black, 1f)
            };
            GradientAlphaKey[] alphas = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.75f, 1f)
            };
            fader.gradient.SetKeys(colors, alphas);


            //float totalSize = HandlerSystem.friends.Length*(1.2f) - 0.2f;
            friendIconGroup = HelperUI.VerticalGroup("Friend Icons", transform, new Vector2(0f, 0f), 0.2f*iconSize);
            friendIconGroup.transform.position = friendIconPosition;
            friendIconGroup.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            friendIconGroup.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0.2f, iconSize);
            visibleButton = HelperUI.ButtonTemplate(friendIconGroup.transform, new Vector2(iconSize, 0.3f), Vector3.zero, "", Color.white);
            visibleButton.transform.SetParent(friendIconGroup.transform, false);
            visibleButton.onClick.AddListener(ToggleVisibility);

            buttonGroup = HelperUI.HorizontalGroup("Friend Actions Icons", transform, new Vector2(0f, 0f));
            buttonGroup.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
            buttonGroup.transform.position = buttonPosition;
            buttonGroup.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 4, 1);
            buttons.Clear();
            buttons.Add(HelperUI.ButtonTemplate(transform, new Vector2(1, 1), Vector3.zero, "DP", Color.gray)); //Display
            buttons[0].transform.SetParent(buttonGroup.transform, false);
            buttons.Add(HelperUI.ButtonTemplate(transform, new Vector2(1, 1), Vector3.zero, "0", Color.gray)); //Update
            buttons[1].transform.SetParent(buttonGroup.transform, false);
            buttons.Add(HelperUI.ButtonTemplate(transform, new Vector2(1, 1), Vector3.zero, "FT", Color.gray)); //Fetch
            buttons[2].transform.SetParent(buttonGroup.transform, false);

            buttonGroup.SetActive(false);
        }

        public void OnEnable()
        {
            friendIconGroup.gameObject.SetActive(true);
            foreach (Friend f in friendIcons.Keys)
            {
                if (!HandlerSystem.friends.Contains(f))
                {
                    friendIcons[f].transform.parent.gameObject.Destroy();
                    friendIcons.Remove(f);
                }
            }
            foreach (Friend friend in HandlerSystem.friends)
            {
                if (!friendIcons.ContainsKey(friend))
                {
                    friendIcons.Add(friend, FriendIcon.Create(transform, iconSize*Vector2.one, Vector3.zero, friend));
                    friendIcons[friend].transform.parent.SetParent(friendIconGroup.transform, false);
                }
            }

            friendIconGroup.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0.2f, iconSize);
            HandlerSystem.Enable();
            Debug.Log("[Multiplayer] Dashboard is ready!");
            StartCoroutine(DelaySend());
        }

        public void ResizeIcons()
        {
            visibleButton.GetComponent<RectTransform>().sizeDelta = new Vector2(iconSize, 0.3f);
            foreach(Friend friend in HandlerSystem.friends)
            {
                if (friendIcons.ContainsKey(friend))
                {
                    RectTransform t = friendIcons[friend].GetComponent<RectTransform>();
                    t.sizeDelta = iconSize * Vector2.one;
                    (t.parent as RectTransform).sizeDelta = iconSize * Vector2.one;

                    Transform text = t.GetChild(0);
                    text.localPosition = new Vector2(iconSize * 0.25f, iconSize * -0.25f);
                    text.localScale = new Vector3(0.5f * iconSize, 0.5f * iconSize, 1);
                }
            }
            friendIconGroup.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0.2f, iconSize);
            if (HandlerInspect.instance != null)
            {
                HandlerInspect.instance.Align();
            }
        }

        public void OnDisable()
        {
            friendIconGroup.gameObject.SetActive(false);
            HandlerSystem.Disable();
            Debug.Log("[Multiplayer] Dashboard is diabled.");
        }

        private IEnumerator DelaySend()
        {
            yield return new WaitForSeconds(1f);
            HandlerSystem.SceneChanged(SceneManager.ActiveSceneName);
            string s = HandlerSystem.ConcatMessage(true, "NICKNAME", friendIcons[HandlerSystem.self].nickname);
            HandlerSystem.SendMessageToAllOthers("MSC", s);
        }

        public static void AddToButtons(Button button)
        {
            buttons.Add(button);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(buttons.Count(), 1);
        }

        public static void ToggleVisibility()
        {
            foreach (Friend friend in HandlerSystem.friends)
            {
                friendIcons[friend].gameObject.SetActive(!friendIcons[friend].gameObject.activeSelf);
            }
            friendsHidden = !friendIcons[HandlerSystem.self].gameObject.activeSelf;
            visibleButton.GetComponent<UnityEngine.UI.Image>().color = friendsHidden ? new Color(0.5f, 0.5f, 0.5f, 0.5f) : Color.white;
        }

        public static Friend? selectedFriend;
        public static bool waitingForSelection = false;

        public static IEnumerator SelectFriend(bool includeSelf = false)
        {
            if (!HandlerSystem.enabled || (!includeSelf && HandlerSystem.friends.Length <= 1))
            {
                yield break;
            }
            selectedFriend = null;
            instance.background.gameObject.SetActive(true);
            GameObject obj = new GameObject("ID Tooltip");
            obj.transform.SetParent(instance.transform, false);
            TextMeshProUGUI textElement = obj.AddComponent<TextMeshProUGUI>();
            textElement.fontSize = 0.6f;
            textElement.horizontalAlignment = HorizontalAlignmentOptions.Center;
            textElement.text = "Select a player";
            textElement.outlineColor = Color.black;
            textElement.outlineWidth = 0.1f;
            textElement.transform.Translate(new Vector3(0, 1, 0));
            obj.GetComponent<RectTransform>().sizeDelta = new Vector2(8f, 2f);
            waitingForSelection = true;
            yield return new WaitUntil(() => selectedFriend is Friend f);
            waitingForSelection = false;
            instance.background.GetComponent<Fader>().Out(0.2f);
            yield return Sequences.Wait(0.2f);
            instance.background.gameObject.SetActive(false);
            obj.Destroy();
        }
    }
}
