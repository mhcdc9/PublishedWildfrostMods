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
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

namespace MultiplayerBase.Handlers
{
    public class HandlerChat : MonoBehaviour
    {
        public static HandlerChat instance;

        public GameObject background;
        public TMP_InputField textInput;

        VerticalLayoutGroup messageGroup;
        public List<ChatMessage> messages = new List<ChatMessage>();

        public bool open = false;

        public float campaignPlacement = -2.1f;
        public float defaultPlacement = -4.8f;

        public static float spacing = 0.3f;

        public void Awake()
        {
            instance = this;

            transform.SetParent(GameObject.Find("CameraContainer/CameraMover/MinibossZoomer/CameraPositioner/CameraPointer/Animator/Rumbler/Shaker/InspectSystem").transform, false);
            transform.SetAsFirstSibling();

            gameObject.AddComponent<RectTransform>();
            gameObject.AddComponent<WorldSpaceCanvasSafeArea>().parent = transform.parent.GetComponent<RectTransform>();

            background = HelperUI.Background(transform, new Color(0f, 0f, 0f, 0.6f));
            background.SetActive(false);
            Fader fader = background.AddComponent<Fader>();
            fader.onEnable = true;
            fader.gradient = new Gradient();
            fader.ease = LeanTweenType.easeOutQuad;
            GradientColorKey[] colors = new GradientColorKey[]
            {
                new GradientColorKey(Color.black, 0f),
                new GradientColorKey(Color.black, 1f)
            };
            GradientAlphaKey[] alphas = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.6f, 1f)
            };
            fader.gradient.SetKeys(colors, alphas);

            textInput = HelperUI.WithInputField(transform, new Vector3(0, -5.2f, 0), new Vector2(8.5f, 0.5f), "Press [Enter] to send a message", 0.8f);
            textInput.GetComponent<Image>().color = new Color(0, 0, 0, 1);
            //textInput.GetComponent<RectTransform>().sizeDelta = new Vector2(8f, 0.5f);
            textInput.gameObject.SetActive(false);
            textInput.onDeselect.AddListener(CloseChat);
            textInput.onSubmit.AddListener(SubmitAndCloseChat);
            RectTransform rt = textInput.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0, 0.5f);
            rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0.2f, 9);

            TextMeshProUGUI text = textInput.transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Left;
            text = textInput.transform.GetChild(0).GetChild(2).GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Left;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableWordWrapping = false;

            GameObject messageGroupObj = HelperUI.VerticalGroup("Message Group", transform, Vector2.zero, 0f);
            messageGroupObj.transform.position = new Vector3(0, campaignPlacement, 0);
            //messageGroupObj.SetActive(false);
            messageGroup = messageGroupObj.GetComponent<VerticalLayoutGroup>();
            rt = messageGroupObj.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0, 0.5f);
            rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0.2f, 3);

            HandlerSystem.HandlerRoutines["CHT"] = HandleMessage;

            HandleMessage(HandlerSystem.self, $"<color=#ff8>[{MultiplayerMain.instance._chatKey.ToUpper()}] -> Chat</color>");
            messages[messages.Count - 1].HideFriend();
        }

        public void OnEnable()
        {

        }
        
        public void OnDisable()
        {
            StopAllCoroutines();
            messageGroup.gameObject.SetActive(false);
            background.SetActive(false);
            textInput.gameObject.SetActive(false);
            open = false;
            closing = false;
        }


        public void Update()
        {
            if (Input.GetKeyUp(MultiplayerMain.instance._chatKeyCode) && !Console.active && !InputFieldSelected())
            {
                if (!open)
                {
                    OpenChat();
                }
            }
        }

        public bool InputFieldSelected()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected != null)
            {
                TMP_InputField input = selected.GetComponent<TMP_InputField>();
                if (input != null && input != textInput)
                {
                    return true;
                }
            }
            return false;
        }

        public void OpenChat()
        {
            open = true;
            foreach(ChatMessage cm in messages)
            {
                cm.ChangeOverflow(TextOverflowModes.Overflow);
            }
            //messageGroup.gameObject.SetActive(true);
            background.SetActive(true);
            textInput.gameObject.SetActive(true);
            LeanTween.moveLocalY(messageGroup.gameObject, defaultPlacement + spacing*messages.Count, 0.2f).setEaseInOutQuart().setOnComplete(() =>
            {
                //float difference = spacing*(messages.Count);
                //messageGroup.transform.Translate(0, difference, 0);
                foreach (ChatMessage cm in messages)
                {
                    cm.ChangeOverflow(TextOverflowModes.Overflow);
                    cm.On();
                }
            });
        }

        public bool closing = false;

        public void SubmitAndCloseChat(string s)
        {
            if (!s.IsNullOrEmpty())
            {
                HandlerSystem.SendMessageToAll("CHT", s, false);
                HandleMessage(HandlerSystem.self, s);
                //textInput.Select();
                //return;
            }
            CloseChat(s);
        }

        public void HandleMessage(Friend f, string s)
        {
            ChatMessage cm;
            if (messages.Count < 20) //Add another message
            {
                cm = ChatMessage.Create(messageGroup.transform, f, s);
                messageGroup.transform.Translate(0, spacing, 0);
                if ((!open || closing) && messages.Count >= 5)
                {
                    messages[messages.Count - 5].Off();
                }
            }
            else //Replace the last message
            {
                cm = messages[0];
                cm.On();
                cm.StartFading(20);
                messages.Remove(cm);
                cm.Set(f, s);
                cm.transform.SetAsLastSibling();
                if (!open || closing) //Hide the soon-to-be 6th message
                {
                    messages[14].Off();
                }
            }
            messages.Add(cm);
        }

        public void CloseChat(string s)
        {
            if (closing)
            {
                return;
            }
            closing = true;
            textInput.text = "";

            //int numberVisible = Math.Min(messages.Count, 5);
            Vector3 position = messageGroup.transform.position;
            messageGroup.transform.position = new Vector3(position.x, campaignPlacement + spacing * messages.Count, position.z);
            for (int i = 0; i<messages.Count; i++)
            {
                messages[i].ChangeOverflow(TextOverflowModes.Ellipsis);
                if (messages.Count - i > 5)
                {
                    messages[i].Off(true);
                }
                else
                {
                    messages[i].StartFading(20);
                }
            }
            //messageGroup.gameObject.SetActive(false);
            textInput.gameObject.SetActive(false);
            StartCoroutine(Closing());
        }

        public IEnumerator Closing()
        {
            background.GetComponent<Fader>().Out(0.2f);
            yield return Sequences.Wait(0.2f);
            
            background.SetActive(false);
            open = false;
            closing = false;
        }

        public class ChatMessage : LayoutElement
        {
            Friend friend;
            string message;
            TextMeshProUGUI text;

            bool hideFriend = false;

            public static ChatMessage Create(Transform t, Friend f, string s)
            {
                GameObject obj = HelperUI.Background(t, new Color(0, 0, 0, 0.5f));
                obj.transform.localScale = Vector3.one;
                RectTransform rt = obj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(3f,0.3f);
                obj.GetComponent<Image>().enabled = false;
                TextMeshProUGUI text = rt.WithText(0.25f, Vector3.zero, new Vector2(0.1f,0), "", Color.white, TextAlignmentOptions.Left).GetComponent<TextMeshProUGUI>();
                text.overflowMode = TextOverflowModes.Ellipsis;
                text.enableWordWrapping = false;
                text.outlineWidth = 0.05f;
                rt.pivot = new Vector2(0, 0.5f);
                ChatMessage cm = obj.AddComponent<ChatMessage>();
                cm.minHeight = 0.5f;
                cm.text = text;
                cm.Set(f, s);
                return cm;
            }

            public void HideFriend()
            {
                hideFriend = true;
                UpdateString();
            }

            public void Set(Friend f, string s)
            {
                friend = f;
                message = s;
                UpdateString();
            }

            public void ChangeOverflow(TMPro.TextOverflowModes mode)
            {
                text.overflowMode = mode;
                UpdateString();
            }

            public void UpdateString()
            {
                text.text = FriendColor() + message;
            }

            public string FriendColor()
            {
                if (hideFriend)
                {
                    return "";
                }
                string s = "";
                if (HandlerSystem.self.Id != friend.Id)
                {
                    s += "<color=#ff8>";
                }
                else
                {
                    s += "<color=#8ff>";
                }
                s += (text.overflowMode == TextOverflowModes.Ellipsis) ? friend.Name.Substring(0, 1) : friend.Name;
                s += "</color>: ";
                return s;
            }

            public void Off(bool instant = false)
            {
                float duration = (instant) ? 0.01f : 0.2f;
                StopAllCoroutines();
                text.CrossFadeAlpha(0, duration, true);
            }

            public void On()
            {
                StopAllCoroutines();
                text.CrossFadeAlpha(1, 0.1f, true);
            }

            public void StartFading(float duration)
            {
                StartCoroutine(FadeAfterDuration(duration));
            }

            public IEnumerator FadeAfterDuration(float duration)
            {
                yield return Sequences.Wait(duration);
                text.CrossFadeAlpha(0, 2, true);
            }
        }
    }
}
