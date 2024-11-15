using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Stabilizer.TileView
{
    internal static class SearchBar
    {
        public static TMP_InputField Instance;

        public static Vector3 defaultPosition = new Vector3(3.8f, 5.1f, 0f);
        public static Vector3 defaultRotation = new Vector3(0, 0, 2f);
        public static Vector2 defaultSize = new Vector2(2.5f, 0.4f);

        public static Vector3 selectedPosition = new Vector3(2.6f, 4.35f, 0f);
        public static Vector3 selectedRotation = new Vector3(0f, 0f, 0f);
        public static Vector2 selectedSize = new Vector2(5, 0.6f);

        public static Vector2 Ratio(Vector2 v) => new Vector2(30 * v.x / 0.4f, 30f);

        public static string text = "";
        public static TMP_InputField CreateSearchBar(Transform t)
        {
            if (Instance != null) { return Instance; }

            text = "";
            Instance = UI.NewInputField("Search Bar", t, defaultPosition, new Vector2(2.5f, 0.4f), "Search by Title...");
            Instance.transform.eulerAngles = defaultRotation;
            Instance.onValueChanged.AddListener(OnChanged);
            Instance.onSelect.AddListener(OnSelect);
            Instance.onDeselect.AddListener(OnDeselect);

            return Instance;
        }

        static float duration = 0.3f;
        static void OnSelect(string s)
        {
            if (ModInspectView.Enabled)
            {
                ModInspectView.EndInspect();
            }

            GameObject _obj = Instance.gameObject;
            LeanTween.cancel(_obj);
            LeanTween.moveLocal(_obj, selectedPosition, duration);
            LeanTween.value(_obj,
                (float f) =>
                {
                    _obj.transform.localPosition = Vector3.Lerp(defaultPosition, selectedPosition, f);
                    _obj.transform.eulerAngles = Vector3.Lerp(defaultRotation, selectedRotation, f);
                    LerpSize(_obj.transform, defaultSize, selectedSize, f);
                    LerpSize(_obj.transform.GetChild(0), Ratio(defaultSize), Ratio(selectedSize), f);
                    LerpSize(_obj.transform.GetChild(0).Find("Placeholder"), Ratio(defaultSize), Ratio(selectedSize), f);
                    LerpSize(_obj.transform.GetChild(0).Find("Text"), Ratio(defaultSize), Ratio(selectedSize), f);
                }, 
                from: 0, to: 1, time: duration
                ).setEaseInOutQuart();
        }

        static void OnDeselect(string s)
        {

            GameObject _obj = Instance.gameObject;
            LeanTween.value(_obj,
                (float f) =>
                {
                    _obj.transform.localPosition = Vector3.Lerp(selectedPosition, defaultPosition, f);
                    _obj.transform.eulerAngles = Vector3.Lerp(selectedRotation, defaultRotation, f);
                    LerpSize(_obj.transform, selectedSize, defaultSize, f);
                    LerpSize(_obj.transform.GetChild(0), Ratio(selectedSize), Ratio(defaultSize), f);
                    LerpSize(_obj.transform.GetChild(0).Find("Placeholder"), Ratio(selectedSize), Ratio(defaultSize), f);
                    LerpSize(_obj.transform.GetChild(0).Find("Text"), Ratio(selectedSize), Ratio(defaultSize), f);
                },
                from: 0, to: 1, time: duration
                ).setEaseInOutQuart();
        }

        static void LerpSize(Transform t, Vector2 from, Vector2 to, float f)
        {
            (t as RectTransform).sizeDelta = Vector2.Lerp(from, to, f);
        }

        static void OnChanged(string s)
        {
            text = s ?? "";
            TileViewManager.Filter();
        }

        internal static bool Satisfies(string s)
        {
            return (text == "" || s.Contains(text));
        }
    }
}
