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


        public static readonly Color restingColor = new Color(0.85f, 0.85f, 0.85f);
        public static readonly Color hoverColor = new Color(1f, 1f, 1f);
        public static readonly Color highlightYellow = new Color(1, 0.78f, 0.35f);
        public static readonly Color disabledColor = new Color(0.3f, 0.3f, 0.3f);

        public static void CreateTemplates()
        {
            GameObject buttonAnimObject = new GameObject("Button Animator");
            buttonAnimObject.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(buttonAnimObject);
            buttonAnimObject.AddComponent<RectTransform>().sizeDelta = new Vector2(1, 1);
            betterTemplate = buttonAnimObject.AddComponent<ButtonAnimator>();
            betterTemplate.baseColourSet = true;
            betterTemplate.textCopyBase = false;
            betterTemplate.highlightColour = highlightYellow;
            betterTemplate.textNormalColour = Color.black;
            betterTemplate.textHighlightColour = Color.black;
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

        public static Button HoverColors(this Button b, Color hover, Color unhover)
        {
            ButtonHover bh = b.gameObject.AddComponent<ButtonHover>();
            bh.Set(b, hover, unhover);
            return b;
        }

        public static void ToggleParts(this Button b, bool on)
        {
            if (on)
            {
                b.EnableAllParts();
            }
            else
            {
                b.DisableAllParts();
            }
        }

        public static void EnableAllParts(this Button b)
        {
            b.interactable = true;
            b.GetComponent<UINavigationItem>().enabled = true;
            b.GetComponent<ButtonHover>()?.Enable();
        }

        public static void DisableAllParts(this Button b)
        {
            b.interactable = false;
            b.GetComponent<UINavigationItem>().enabled = false;
            b.GetComponent<ButtonHover>()?.Disable();
        }

        public static ButtonAnimator EditButtonAnimator(this Button b, Color? highlightColor = null, Color? textHighlightColor = null, Color? normalColor = null, Color? textNormalColor = null)
        {
            ButtonAnimator anim = b.transform.parent.GetComponent<ButtonAnimator>();
            if (highlightColor is Color c1)
            {
                anim.highlightColour = c1;
            }
            if (textHighlightColor is Color c2)
            {
                anim.textHighlightColour = c2;
            }
            if (normalColor is Color c3)
            {
                anim.baseColour = c3;
            }
            if (textNormalColor is Color c4)
            {
                anim.textNormalColour = c4;
            }
            return anim;
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

        public static TMP_InputField WithInputField(this Transform parent, Vector3 position, Vector2 size, string defaultText, float fontRatio = 0.9f)
        {
            float baseFontSize = 30f;
            Vector2 textSizeDelta = new Vector2(baseFontSize * size.x / size.y / fontRatio, baseFontSize);

            // Original credit to Hopeful :3
            // Image adds CanvasRenderer, InputFieldKeepFocus adds TMP_InputField
            // Add ContentSizeFitter to match window size?
            // Add VerticalLayoutGroup to stay a certain height?
            // Or use CopyRectTransform if lazy
            GameObject container = new GameObject("Input Field", typeof(RectTransform), typeof(Image), typeof(InputFieldKeepFocus));
            container.transform.SetParent(parent);
            (container.transform as RectTransform).sizeDelta = size;
            // Textbox image. Console settings (but not masked for round edges)
            var _image = container.GetOrAdd<Image>();
            _image.color = new Color(0, 0, 0, 0f);
            container.SetActive(false);

            // Addendum to create reasonably sized caret
            GameObject textarea = new GameObject("Text Area", new Type[] { typeof(RectTransform), typeof(Image) });
            (textarea.transform as RectTransform).sizeDelta = textSizeDelta;
            textarea.transform.SetParent(container.transform);
            _image = textarea.GetOrAdd<Image>();
            _image.color = new Color(0, 0, 0, 0);

            GameObject placeholderContainer = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderContainer.transform.SetParent(textarea.transform);
            (placeholderContainer.transform as RectTransform).sizeDelta = textSizeDelta;

            var _placeholder = placeholderContainer.GetOrAdd<TextMeshProUGUI>();
            _placeholder.text = defaultText;
            _placeholder.color = Color.gray;
            _placeholder.fontSize = baseFontSize - 1; //This makes the character:caret ratio 30:3. If you want to change the 
            _placeholder.richText = false;
            _placeholder.overflowMode = TextOverflowModes.Ellipsis; // Change as you like
            _placeholder.alignment = TextAlignmentOptions.Center;

            GameObject textContainer = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textContainer.transform.SetParent(textarea.transform);
            (textContainer.transform as RectTransform).sizeDelta = textSizeDelta;
            var _text = textContainer.GetOrAdd<TextMeshProUGUI>();
            _text.color = Color.white;
            _text.fontSize = baseFontSize - 1;
            _text.richText = false;
            _text.overflowMode = TextOverflowModes.Ellipsis; // Change as you like
            _text.alignment = TextAlignmentOptions.Center;

            // Need to set the TextViewport for where text should be visible
            // ..it should be a dedicated TextArea, but we lazy
            var _inputField = container.GetOrAdd<TMP_InputField>();
            _inputField.textViewport = textarea.transform as RectTransform;
            _inputField.textComponent = _text;
            _inputField.placeholder = _placeholder; // Default to placeholder when no text is inputted
            _inputField.targetGraphic = _image; // Not sure what this is
            // Michael: targetGraphic is from the selectable class. I assume this can be used to get things to be highlighted. (Not used by Input_Fields inherently)

            _inputField.caretWidth = 3; //caretWidth is an integer, sadly :(
                                        //Small trick to spawn the caret
                                        //_inputField.enabled = false;
                                        //_inputField.enabled = true;

            textarea.transform.localScale = ((fontRatio * size.y / baseFontSize) * new Vector3(1, 1, 0)) + new Vector3(0, 0, 1);
            container.transform.localPosition = position;
            container.SetActive(true);

            return _inputField;
        }

        public static RectTransform WithText(this RectTransform t, float fontSize, Vector3 position, Vector2 borders, string text, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            GameObject obj = new GameObject("Text", new Type[] { typeof(TextMeshProUGUI) });
            obj.transform.SetParent(t.transform);
            obj.transform.localPosition = position;
            obj.GetOrAdd<RectTransform>().sizeDelta = t.sizeDelta - borders;
            TextMeshProUGUI _text = obj.GetComponent<TextMeshProUGUI>();
            _text.fontSize = fontSize;
            _text.color = color;
            _text.text = text;
            _text.alignment = alignment;

            return obj.GetOrAdd<RectTransform>();
        }
    }
}
