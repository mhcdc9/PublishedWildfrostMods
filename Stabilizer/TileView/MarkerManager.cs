using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Threading;
using HarmonyLib;
using Deadpan.Enums.Engine.Components.Modding;

namespace Stabilizer.TileView
{
    public class MarkerManager : MonoBehaviour
    {
        public class Marker
        {
            internal List<string> members = new List<string>();
            internal string title = "";
            internal string symbol = "<sprite name=vim>";

            internal bool startup = false;
            internal bool visibleOnTiles = false;
            internal bool hidden = false;

            internal static string[] vanillaSymbols = { "crown", "health", "scrap", "lumin", "snow", "frost"};

            //Title
            //Symbol
            //startup,visibleOnTiles,hidden
            //LIST
            //(The GUID of the mods)
            public Marker(string path)
            {
                if (!File.Exists(path)) { return; }

                string[] lines = File.ReadAllLines(path);
                this.title = lines[0].Trim();
                this.symbol = lines[1].Trim();
                bool list = false;
                for (int i = 2; i < lines.Length; i++)
                {
                    if (!list)
                    {
                        switch (lines[i].Trim().ToLower())
                        {
                            case "startup":
                                if (hidden) 
                                { 
                                    hiddenMarkers.Add(this); 
                                }
                                else 
                                { 
                                    activeMarkers.Add(this); 
                                }
                                startup = true; break;
                            case "hidden":
                                if (startup) 
                                { 
                                    hiddenMarkers.Add(this);
                                    activeMarkers.Remove(this);
                                }
                                hidden = true; break;
                            case "visible": visibleOnTiles = true; break;
                            case "list": list = true; break;
                        }
                    }
                    else
                    {
                        if (lines[i].IsNullOrWhitespace()) continue;

                        members.Add(lines[i].Trim());
                    }
                }
            }

            public Marker(string title, List<string> members, string symbol)
            {
                this.title = title;
                this.members = members;
                this.symbol = symbol;
            }

            public string SaveAsFile()
            {
                List<string> list = new List<string> { title.ToLower(), symbol};
                if (startup) { list.Add("startup"); }
                if (visibleOnTiles) { list.Add("visible"); }
                if (hidden) { list.Add("hidden"); }
                list.Add("list");
                foreach(string member in members)
                {
                    list.Add(member);
                }
                string fileName = $"{title.ToLower()}.txt";
                File.WriteAllLines(Path.Combine(MarkerPath, fileName), list);
                return fileName;

            }

            public Transform highlightBox;
            public void Assign(Transform t)
            {
                highlightBox = UI.NewButton(title, t, Vector3.zero, new Vector2(0.6f, 0.6f), new Color(1, 1, 1, 0f), Toggle)
                .WithBox(Vector3.zero, new Vector2(0.05f,0.05f), new Color(1,1,1,0))
                .WithText(0.3f, new Vector3(0, -0.03f, 0), Vector2.zero, symbol, Color.black)
                .parent;
                SetColor();
            }

            public void SetColor()
            {
                if (!highlightBox) { return; }

                Color c = ModInspectView.Enabled
                    ? ( members.Contains(ModInspectView.Mod.GUID) ? UI.OffYellow : UI.Empty )
                    : ( activeMarkers.Contains(this) ? UI.OffYellow : (hiddenMarkers.Contains(this) ? UI.Reddish : UI.Empty) );


                highlightBox.GetComponent<Image>().color = c;
            }

            public void SetTransparency(float alpha)
            {
                if (!highlightBox) { return; }

                highlightBox.GetChild(0).GetComponent<TextMeshProUGUI>().alpha = alpha;
            }

            public void Toggle()
            {
                if (EditPhase == 1)
                {
                    MarkerSelectedForEditing(this);
                    return;
                }

                if (ModInspectView.Enabled && ModInspectView.Mod != null)
                {
                    ToggleElement(members, ModInspectView.Mod.GUID);
                    SetColor();
                    if (TileViewManager.newContent?.activeSelf == true)
                    {
                        TileViewManager.newContent.GetComponentsInChildren<ModTile>().FirstOrDefault(t => t.Mod == ModInspectView.Mod)?.SetMark();
                    }
                    return;
                }

                ToggleElement(hidden ? hiddenMarkers : activeMarkers, this);
                SetColor();
                TileViewManager.Filter();
            }

            public bool ToggleElement<T>(List<T> list, T value)
            {
                if (list.Contains(value))
                {
                    list.Remove(value);
                    return false;
                }
                else
                {
                    list.Add(value);
                    return true;
                }
            }

            public void Assign(GameObject obj, string title = null, List<string> members = null,  int symbol = 4)
            {
                Assign(obj, title, members, $"<sprite name={vanillaSymbols[symbol % vanillaSymbols.Length]}>");
            }
            public void Assign(GameObject obj, string title = null, List<string> members = null, string symbol = ":P")
            {
                this.members = members ?? this.members;
                if (title == null)
                {
                    for(int i=0; i<20; i++)
                    {
                        if (!markers.ContainsKey($"Group {i}"))
                        {
                            this.title = $"Group {i}";
                            break;
                        }
                    }
                }
                else
                {
                    this.title = title;
                }
                obj.name = $"Tag ({this.title})";

                obj.GetComponentInChildren<TextMeshProUGUI>().text = this.symbol;
            }


        }

        public static SortedDictionary<string, Marker> markers = new SortedDictionary<string, Marker>();
        public static List<Marker> activeMarkers = new List<Marker>();
        public static List<Marker> hiddenMarkers = new List<Marker>();

        public static int EditPhase = 0;
        public static Marker EditMarker = null;
        public static string MarkerPath => Path.Combine(Stabilizer.Instance.ModDirectory, "markers");
        public static string CatalogPath => Path.Combine(MarkerPath, "catalog.txt");

        public static void LoadDictionary()
        {
            if (File.Exists(CatalogPath))
            {
                string[] lines = File.ReadAllLines(CatalogPath).Where( (s) => File.Exists(Path.Combine(MarkerPath, s)) ).ToArray();
                for(int i = 0; i < lines.Length; i++)
                {
                    Marker marker = new Marker(Path.Combine(MarkerPath, lines[i]));
                    markers[i.ToString("D2") + "-" + marker.title] = marker;
                }
            }
            else
            {
                markers["00-Quick Access"] = new Marker("Quick Access", new List<string> { }, "<sprite name=crown>");
                markers["01-Favorites"] = new Marker("Favorites", new List<string> { }, "<sprite name=health>")
                {
                    visibleOnTiles = true
                };
                markers["02-Local Mods"] = new Marker("Local Mods", new List<string> { }, "<sprite name=scrap>");
                markers["03-Battles"] = new Marker("Battles", new List<string> { }, "<sprite name=attack>");
                markers["04-Spicy"] = new Marker("Spicy", new List<string> { }, "<sprite name=spice>");
                markers["05-Cosmetic"] = new Marker("Cosmetic", new List<string> { }, "<sprite name=lumin>");
                markers["06-Frost"] = new Marker("Hidden", new List<string> { }, "<sprite name=frost>")
                {
                    startup = true,
                    visibleOnTiles = true,
                    hidden = true
                };
            }
        }

        public static void SaveDictionary()
        {
            Directory.CreateDirectory(MarkerPath);
            string[] lines = markers.Values.Select( m => m.SaveAsFile() ).ToArray();
            File.WriteAllLines(CatalogPath, lines);
        }

        public static bool Satisfies(string s)
        {
            return ((activeMarkers.Count == 0 || activeMarkers.Any(m => m.members.Contains(s)))
                && !hiddenMarkers.Any(m => m.members.Contains(s)));
        }

        public static void ResetMarkerColors()
        {
            foreach(var marker in markers.Values)
            {
                marker.SetColor();
            }
        }

        public static Vector3 position = new Vector3(-6.02f, 2.2f, 0);
        public static float width = 0.6f;
        public static RectTransform t_edit;

        public static MarkerManager CreateTagManager(Transform t)
        {
            GameObject parent = new GameObject("Tag Manager");
            parent.transform.SetParent(t);
            parent.transform.localPosition = position;

            GameObject editMarkersButton = UI.NewButton("Edit Button", parent.transform, new Vector3(-.6f, -.1f, 0), new Vector2(1.75f, 0.45f), UI.OffYellow, StartEditing)
                .WithText(0.4f, Vector3.zero, Vector2.zero, "Edit", Color.black, TextAlignmentOptions.Center).parent.gameObject;
            t_edit = editMarkersButton.GetComponent<RectTransform>();
            t_edit.pivot = new Vector2(1f, 0.5f);
            t_edit.eulerAngles = new Vector3(0, 0, 90);

            GameObject vGroup = new GameObject("Tag List", new Type[] { typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup) });
            vGroup.GetComponent<RectTransform>().sizeDelta = new Vector2(width, markers.Count*0.6f);
            vGroup.GetComponent<RectTransform>().pivot = new Vector2(0.5f,1f);
            vGroup.GetComponent<Image>().color = new Color(1f, 0.7f, 0.5f, 0f);
            vGroup.transform.SetParent(parent.transform, false);

            VerticalLayoutGroup _group = vGroup.GetComponent<VerticalLayoutGroup>();
            _group.childAlignment = TextAnchor.UpperCenter;
            _group.childForceExpandHeight = true;

            foreach(var val in markers.Values)
            {
                val.Assign(vGroup.transform);
            }

            return parent.AddComponent<MarkerManager>();
        }

        public static void StartEditing()
        {
            if (EditPhase == 0)
            {
                EditPhase = 1;
                t_edit.GetComponent<Image>().color = Color.white;
                ChangeText("Choose");
            }
            else
            {
                EditPhase = 0;
                t_edit.GetComponent<Image>().color = UI.OffYellow;
                markers.Values.Do(m => m.SetTransparency(1f));
                ChangeText("Edit");
                TileViewManager.EndEditMode();
            }
        }

        public static void MarkerSelectedForEditing(Marker mark)
        {
            EditMarker = mark;
            EditPhase = 2;
            markers.Values.Where(m => m != mark).Do(m => m.SetTransparency(0.5f));
            ChangeText("Done?");
            TileViewManager.EditSymbolMode(mark);
        }

        public static void ChangeText(string text)
        {
            TextMeshProUGUI tmp = t_edit.GetChild(0).GetComponent<TextMeshProUGUI>();
            tmp.text = text;

        }

        public void OnDestroy()
        {
            EditPhase = 0;
            SaveDictionary();
        }
    }
}
