using Stabilizer.TileView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Stabilizer.Journal
{
    internal class JournalFilterSearch
    {
        public static TMP_InputField Instance;

        public static Vector3 selectedPosition = new Vector3(-3.1f, 4f, 0f);
        public static Vector3 selectedRotation = new Vector3(0, 0, 0f);
        public static Vector2 selectedSize = new Vector2(6f, 0.6f);

        public static Vector3 defaultPosition = new Vector3(-3.1f, 4f, 0f);
        public static Vector3 defaultRotation = new Vector3(0f, 0f, 0f);
        public static Vector2 defaultSize = new Vector2(6f, 0.6f);

        public static List<string> autoCompletes;
        public static int index = -1;

        public static Vector2 Ratio(Vector2 v) => new Vector2(30 * v.x / 0.4f, 30f);

        public static string text = "";
        internal static TMP_InputField CreateSearchBar(Transform t)
        {
            autoCompletes = JournalFilterManager.mods.Select(m => m?.Title ?? "Unmodded").ToList();

            if (Instance != null) { return Instance; }

            text = "";
            Instance = UI.NewInputField("Search Bar", t, defaultPosition, new Vector2(2.5f, 0.4f), "Mod Title...");
            Instance.transform.eulerAngles = defaultRotation;
            Instance.onValueChanged.AddListener(OnChanged);
            Instance.onSubmit.AddListener(OnSubmit);
            Instance.onDeselect.AddListener(OnDeselect);

            OnSelect("");
            return Instance;
        }

        private static void OnSubmit(string arg0)
        {
            JournalFilterManager.Filter(index);
            Instance.gameObject.Destroy();
        }

        static float duration = 0.5f;
        static void OnSelect(string _)
        {
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
            index = -1;
            JournalFilterManager.ChangeText();
            Instance.gameObject.Destroy();
        }

        static void LerpSize(Transform t, Vector2 from, Vector2 to, float f)
        {
            (t as RectTransform).sizeDelta = Vector2.Lerp(from, to, f);
        }

        static void OnChanged(string s)
        {
            if (s.IsNullOrEmpty())
            {
                JournalFilterManager.ChangeText("");
                return;
            }
            s = s.ToLower();
            List<string> list = autoCompletes.Where(t => t.ToLower().Contains(s)).OrderBy(t => t.ToLower().IndexOf(s)).ToList();
            if (list.Count == 0)
            {
                JournalFilterManager.ChangeText(s);
                return;
            }
            int indexOf = list[0].ToLower().IndexOf(s);
            string autoCompleteString = "<color=#ff4545>" + list[0].Substring(0, indexOf)
                + "<color=#000000>" + list[0].Substring(indexOf, s.Length) + "</color>" +
                list[0].Substring(indexOf + s.Length) + "</color>";
            index = autoCompletes.IndexOf(list[0]);
            JournalFilterManager.ChangeText(autoCompleteString);
        }
    }
}
