using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using D = UnityEngine.Debug;

namespace Stabilizer.Journal
{
    internal class JournalFilterManager : MonoBehaviour
    {
        static JournalFilterManager instance;

        internal static JournalCardManager jcm;
        internal static List<WildfrostMod> mods = new List<WildfrostMod>(); 
        internal static int index = -1;
        public static void StartJournalFilter()
        {
            if (instance != null) { return; }
            //Canvas/SafeArea/Menu/Journal/Positioner/Pages/Page1/
            Transform parent = new GameObject("Journal Filter Group").transform;
            parent.SetParent(GameObject.Find("Canvas/SafeArea/Menu/Journal/Positioner").transform);

            parent.position = new Vector3(0, -0.45f, 0);

            RectTransform text = UI.NewButton("Filter Tab", parent, new Vector3(-3.6f,5.0f), new Vector2(4f, 0.6f), UI.SelectedTan, OpenSearch)
                .WithText(0.4f, new Vector3(0,0.05f,0), 0.2f*Vector2.one, "Filter: None", Color.black);
            text.GetComponent<TextMeshProUGUI>().enableAutoSizing = true;
            Transform transform = text.parent;

            UI.NewButton("Left Button", parent, new Vector3(-6f, 5f), new Vector2(0.6f, 0.6f), UI.SelectedTan, DecrementFilter)
                .WithText(0.5f, new Vector3(0, 0.05f, 0), Vector2.zero, "<", Color.black);

            UI.NewButton("Right Button", parent, new Vector3(-1.2f, 5f), new Vector2(0.6f, 0.6f), UI.SelectedTan, IncrementFilter)
                .WithText(0.5f, new Vector3(0, 0.05f, 0), Vector2.zero, ">", Color.black);

            UI.NewButton("Clear Button", parent, new Vector3(-0.5f, 5f), new Vector2(0.6f, 0.6f), UI.ExitRed, ClearFilter)
                .WithText(0.45f, new Vector3(0, 0.05f, 0), Vector2.zero, "X", Color.black);

            parent.SetSiblingIndex(1);
            instance = transform.gameObject.AddComponent<JournalFilterManager>();

            //UI.NewButton("Search Tab", parent, new Vector3(1.4f, 4.96f), new Vector2(1.75f, 0.45f), UI.OffYellow, OpenFilter)
                //.WithText(0.4f, Vector3.zero, Vector2.zero, "Search", Color.black, TMPro.TextAlignmentOptions.Center);
        }

        public static void OpenSearch()
        {
            if (JournalFilterSearch.Instance != null) { return; }
            JournalFilterSearch.CreateSearchBar(instance.transform.parent.parent);
        }

        public static void IncrementFilter()
        {
            Filter(index + 1);
        }

        public static void DecrementFilter()
        {
            Filter(index - 1);
        }

        public static void ClearFilter()
        {
            Filter(-1);
        }

        public static void Filter(int i)
        {
            if (jcm == null)
            {
                D.Log("[Stabilizer] Could not find JCM");
                return;
            }
            index = i;
            if (index == -2)
            {
                index = mods.Count - 1;
            }
            if (index >= 0 && index < mods.Count)
            {
                D.Log($"[Stabilizer] Filter: {mods[index]?.Title ?? "Unmodded"}");
                foreach (JournalCard card in jcm.cardIcons)
                {
                    //D.Log($"[Stabilizer] {card.cardData.title}, {card.cardData.ModAdded?.Title ?? "null"}");
                    //D.Log($"[Stabilizer] {card.cardData.ModAdded == mods[index]}");
                    card.gameObject.SetActive(card.cardData.ModAdded == mods[index]);
                }
            }
            else
            {
                D.Log($"[Stabilizer] Reset.");
                foreach (JournalCard card in jcm.cardIcons)
                {
                    //D.Log($"[Stabilizer] {card.cardData.title}, {card.cardData.ModAdded?.Title ?? "null"}");
                    card.gameObject.SetActive(true);
                }
                index = -1;
            }
            instance.StartCoroutine(jcm.ScrollToTop());
            ChangeText();

        }

        internal static void ChangeText(string s)
        {
            instance.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = s;
        }

        internal static void ChangeText()
        {
            TextMeshProUGUI text = instance.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            if (index == -1)
            {
                text.text = "Filter: None";
            }
            else
            {
                text.text = mods[index] == null ? "Unmodded" : mods[index].Title;
            }
        }

        [HarmonyPatch]
        internal class PatchJournalCardManager
        {
            internal static bool reset = true;

            [HarmonyPostfix] 
            [HarmonyPatch(typeof(JournalCardManager), nameof(JournalCardManager.OnEnable), new Type[0])]
            internal static void CreateList(JournalCardManager __instance)
            {
                jcm = __instance;
                if (!reset) 
                { 
                    Filter(index); 
                    return; 
                }

                reset = false;
                mods.Clear();
                List<CardData> list = AddressableLoader.GetGroup<CardData>("CardData");
                WildfrostMod mod;
                foreach (CardData card in list)
                {
                    mod = card?.ModAdded;
                    if (!mods.Contains(mod))
                    {
                        mods.Add(mod);
                    }
                }
                index = -1;
                
                D.Log($"[Stabilizer] {mods.Count}");
                mods.Sort((m1, m2) =>
                {
                    string t1 = m1?.Title ?? string.Empty;
                    string t2 = m2?.Title ?? string.Empty;
                    return t1.CompareTo(t2);
                });
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(JournalCardManager), nameof(JournalCardManager.SelectTab), new Type[]
            {
                typeof(int)
            })]
            internal static void KeepFilter()
            {
                Filter(index);
            }
        }
    }

    
}
