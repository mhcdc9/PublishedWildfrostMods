using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Runtime.Remoting.Messaging;
using UnityEngine.Tilemaps;

namespace Stabilizer.TileView
{
    [HarmonyPatch(typeof(ModsSceneManager), "Start")]
    internal class TileViewManager : MonoBehaviour
    {
        public static ModTile hover;

        public void Update()
        {
            if (InputSystem.IsButtonPressed("Inspect"))
            {
                if (ModInspectView.Enabled)
                {
                    ModInspectView.EndInspect();
                }
                else if (hover)
                {
                    ModInspectView.StartInspect(hover.Mod, hover.transform.position);
                }
            }
        }

        internal static bool properOpen;
        internal static bool populated = false;
        internal static GameObject newContent;
        internal static Transform Toggle;

        internal static GameObject increment;
        internal static GameObject decrement;
        static IEnumerator Postfix(IEnumerator __result, ModsSceneManager __instance)
        {
            bool notTileView = (!properOpen || !Stabilizer.Instance.startAsTileView);
            yield return __result;
            if (properOpen)
            {
                SmoothScrollRect scroll = GameObject.Find("Canvas/SafeArea/Menu/Panel/Positioner/Scroll View").GetComponent<SmoothScrollRect>();
                Transform menu = scroll.transform.parent.parent.parent;
                Toggle = menu.NewButton("Toggle Button", new Vector3(-4.3f, 5.1f), new Vector2(2f, 0.4f), UI.MenuToggle, ToggleTileView);
                (Toggle as RectTransform).WithText(0.4f, Vector3.zero, Vector2.zero, notTileView ? "List View" : "Tile View", Color.black);
                SearchBar.CreateSearchBar(menu);
                MarkerManager.CreateTagManager(menu);
            }
            populated = false;
            if  (notTileView) { properOpen = false; yield break; }

            yield return CreateTileView(__instance);
            properOpen = false;
        }

        static IEnumerator CreateTileView(ModsSceneManager __instance)
        {
            newContent = new GameObject("Content (Tile)", new Type[] { typeof(RectTransform), typeof(GridLayoutGroup), typeof(TileViewManager) });
            GameObject content = __instance.Content;
            newContent.transform.SetParent(content.transform.parent, false);
            newContent.transform.Translate(new Vector3(-0.01f, 0,0));
            float width = content.GetComponent<RectTransform>().sizeDelta.x - 0.2f;
            int amount = content.transform.childCount;
            float gapRatio = 0.05f;

            float idealTileSize = AdjustGrid(width, amount, gapRatio);

            SmoothScrollRect scroll = GameObject.Find("Canvas/SafeArea/Menu/Panel/Positioner/Scroll View").GetComponent<SmoothScrollRect>();
            scroll.content = newContent.GetComponent<RectTransform>();

            content.SetActive(false);

            Transform menu = scroll.transform.parent.parent.parent;
            increment = menu.NewButton("Plus Button", new Vector3(6.66f, 2.9f), new Vector2(0.5f, 1f), UI.OffYellow, DecreaseTilePerRow)
                .WithText(0.4f, Vector3.zero, 0.1f * Vector2.one, "+", Color.black)
                .parent.gameObject;
            decrement = menu.NewButton("Minus Button", new Vector3(6.66f, 1.7f), new Vector2(0.5f, 1f), UI.OffYellow, IncreaseTilePerRow)
                .WithText(0.4f, Vector3.zero, 0.1f * Vector2.one, "-", Color.black)
                .parent.gameObject;

            int pauseIndex = 0;
            foreach (var mod in Bootstrap.Mods)
            {
                bool active = SearchBar.Satisfies(mod.Title.ToLower()) && MarkerManager.Satisfies(mod.GUID);
                ModTile tile = Stabilizer.MakeTile(newContent.transform, mod, (1-gapRatio)*idealTileSize, active);
                //tile.transform.position = new Vector2(Dead.PettyRandom.Range(-10f, 10f), Dead.PettyRandom.Range(-5f, 5f));
                pauseIndex++;
                if (active && pauseIndex == Stabilizer.Instance.tilesPerRow) { pauseIndex = 0;  yield return Sequences.Wait(0.05f); }
            }
            populated = true;
        }

        static void ToggleTileView()
        {
            if (newContent == null)
            {
                References.instance.StartCoroutine(CreateTileView(GameObject.FindObjectOfType<ModsSceneManager>()));
                Stabilizer.Instance.startAsTileView = true;
                Stabilizer.Instance.SaveConfigs();
                Toggle.GetComponentInChildren<TextMeshProUGUI>().text = "Tile View";
                return;
            }
            bool toggle = newContent.activeSelf;
            Toggle.GetComponentInChildren<TextMeshProUGUI>().text = toggle ? "List View" : "Tile View";
            if (ModInspectView.Enabled)
            {
                ModInspectView.EndInspect();
            }
            newContent.SetActive(!toggle);
            increment?.SetActive(!toggle);
            decrement?.SetActive(!toggle);
            Filter();
            GameObject content = GameObject.FindObjectOfType<ModsSceneManager>().Content;
            content.SetActive(toggle);
            Stabilizer.Instance.startAsTileView = !toggle;
            SmoothScrollRect scroll = GameObject.Find("Canvas/SafeArea/Menu/Panel/Positioner/Scroll View").GetComponent<SmoothScrollRect>();
            scroll.content = (toggle) ? content.transform as RectTransform : newContent.transform as RectTransform;
            Stabilizer.Instance.SaveConfigs();
        }

        static void IncreaseTilePerRow()
        {
            if (Stabilizer.Instance.tilesPerRow >= 10) { return; }
            Stabilizer.Instance.tilesPerRow += 1;
            if (populated)
            {
                AdjustGrid(newContent.GetComponent<RectTransform>().sizeDelta.x, newContent.transform.childCount, 0.1f);
                UpdateTileSizes();
                Stabilizer.Instance.SaveConfigs();
            }

        }

        static void DecreaseTilePerRow()
        {
            if (Stabilizer.Instance.tilesPerRow <= 1) { return; }
            Stabilizer.Instance.tilesPerRow -= 1;
            if (populated)
            {
                AdjustGrid(newContent.GetComponent<RectTransform>().sizeDelta.x, newContent.transform.childCount, 0.1f);
                UpdateTileSizes();
                Stabilizer.Instance.SaveConfigs();
            }
        }

        static void UpdateTileSizes()
        {
            float size = newContent.GetComponent<GridLayoutGroup>().cellSize.x;
            foreach (ModTile tile in newContent.GetComponentsInChildren<ModTile>())
            {
                tile.SetSize(size);
            }
        }

        static float AdjustGrid(float width, float amount, float gapRatio)
        {
            float idealTileSize = (width / (Stabilizer.Instance.tilesPerRow));
            newContent.GetComponent<RectTransform>().sizeDelta = new Vector2(width, idealTileSize * (float)Math.Ceiling(amount / Stabilizer.Instance.tilesPerRow));

            GridLayoutGroup grid = newContent.GetComponent<GridLayoutGroup>();
            grid.cellSize = (1 - gapRatio) * idealTileSize * Vector2.one;
            grid.spacing = gapRatio * idealTileSize * Vector2.one;

            float y = newContent.GetComponent<RectTransform>().sizeDelta.y;
            newContent.transform.localPosition = new Vector3(-newContent.transform.parent.transform.localPosition.x, -y / 2, 0);

            return idealTileSize;
        }
        public static void Filter()
        {
            string s = SearchBar.text;

            if (newContent != null && newContent.activeSelf)
            {
                foreach (ModTile tile in newContent.GetComponentsInChildren<ModTile>(true))
                {
                    if (SearchBar.Satisfies(tile.Mod.Title.ToLower()) && MarkerManager.Satisfies(tile.Mod.GUID))
                    {
                        tile.gameObject.SetActive(true);
                    }
                    else
                    {
                        tile.gameObject.SetActive(false);
                    }
                }
            }
            else
            {

                ModsSceneManager manager = GameObject.FindObjectOfType<ModsSceneManager>();
                foreach (ModHolder holder in manager.Content.GetComponentsInChildren<ModHolder>(true))
                {
                    if (SearchBar.Satisfies(holder.Mod.Title.ToLower()) && MarkerManager.Satisfies(holder.Mod.GUID))
                    {
                        holder.gameObject.SetActive(true);
                    }
                    else
                    {
                        holder.gameObject.SetActive(false);
                    }
                }
            }
        }

        public static void EditSymbolMode(MarkerManager.Marker mark)
        {
            if (newContent == null) { return; }

            List<string> members = mark.members;
            string symbol = mark.symbol;
            foreach (ModTile tile in newContent.GetComponentsInChildren<ModTile>(true))
            {
                tile.SetMark(false);
                tile.SetMark(members.Contains(tile.Mod.GUID) ? symbol : "");
            }
        }

        public static void EndEditMode()
        {
            if (newContent == null) { return; }

            newContent.GetComponentsInChildren<ModTile>(true).Do(t => t.SetMark());
        }
    }
}
