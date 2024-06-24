using MultiplayerBase.UI;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
using Color = UnityEngine.Color;
using TMPro;
using static AvatarPoser;
using Deadpan.Enums.Engine.Components.Modding;

namespace MultiplayerBase.Matchmaking
{
    internal class ModView : MonoBehaviour
    {
        TextMeshProUGUI textElement;
        static Vector3 defaultPosition = new Vector3(6f,0.7f,0f);
        static Vector2 dim = new Vector2(6f, 6f);
        static Vector2 innerDim = new Vector2(5f, 5.5f);
        static float titleSize = 0.4f;
        static float bodySize = 0.3f;

        string hostTitle = $"<size={titleSize}><color=#FC5>Your Mods</color></size>";
        string clientTitle = $"<size={titleSize}><color=#FC5>Mods (Differences in <color=#F33>Red</color> and <color=#888>Gray</color>)</color></size>";
        string[] hostMods;

        public static ModView Create(Transform transform)
        {
            GameObject obj = new GameObject("Member View");
            obj.AddComponent<Image>().color = Color.black;
            obj.GetComponent<RectTransform>().sizeDelta = new Vector2(6, 6);
            obj.transform.Translate(defaultPosition);
            ModView mv = obj.AddComponent<ModView>();
            mv.transform.SetParent(transform);
            GameObject obj2 = new GameObject("Text");
            obj2.transform.SetParent(obj.transform, false);
            mv.textElement = obj2.AddComponent<TextMeshProUGUI>();
            obj2.GetComponent<RectTransform>().sizeDelta = innerDim;
            return mv;
        }

        public static string ActiveModListAsString()
        {
            List<string> mods = ActiveModList();
            mods.RemoveAt(3);
            mods.Add("The GRAY (not grey) Mod");
            string s = string.Join(", ", mods.Select((modName) => modName.Replace(",", ",|")));
            return s;
        }

        public static List<string> ActiveModList()
        {
            return Bootstrap.Mods.Where((mod) => mod.HasLoaded).Select((mod) => mod.Title).ToList();
        }

        public void OpenModView(Lobby lobby, bool isHost)
        {
            string s1 = isHost ? hostTitle : clientTitle;
            string modList = lobby.GetData("mods");
            string[] mods = modList.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            mods = mods.Select( (mod) => mod.Replace(",|", ",") ).ToArray();
            string s2 = "<color=#F33>";
            List<string> myMods = ActiveModList();
            foreach(string myMod in myMods)
            {
                if (!modList.Contains(myMod))
                {
                    s2 += myMod + "\n";
                }
            }
            s2 += "</color>";
            string s3 = "<color=#888>";
            string s4 = "<color=#FFF>";
            foreach(string mod in mods)
            {
                if (myMods.Contains(mod))
                {
                    s4 += mod + "\n";
                }
                else
                {
                    s3 += mod + "\n";
                }
            }
            textElement.text = s1 + $"\n<size={bodySize}>" + s2 + s3 + s4;
        }
    }
}
