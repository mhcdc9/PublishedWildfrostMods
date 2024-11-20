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
            bottomPanel.GetComponent<Image>().color = new Color(0.4f, 0.2f, 0.2f, 0.9f);
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
            StartContentLayer(mod, Instance.bottomPanel.transform.GetChild(1) as RectTransform);
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
            cosmeticLayer.transform.SetParent(bottomPanel.transform, false);
            cosmeticLayer.GetComponent<RectTransform>().sizeDelta = t3.sizeDelta;

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

        internal static void StartContentLayer(WildfrostMod mod, RectTransform parent)
        {
            ModInspectComp.RunContent(mod, parent);
            StabilizerEvents.InvokeModInspect(mod, parent);
        }
    }
}
