using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MultiplayerBase.UI
{
    public static class HelperUI
    {
        public static Button template;
        public static ButtonAnimator betterTemplate;

        public static void CreateTemplates()
        {
            GameObject buttonAnimObject = new GameObject("Button Animator");
            buttonAnimObject.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(buttonAnimObject);
            buttonAnimObject.AddComponent<RectTransform>().sizeDelta = new Vector2(1, 1);
            betterTemplate = buttonAnimObject.AddComponent<ButtonAnimator>();
            betterTemplate.highlightColour = new Color(1, 0.78f, 0.35f);
            betterTemplate.type = ButtonType.Normal;

            //4 tween ui's
            TweenUI tween;

            //hover: outElastic scale to 1.15x
            tween = buttonAnimObject.AddComponent<TweenUI>();
            tween.target = buttonAnimObject;
            tween.property = TweenUI.Property.Scale;
            tween.ease = LeanTweenType.easeOutElastic;
            tween.to = new Vector3(1.15f, 1.15f, 1f);
            tween.duration = 0.67f;
            betterTemplate.hoverTween = tween;

            //unhover: outBack scale to 1x
            tween = buttonAnimObject.AddComponent<TweenUI>();
            tween.target = buttonAnimObject;
            tween.property = TweenUI.Property.Scale;
            tween.ease = LeanTweenType.easeOutBack;
            tween.to = new Vector3(1f, 1f, 1f);
            tween.duration = 0.2f;
            betterTemplate.unHoverTween = tween;

            //press: outElastic scale to 0.95x
            tween = buttonAnimObject.AddComponent<TweenUI>();
            tween.target = buttonAnimObject;
            tween.property = TweenUI.Property.Scale;
            tween.ease = LeanTweenType.easeOutElastic;
            tween.to = new Vector3(0.95f, 0.95f, 1f);
            tween.duration = 0.5f;
            betterTemplate.pressTween = tween;

            //release: outElastic scale to 1x
            tween = buttonAnimObject.AddComponent<TweenUI>();
            tween.target = buttonAnimObject;
            tween.property = TweenUI.Property.Scale;
            tween.ease = LeanTweenType.easeOutBack;
            tween.to = new Vector3(1f, 1f, 1f);
            tween.duration = 0.2f;
            betterTemplate.releaseTween = tween;

            //button and text
            GameObject buttonObject = new GameObject("Button");
            buttonObject.transform.SetParent(buttonAnimObject.transform, false);
            betterTemplate.image = buttonObject.AddComponent<Image>();
            template = buttonObject.AddComponent<Button>();
            buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1, 1);
            betterTemplate.nav = buttonObject.AddComponent<UINavigationItem>();
            GameObject gameObject2 = new GameObject("Text");
            gameObject2.transform.SetParent(buttonObject.transform);
            TextMeshProUGUI textElement = gameObject2.AddComponent<TextMeshProUGUI>();
            textElement.fontSize = 0.4f;
            textElement.color = Color.black;
            textElement.verticalAlignment = VerticalAlignmentOptions.Middle;
            textElement.horizontalAlignment = HorizontalAlignmentOptions.Center;

            betterTemplate.button = template;
            betterTemplate.text = textElement;
        }

        public static Button ButtonTemplate(Transform transform, Vector2 dim, Vector3 pos, string text, Color color)
        {
            if (template == null)
            {
                CreateTemplates();
            }
            GameObject newButtonObject = GameObject.Instantiate(template.gameObject, transform);
            newButtonObject.name = text;
            newButtonObject.GetComponent<RectTransform>().sizeDelta = dim;
            newButtonObject.transform.localPosition = pos;
            TextMeshProUGUI textElement2 = newButtonObject.GetComponentInChildren<TextMeshProUGUI>();
            textElement2.text = text;
            textElement2.GetComponent<RectTransform>().sizeDelta = dim;
            newButtonObject.GetComponent<Image>().color = color;
            return newButtonObject.GetComponent<Button>();
        }

        public static Button BetterButtonTemplate(Transform transform, Vector2 dim, Vector3 pos, string text, Color color)
        {
            if (betterTemplate == null)
            {
                CreateTemplates();
            }
            GameObject newAnimObject = GameObject.Instantiate(betterTemplate.gameObject, transform);
            ButtonAnimator buttonAnimator = newAnimObject.GetComponent<ButtonAnimator>();
            newAnimObject.name = "Button Animator";
            newAnimObject.GetComponent<RectTransform>().sizeDelta = dim;
            newAnimObject.transform.localPosition = pos;

            GameObject newButtonObject = newAnimObject.transform.GetChild(0).gameObject;
            newButtonObject.GetComponent<RectTransform>().sizeDelta = dim;
            newButtonObject.transform.localPosition = Vector3.zero;
            TextMeshProUGUI textElement2 = newButtonObject.GetComponentInChildren<TextMeshProUGUI>();
            textElement2.text = text;
            textElement2.GetComponent<RectTransform>().sizeDelta = dim;
            newButtonObject.GetComponent<Image>().color = color;

            //EventTriggers
            EventTrigger et = newButtonObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerEnter;
            entry.callback.AddListener(e=> { buttonAnimator.Hover(); });
            et.triggers.Add(entry);
            entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerExit;
            entry.callback.AddListener(e => { buttonAnimator.UnHover(); });
            et.triggers.Add(entry);
            entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerDown;
            entry.callback.AddListener(e => { buttonAnimator.Press(); });
            et.triggers.Add(entry);
            entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerUp;
            entry.callback.AddListener(e => { buttonAnimator.Release(); });
            et.triggers.Add(entry);

            newAnimObject.SetActive(true);

            return newButtonObject.GetComponent<Button>();

        }

        public static Transform AddLayoutElement(this Transform t, Vector2 minDim)
        {
            LayoutElement el = t.gameObject.AddComponent<LayoutElement>();
            el.minWidth = minDim.x;
            el.minHeight = minDim.y;
            return t;
        }

        public static GameObject ButtonTemplateWithIcon(Transform transform, Vector2 totalDim, Vector2 iconDim, Vector3 pos, string text, Color color, float innerPadding, float outerPadding)
        {
            GameObject obj1 = HelperUI.ButtonTemplate(transform, totalDim, pos, text, color).gameObject;
            GameObject obj2 = new GameObject("Icon");
            obj2.AddComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f);
            obj2.transform.SetParent(obj1.transform, false);
            obj2.GetComponent<RectTransform>().sizeDelta = iconDim;
            obj2.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, outerPadding, iconDim.x);
            TextMeshProUGUI memberName = obj1.GetComponentInChildren<TextMeshProUGUI>();
            memberName.alignment = TextAlignmentOptions.CaplineLeft;
            memberName.overflowMode = TextOverflowModes.Ellipsis;
            memberName.enableWordWrapping = false;
            Vector2 textDim = totalDim - new Vector2(iconDim.x + innerPadding + 2 * outerPadding, 2*outerPadding);
            memberName.GetComponent<RectTransform>().sizeDelta = textDim;
            memberName.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, outerPadding, textDim.x);
            return obj1;
        }

        public static GameObject Background(Transform transform, Color color)
        {
            GameObject background = new GameObject("Background");
            background.transform.localScale = new Vector3(10, 10, 1);
            background.transform.SetParent(transform);
            Image image = background.AddComponent<Image>();
            image.color = color;
            return background;
        }

        public static GameObject HorizontalGroup(string name, Transform transform, Vector2 scale, float spacing = 0.2f)
        {
            GameObject gameObject = new GameObject(name);
            HorizontalLayoutGroup layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = spacing;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            gameObject.GetComponent<RectTransform>().sizeDelta = scale;
            gameObject.transform.SetParent(transform, false);
            return gameObject;
        }

        public static GameObject VerticalGroup(string name, Transform transform, Vector2 scale, float spacing = 0.2f)
        {
            GameObject gameObject = new GameObject(name);
            VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = spacing;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            gameObject.GetComponent<RectTransform>().sizeDelta = scale;
            gameObject.transform.SetParent(transform, false);
            return gameObject;
        }

        public static OtherCardViewer OtherCardViewer(string name, Transform transform, CardController cc)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(transform);
            Image image = gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.25f);
            gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1f, 1f);
            OtherCardViewer ocv = gameObject.AddComponent<OtherCardViewer>();
            ocv.AssignController(cc);
            ocv.holder = gameObject.GetComponent<RectTransform>();
            ocv.onAdd = new UnityEventEntity();
            ocv.onRemove = new UnityEventEntity();
            return ocv;
        }
    }
}
