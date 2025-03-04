using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.SmartFormat.Utilities;
using UnityEngine.UI;

namespace Stabilizer
{
    internal static class UI
    {
        public static Color MenuYellow => new Color(1f, 0.7f, 0.3f);
        public static Color MenuToggle => new Color(1f, 1f, 0.5f, 0.5f);
        public static Color OffYellow => new Color(1f, 0.9f, 0.6f, 0.9f);
        public static Color TabTan => new Color(0.75f, 0.5f, 0.3f, 1);
        public static Color SelectedTan => new Color(0.9f, 0.8f, 0.7f, 0.95f);

        public static Color Reddish => new Color(1f, 0f, 0f, 0.5f);
        public static Color ExitRed => new Color(1f, 0.3f, 0.3f, 1f);
        public static Color Empty => new Color(1, 1, 1, 0);
        public static Color Translucent => new Color(1f, 1f, 1f, 0.5f);

        public static RectTransform AddLayoutElement(this RectTransform t, Vector2 min, Vector2 pref, Vector2 flex)
        {
            LayoutElement element = t.gameObject.AddComponent<LayoutElement>();
            element.minWidth = min.x;
            element.minHeight = min.y;
            element.preferredWidth = pref.x;
            element.preferredHeight = pref.y;
            element.flexibleWidth = flex.x;
            element.flexibleHeight = flex.y;
            return t;
        }
        public static RectTransform NewButton(this Transform t, string name, Vector3 position, Vector2 size, Color color, UnityAction action)
        {
            GameObject button = new GameObject(name, new Type[] { typeof(RectTransform), typeof(Image), typeof(Button) });
            button.GetComponent<RectTransform>().sizeDelta = size;
            button.GetComponent<Image>().color = color;
            button.GetComponent<Button>().onClick.AddListener(action);
            button.transform.SetParent(t);
            button.transform.localPosition = position;
            button.AddComponent<UINavigationItem>();
            return button.GetComponent<RectTransform>();
        }

        public static RectTransform WithBox(this RectTransform t, Vector3 position, Vector2 borders, Color color)
        {
            GameObject obj = new GameObject("Box", new Type[] { typeof(Image) });
            obj.transform.SetParent(t.transform);
            obj.transform.localPosition = position;
            obj.GetOrAdd<RectTransform>().sizeDelta = t.sizeDelta - borders;
            Image _text = obj.GetComponent<Image>();
            _text.color = color;

            return obj.GetOrAdd<RectTransform>();
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

        public static TMP_InputField NewInputField(string name, Transform parent, Vector3 position, Vector2 size, string defaultText, float fontRatio = 0.9f)
        {
            float baseFontSize = 30f;
            Vector2 textSizeDelta = new Vector2(30 * size.x / size.y, baseFontSize);

            // Original credit to Hopeful :3
            // Image adds CanvasRenderer, InputFieldKeepFocus adds TMP_InputField
            // Add ContentSizeFitter to match window size?
            // Add VerticalLayoutGroup to stay a certain height?
            // Or use CopyRectTransform if lazy
            GameObject container = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputFieldKeepFocus));
            container.transform.SetParent(parent);
            (container.transform as RectTransform)?.SetSize(size);
            // Textbox image. Console settings (but not masked for round edges)
            var _image = container.GetOrAdd<Image>();
            _image.color = new Color(0, 0, 0, 0.7f);
            container.SetActive(false);

            // Addendum to create reasonably sized caret
            GameObject textarea = new GameObject("Text Area", new Type[] { typeof(RectTransform), typeof(Image) });
            (textarea.transform as RectTransform)?.SetSize(textSizeDelta);
            textarea.transform.SetParent(container.transform);
            _image = textarea.GetOrAdd<Image>();
            _image.color = new Color(0, 0, 0, 0);

            GameObject textContainer = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textContainer.transform.SetParent(textarea.transform);
            (textContainer.transform as RectTransform)?.SetSize(textSizeDelta);
            var _text = textContainer.GetOrAdd<TextMeshProUGUI>();
            _text.color = Color.white;
            _text.fontSize = baseFontSize - 1;
            _text.richText = false;
            _text.overflowMode = TextOverflowModes.Ellipsis; // Change as you like
            _text.alignment = TextAlignmentOptions.Left;

            GameObject placeholderContainer = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderContainer.transform.SetParent(textarea.transform);
            (placeholderContainer.transform as RectTransform)?.SetSize(textSizeDelta);

            var _placeholder = placeholderContainer.GetOrAdd<TextMeshProUGUI>();
            _placeholder.text = defaultText;
            _placeholder.color = Color.gray;
            _placeholder.fontSize = baseFontSize - 1; //This makes the character:caret ratio 30:3. If you want to change the 
            _placeholder.richText = false;
            _placeholder.overflowMode = TextOverflowModes.Ellipsis; // Change as you like
            _placeholder.alignment = TextAlignmentOptions.Left;

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

            textarea.transform.localScale = ((fontRatio * size.y / baseFontSize) * new Vector3(1,1,0)) + new Vector3(0,0,1);
            container.transform.localPosition = position;
            container.SetActive(true);

            return _inputField;
        }

        public static void SetSize(this RectTransform t, Vector2 size)
        {
            t.sizeDelta = size;
        }
    }
}
