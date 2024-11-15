using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Stabilizer.TileView
{
    public class ModTile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /*
         * Structure: 
         * Tile (RectTransform, Image, ModTile)
         * Icon Border (RectTransform, Image, Event Trigger)
         * Icon (RectTransform, Image, Button)
         */

        public static GameObject Prefab;
        public static float borderDiff = 0.15f;
        public static float iconDiff = 0.3f;

        public WildfrostMod Mod => mod;
        WildfrostMod mod;

        public GameObject Border => border;
        GameObject border;

        public GameObject Icon => icon;
        GameObject icon;

        public GameObject Mark => mark;
        GameObject mark;

        private void OnEnable()
        {

            Events.OnModLoaded += ModLoaded;
            Events.OnModUnloaded += ModUnloaded;
        }

        private void OnDisable()
        {
            Events.OnModLoaded -= ModLoaded;
            Events.OnModUnloaded -= ModUnloaded;
        }

        internal void ModLoaded(WildfrostMod mod)
        {
            if (mod == this.Mod)
            {
                Activate();
            }
        }

        internal void ModUnloaded(WildfrostMod mod)
        {
            if (mod == this.Mod)
            {
                Deactivate();
            }
        }

        public void Activate()
        {
            SetColors(new Color(0.2f,0.95f,0.75f), new Color(0.55f, 0.35f, 0.30f),  Color.white);
        }

        public void Deactivate()
        {
            SetColors(new Color(0.4f, 0.2f, 0.2f), new Color(0f, 0f, 0f, 0f),  new Color(0.8f, 0.8f, 0.8f));
        }

        private void Setup(GameObject border, GameObject icon, GameObject mark)
        {
            this.border = border;
            border.transform.SetParent(transform, false);
            this.icon = icon;
            icon.transform.SetParent(transform, false);
            this.mark = mark;
            mark.transform.SetParent(transform, false);
        }

        public void SetSize(float size)
        {
            Vector2 baseSize = size * Vector2.one;
            RectTransform t = GetComponent<RectTransform>();
            t.sizeDelta = baseSize;
            t = border.GetComponent<RectTransform>(); 
            t.sizeDelta = (size - borderDiff) * Vector2.one;
            t = icon.GetComponent<RectTransform>();
            t.sizeDelta = (size - iconDiff) * Vector2.one;
            t = mark.GetComponent<RectTransform>();
            SetMark(MarkerManager.EditPhase != 2 && Mod != null);
        }

        public void SetMark(bool findMark = true)
        {
            float size = GetComponent<RectTransform>().sizeDelta.x;
            RectTransform t = mark.GetComponent<RectTransform>();
            bool editMode = (MarkerManager.EditPhase == 2);
            float scaleFactor = editMode ? (size - borderDiff) : 0.25f * size;
            t.sizeDelta = scaleFactor * Vector2.one;
            t.transform.localPosition = editMode ? Vector3.zero : ((size - scaleFactor)/2) * new Vector3(1, -1, 0);
            TextMeshProUGUI text = mark.GetComponent<TextMeshProUGUI>();
            text.fontSize = 0.8f * scaleFactor;
            text.color = (editMode) ? new Color(1, 1, 1, 0.5f) : Color.white;
            if (findMark) { SetMark(FindMark()); }
        }

        public void SetMark(string symbol)
        {
            mark.GetComponent<TextMeshProUGUI>().text = symbol;
        }

        public string FindMark()
        {
            var mark = MarkerManager.markers.Values.FirstOrDefault(m => m.visibleOnTiles && m.members.Contains(Mod.GUID));
            if (mark != null)
            {
                return mark.symbol;
            }
            return "";
        }

        public void SetMod(WildfrostMod mod)
        {
            Debug.Log(mod.Title);
            this.mod = mod;
            name = $"Tile ({mod.Title})";
            border = GetComponentsInChildren<Transform>().FirstOrDefault((t) => t.name == "Border")?.gameObject;
            icon = GetComponentsInChildren<Transform>().FirstOrDefault((t) => t.name == "Icon")?.gameObject;
            icon.GetComponent<Image>().sprite = mod.IconSprite;
            mark = GetComponentsInChildren<Transform>().FirstOrDefault((t) => t.name == "Symbol")?.gameObject;
            Button b = GetComponent<Button>();
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(ToggleMod);
            
            if (Mod != null)
            {
                if (Mod.HasLoaded)
                {
                    Activate();
                }
                else
                {
                    Deactivate();
                }
            }
        }

        public void ToggleMod()
        {
            if (MarkerManager.EditPhase == 2)
            {
                SetMark("");
                if (MarkerManager.EditMarker.ToggleElement(MarkerManager.EditMarker.members, Mod.GUID))
                {
                    SetMark(MarkerManager.EditMarker.symbol);
                }
                SfxSystem.OneShot(FMODUnity.RuntimeManager.PathToEventReference("event:/sfx/modifiers/mod_bell_ringing"));
                return;
            }
            mod.ModToggle();
            if (Mod.HasLoaded)
            {
                SfxSystem.OneShot(FMODUnity.RuntimeManager.PathToEventReference("event:/sfx/modifiers/daily_bell_ringing"));
            }
        }

        public void SetColors(Color mainColor, Color borderColor, Color iconColor)
        {
            Image i = GetComponent<Image>();
            i.color = mainColor;
            i = border.GetComponent<Image>();
            i.color = borderColor;
            i = icon.GetComponent<Image>();
            i.color = iconColor;
        }

        public static GameObject GetPrefab()
        {
            if (Prefab != null) { return Prefab; }

            Prefab = new GameObject("Tile Prefab", new Type[] {typeof(RectTransform), typeof(Image), typeof(ModTile), typeof(UINavigationItem), typeof(Button) });
            GameObject border = new GameObject("Border", new Type[] {typeof(RectTransform), typeof(Image)});
            GameObject icon = new GameObject("Icon", new Type[] { typeof(RectTransform), typeof(Image) });
            GameObject mark = new GameObject("Symbol", new Type[] { typeof(TextMeshProUGUI) });

            mark.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            ModTile m = Prefab.GetComponent<ModTile>();
            m.Setup(border, icon, mark);
            m.SetSize(1);

            GameObject.DontDestroyOnLoad(Prefab);
            Prefab.SetActive(false);

            Prefab.transform.SetParent(Stabilizer.prefabHolder.transform);
            return Prefab;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            TileViewManager.hover = this;
            float size = GetComponent<RectTransform>().sizeDelta.x;
            icon.GetComponent<RectTransform>().sizeDelta = (size - borderDiff) * Vector2.one;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (TileViewManager.hover == this)
            {
                TileViewManager.hover = null;
            }
            float size = GetComponent<RectTransform>().sizeDelta.x;
            icon.GetComponent<RectTransform>().sizeDelta = (size - iconDiff) * Vector2.one;
        }
    }

    
}
