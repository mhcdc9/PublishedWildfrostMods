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

namespace MultiplayerBase.Matchmaking
{
    internal class LobbyView : MonoBehaviour
    {
        GameObject lobbyGroup;
        GameObject memberGroup;

        public static LobbyView Create(Transform transform)
        {
            GameObject obj = new GameObject("Lobby View");
            //obj.AddComponent<Image>();
            //obj.GetComponent<RectTransform>().sizeDelta = new Vector2(6, 6);
            LobbyView lv = obj.AddComponent<LobbyView>();
            lv.lobbyGroup = HelperUI.VerticalGroup("Lobby Group", obj.transform, new Vector2(6, 6), spacing: 0.2f);
            lv.transform.SetParent(transform);
            return lv;
        }

        static Vector2 dim = new Vector2(4f, 6f);
        static Vector2 dim2 = new Vector2(6f, 1f);
        static int amountOfFakes = 5;
        static Vector3 pos = new Vector3(-3f, 0, 0);
        static Vector2 memberDim = new Vector2(4f, 0.8f);
        static Vector3 posIcon = new Vector3(-1.6f, 0, 0);
        static Vector3 posText = new Vector3(1f, 0, 0);
        static Vector2 iconDim = new Vector2(0.8f, 0.8f);
        static Vector2 textDim = new Vector2(3f, 0.8f);
        static float spacing = 0.15f;

        public void makeFakeMembers()
        {
            if (memberGroup != null)
            {
                memberGroup.transform.DestroyAllChildren();
                memberGroup.Destroy();
            }
            memberGroup = HelperUI.VerticalGroup("Member Group", transform, dim, spacing: spacing);
            memberGroup.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
            memberGroup.transform.Translate(pos);
            memberGroup.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            VerticalLayoutGroup vertical = memberGroup.GetComponent<VerticalLayoutGroup>();
            vertical.childAlignment = TextAnchor.UpperLeft;
            GameObject textElement = new GameObject("Member Title");
            textElement.transform.SetParent(memberGroup.transform, false);
            TextMeshProUGUI text = textElement.AddComponent<TextMeshProUGUI>();
            text.text = "<size=0.6>Current Lobby: Blah</size>\n<size=0.4f>Members</size>";
            textElement.GetComponent<RectTransform>().sizeDelta = dim2;
            for (int i=0; i<amountOfFakes; i++)
            {
                GameObject obj1 = HelperUI.ButtonTemplate(memberGroup.transform, memberDim, new Vector3(0,0,0), $"This is Name {i}", new Color(0.7f,0.7f,0.7f) ).gameObject;
                GameObject obj2 = new GameObject("Icon");
                obj2.AddComponent<Image>().color = new Color(0.7f, 0.3f, 0.3f);
                obj2.transform.SetParent(obj1.transform, false);
                obj2.GetComponent<RectTransform>().sizeDelta = iconDim;
                obj2.transform.localPosition = posIcon;
                TextMeshProUGUI memberName = obj1.GetComponentInChildren<TextMeshProUGUI>();
                memberName.alignment = TextAlignmentOptions.CaplineLeft;
                memberName.transform.localPosition = posText;
            }
        }
    }
}

