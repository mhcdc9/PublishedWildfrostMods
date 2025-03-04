using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Stabilizer.Compatibility;
using System.Collections;
using static Building;
using WildfrostHopeMod.Configs;
using System.IO;

namespace Stabilizer.TileView
{
    internal class ModInspectView : MonoBehaviour
    {
        public static ModInspectView Instance;
        public static bool Enabled => Instance!=null && Instance.gameObject.activeSelf;
        public static WildfrostMod Mod => Instance?.holder?.Mod;

        internal static RectTransform FindViewPort()
        {
            return GameObject.Find("Canvas/SafeArea/Menu/Panel/Positioner/Scroll View").GetComponent<RectTransform>();
        }

        internal static void CreateModInspectView()
        {
            GameObject parent = new GameObject("Mod Inspect View", new Type[2] { typeof(RectTransform), typeof(Image) });
            parent.SetActive(false);
            parent.transform.SetParent(GameObject.Find("Canvas/SafeArea/Menu/Panel/Positioner/Scroll View/Viewport").transform, false);

            Instance = parent.AddComponent<ModInspectView>();
            GameObject modHolder = GameObject.Instantiate(GameObject.FindObjectOfType<ModsSceneManager>().ModPrefab, parent.transform);
            Instance.holder = modHolder.GetComponent<ModHolder>();

            GameObject bottomPanel = new GameObject("Bottom Panel", new Type[3] { typeof(RectTransform), typeof(Image), typeof(Wobbler) });
            bottomPanel.transform.SetParent(parent.transform);
            Instance.bottomPanel = bottomPanel;
            bottomPanel.AddComponent<Mask>();
            bottomPanel.GetComponent<Image>().color = new Color(0.4f, 0.2f, 0.2f, 1f);
        }

        public static void StartInspect(WildfrostMod mod, Vector3 position)
        {
            if (Instance == null)
            {
                CreateModInspectView();
            }

            Instance.SetSize(position);
            Instance.gameObject.SetActive(true);
            Instance.StartCoroutine(StartCosmeticLayer(mod, Instance.bottomPanel.transform.GetChild(0) as RectTransform));
            Instance.StartCoroutine(StartContentLayer(mod, Instance.bottomPanel.transform.GetChild(1) as RectTransform));
            Instance.bottomPanel.GetComponent<Wobbler>()?.WobbleRandom();
            Instance.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            Instance.holder.Mod = mod;
            Instance.holder.UpdateInfo();
            MarkerManager.ResetMarkerColors();
        }



        public static void EndInspect()
        {
            Instance.StopAllCoroutines();
            Instance.bottomPanel.transform.DestroyAllChildren();
            Instance.gameObject.SetActive(false);
            MarkerManager.ResetMarkerColors();
            TileViewManager.Filter();
        }

        public ModHolder holder;
        public GameObject bottomPanel;

        public static float dur = 0.4f;
        internal void SetSize(Vector3? startPosition = null)
        {
            Vector3 fixedStartPos = startPosition ?? Vector3.zero;

            RectTransform t = GetComponent<RectTransform>();
            RectTransform viewport = FindViewPort();
            t.sizeDelta = viewport.sizeDelta;
            t.position = viewport.position + 0.1f*Vector3.left;

            float y = t.sizeDelta.y;
            RectTransform t2 = holder.GetComponent<RectTransform>();
            Vector3 holderPosition = new Vector2(0, y / 2 - t2.sizeDelta.y / 2 - 0.1f);
            t2.localPosition = holderPosition + new Vector3(0, t2.sizeDelta.y);
            LeanTween.moveLocal(holder.gameObject, holderPosition, dur).setEaseInOutQuart();

            RectTransform t3 = bottomPanel.GetComponent<RectTransform>();
            t3.sizeDelta = t.sizeDelta - t2.sizeDelta.y * Vector2.up - (0.1f * new Vector2(4, 3));
            t3.localScale = Vector3.zero;
            LeanTween.scale(bottomPanel, Vector3.one, dur).setEaseInOutQuart();
            Vector3 panelPosition = new Vector2(0, -y / 2 + t3.sizeDelta.y / 2 + 0.1f);
            t3.position = fixedStartPos;
            LeanTween.moveLocal(bottomPanel, panelPosition, dur).setEaseOutQuart();

            GameObject cosmeticLayer = new GameObject("Cosmetic Layer", new Type[] {typeof(RectTransform)});
            RectTransform t_cosmetic = cosmeticLayer.GetComponent<RectTransform>();
            t_cosmetic.SetParent(bottomPanel.transform, false);
            t_cosmetic.sizeDelta = t3.sizeDelta;

            GameObject contentLayer = new GameObject("Content Layer", new Type[] { typeof(RectTransform) });
            contentLayer.transform.SetParent(bottomPanel.transform, false);
            contentLayer.GetComponent<RectTransform>().sizeDelta = t3.sizeDelta;
        }

        internal static IEnumerator StartCosmeticLayer(WildfrostMod mod, RectTransform parent)
        {
            yield return Sequences.Wait(dur);
            if (!Instance.enabled) { yield break; }
            yield return ModInspectComp.RunCosmetic(mod, parent);
        }

        internal static IEnumerator StartContentLayer(WildfrostMod mod, RectTransform parent)
        {
            yield return Sequences.Wait(dur);
            if (!Instance.enabled) { yield break; }

            GameObject obj = new GameObject("Vertical Group");
            RectTransform t = obj.AddComponent<RectTransform>();
            t.SetParent(parent.transform, false);
            t.pivot = new Vector2(0, 0.5f);
            t.sizeDelta = new Vector2(2, 5f);
            t.Translate(new Vector3(parent.rect.xMin+0.2f, 0,0));
            VerticalLayoutGroup group = obj.AddComponent<VerticalLayoutGroup>();
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = false;
            group.spacing = 0.1f;

            t.NewButton("Button 1", Vector3.zero, new Vector2(2.2f, 0.6f), UI.OffYellow, ToModConfigs)
                .AddLayoutElement(new Vector2(2.2f, 0.6f), Vector2.zero, Vector2.zero)
                .WithText(0.25f, Vector3.zero, 0.1f*Vector2.one, "Mod Config", Color.black);
            /*t.NewButton("Button 2", Vector3.zero, new Vector2(2f, 0.6f), UI.OffYellow, ToModConfigs)
                .AddLayoutElement(new Vector2(2.2f, 0.6f), Vector2.zero, Vector2.zero)
                .WithText(0.25f, Vector3.zero, 0.1f * Vector2.one, "More Mod Configuration", Color.black);
            t.NewButton("Button 3", Vector3.zero, new Vector2(2f, 0.9f), UI.OffYellow, ToModConfigs)
                .AddLayoutElement(new Vector2(2.2f, 1.6f), Vector2.zero, Vector2.zero)
                .WithText(0.25f, Vector3.zero, 0.1f * Vector2.one, "Even More Mod Configurations!!!", Color.black);*/

            parent.WithText(0.25f, new Vector3(0.2f, parent.rect.yMin + 0.3f), new Vector2(0, parent.sizeDelta.y - 2), $"GUID: {mod.GUID}\nLast Updated: {Directory.GetLastWriteTime(mod.ModDirectory)}", Color.white, TMPro.TextAlignmentOptions.Left);

            ModInspectComp.RunContent(mod, parent);
            StabilizerEvents.InvokeModInspect(mod, parent);
        }

        internal static void ToModConfigs()
        {
            ConfigSection section = WildfrostHopeMod.ConfigManager.GetConfigSection(Mod);
            if (section == null || !Mod.HasLoaded || section.items.Count == 0)
            {
                Debug.Log("[Stabilizer] No Config Section found.");
                return;
            }
            
            References.instance.StartCoroutine(JournalSequence());
        }
        //Canvas/SafeArea/Menu/Journal/Positioner/Pages/Page1/
        //Canvas/SafeArea/Menu/Journal/Positioner/Pages/Page2/ModSettings/ScrollView/ (SmoothScrollRect)
        //Canvas/SafeArea/Menu/Journal/Positioner/Pages/Page2/ModSettings/Scroll View/Viewport/Content

        static float scale = 1f;
        static float fade = 0.3f;

        internal static IEnumerator JournalSequence()
        {
            //Cannot lookup Canvas directly because the name is ambiguous
            GameObject modCanvas = Instance.transform.root.gameObject;
            Image background = modCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>();

            LeanTween.value(modCanvas,
            (f) =>
            {
                modCanvas.transform.localScale = (f) * Vector3.one;
                background.color = background.color.WithAlpha(0.702f * f);
            },
            1, 0, fade).setEaseInQuart();

            yield return Sequences.Wait(fade);

            modCanvas.SetActive(false);


            PauseMenu pausemenu = GameObject.FindObjectOfType<PauseMenu>(true);
            pausemenu.Settings();

            yield return null;

            GameObject settings = GameObject.Find("Canvas/SafeArea/Menu/Journal/Positioner/Pages/Page1/SettingsMenu/");
            if (settings != null)
            {
                Debug.Log("[Stabilizer] Found Settings");
                Transform modSettings = settings.transform.GetAllChildren().FirstOrDefault((t) => t.name.Contains("ModSettings"));
                if (modSettings != null)
                {
                    Debug.Log("[Stabilizer] Found Mod Settings");
                    Button button = modSettings.GetComponentInChildren<Button>();
                    button?.onClick?.Invoke();
                    yield return null;

                    GameObject section = GameObject.Find("Canvas/SafeArea/Menu/Journal/Positioner/Pages/Page2/ModSettings/Scroll View/Viewport/Content/" + Mod.GUID);
                    if (section != null)
                    {
                        Debug.Log("[Stabilizer] Found Section");
                        SmoothScrollRect scroller = section.transform.parent.parent.parent.GetComponent<SmoothScrollRect>();

                        RectTransform rt = section.transform as RectTransform;
                        float y = rt.localPosition.y;
                        scroller?.ScrollTo(-scale*new Vector2(0, y + rt.sizeDelta.y/2));
                    }
                }
            }

            GameObject journal = GameObject.Find("Canvas/SafeArea/Menu/Journal");
            yield return new WaitUntil(() => journal == null || !journal.activeInHierarchy);

            modCanvas?.SetActive(true);

            LeanTween.value(modCanvas,
            (f) =>
            {
                modCanvas.transform.localScale = (f) * Vector3.one;
                background.color = background.color.WithAlpha(0.702f * f);
            },
            0, 1, fade).setEaseOutQuart();

            
        }
    }
}
