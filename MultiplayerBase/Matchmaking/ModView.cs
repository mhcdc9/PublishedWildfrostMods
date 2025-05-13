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
        static Vector3 defaultPosition = new Vector3(6f,-0.4f,0f);
        static Vector2 dim = new Vector2(6f, 8.2f);
        static Vector2 innerDim = new Vector2(5f, 7.5f);
        static float titleSize = 0.6f;
        static float bodySize = 0.3f;

        string hostTitle = $"<size={titleSize}><color=#FC8>My Mods</color></size>";
        string clientTitle = $"<size={titleSize}><color=#FC8>Mods</color></size>";
        string[] hostMods;

        TweenUI exitTween;

        public static ModView Create(Transform transform)
        {
            GameObject obj = new GameObject("Mod View");
            obj.SetActive(false);
            obj.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);
            obj.GetComponent<RectTransform>().sizeDelta = dim;
            obj.transform.Translate(defaultPosition);
            //Tween1: Enter
            TweenUI tween = obj.AddComponent<TweenUI>();
            tween.target = obj;
            tween.property = TweenUI.Property.Move;
            tween.ease = LeanTweenType.easeOutElastic;
            tween.fireOnEnable = true;
            tween.duration = 1f;
            tween.to = defaultPosition;
            tween.hasFrom = true;
            tween.from = defaultPosition + new Vector3(8, 0, 0);

            //Tween2: Exit
            tween = obj.AddComponent<TweenUI>();
            tween.target = obj;
            tween.property = TweenUI.Property.Move;
            tween.ease = LeanTweenType.easeOutQuart;
            tween.disableAfter = true;
            tween.duration = 1f;
            tween.to = defaultPosition + new Vector3(9,0,0);

            ModView mv = obj.AddComponent<ModView>();
            mv.transform.SetParent(transform);
            GameObject obj2 = new GameObject("Text");
            obj2.transform.SetParent(obj.transform, false);
            mv.textElement = obj2.AddComponent<TextMeshProUGUI>();
            mv.exitTween = tween;
            obj2.GetComponent<RectTransform>().sizeDelta = innerDim;
            return mv;
        }

        public static string ActiveModListAsString()
        {
            List<string> mods = ActiveModList();
            string s = string.Join(", ", mods.Select((modName) => modName.Replace(",", ",|")));
            return s;
        }

        public static List<string> ActiveModList()
        {
            return Bootstrap.Mods.Where((mod) => mod.HasLoaded).Select((mod) => mod.Title).ToList();
        }

        public void OpenModView(Lobby lobby, bool isHost)
        {
            gameObject.SetActive(true);
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
                    s2 += myMod + " (Yours)\n";
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
                    s3 += mod + " (Host) \n";
                }
            }
            textElement.text = s1 + $"\n<size={bodySize}>" + s2 + s3 + s4;
        }

        public void CloseModView()
        {
            exitTween.Fire();
        }
    }
}
