using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MultiplayerBase
{
    public static class HelperUI
    {
        public static Button template;

        public static Button ButtonTemplate(Transform transform, Vector2 dim, Vector3 pos, string text, Color color)
        {
            if (template == null)
            {
                GameObject buttonObject = new GameObject("Button");
                buttonObject.AddComponent<Image>();
                UnityEngine.Object.DontDestroyOnLoad(buttonObject);
                template = buttonObject.AddComponent<Button>();
                buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1, 1);
                GameObject gameObject2 = new GameObject("Text");
                gameObject2.transform.SetParent(buttonObject.transform);
                TextMeshProUGUI textElement = gameObject2.AddComponent<TextMeshProUGUI>();
                textElement.fontSize = 0.4f;
                textElement.color = Color.black;
                textElement.verticalAlignment = VerticalAlignmentOptions.Middle;
                textElement.horizontalAlignment = HorizontalAlignmentOptions.Center;
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
