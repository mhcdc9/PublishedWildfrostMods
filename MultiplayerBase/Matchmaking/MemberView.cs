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
using Steamworks;
using TMPro;

namespace MultiplayerBase.Matchmaking
{
    internal class MemberView : MonoBehaviour
    {
        GameObject memberGroup;

        public static MemberView Create(Transform transform)
        {
            GameObject obj = new GameObject("Member View");
            obj.AddComponent<Image>().color = Color.black;
            obj.GetComponent<RectTransform>().sizeDelta = new Vector2(6, 6);
            obj.transform.Translate(pos);
            MemberView mv = obj.AddComponent<MemberView>();
            mv.transform.SetParent(transform);
            return mv;
        }

        static Vector2 dim = new Vector2(5f, 5f);
        static Vector2 dim2 = new Vector2(6f, 1f);
        static int amountOfFakes = 5;
        static Vector3 pos = new Vector3(-6f, 0.7f, 0);
        static Vector2 memberDim = new Vector2(5f, 1f);
        static Vector3 posIcon = new Vector3(-1.6f, 0, 0);
        static Vector3 posText = new Vector3(1f, 0, 0);
        static Vector2 iconDim = new Vector2(0.8f, 0.8f);
        static Vector2 textDim = new Vector2(3f, 0.8f);
        static float fontSize = 0.3f;
        static float spacing = 0.15f;

        public void MakeFakeMembers()
        {
            if (memberGroup != null)
            {
                memberGroup.transform.DestroyAllChildren();
                memberGroup.Destroy();
            }
            memberGroup = HelperUI.VerticalGroup("Member Group", transform, dim, spacing: spacing);
            memberGroup.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
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
                GameObject obj1 = HelperUI.ButtonTemplateWithIcon(memberGroup.transform, memberDim, iconDim, Vector3.zero, $"This is a really long name {i}", new Color(0.8f,0.8f,0.8f), 0.1f, 0.1f).gameObject;
                obj1.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
                obj1.GetComponentInChildren<TextMeshProUGUI>().fontSize = fontSize;
            }
        }

        public void OpenMemberView(Lobby lobby, bool joined, bool isHost)
        {
            if (memberGroup != null)
            {
                memberGroup.transform.DestroyAllChildren();
                memberGroup.Destroy();
            }
            memberGroup = HelperUI.VerticalGroup("Member Group", transform, dim, spacing: spacing);
            memberGroup.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
            VerticalLayoutGroup vertical = memberGroup.GetComponent<VerticalLayoutGroup>();
            vertical.childAlignment = TextAnchor.UpperLeft;
            GameObject textElement = new GameObject("Member Title");
            textElement.transform.SetParent(memberGroup.transform, false);
            TextMeshProUGUI text = textElement.AddComponent<TextMeshProUGUI>();
            text.text = "<size=0.6>My Lobby: Blah</size>\n<size=0.4f>Members</size>";
            textElement.GetComponent<RectTransform>().sizeDelta = dim2;
            List<Task<Steamworks.Data.Image?>> getIcons = new List<Task<Steamworks.Data.Image?>>();
            List<Image> images = new List<Image>();
            if (joined)
            {
                foreach(Friend friend in lobby.Members)
                {
                    GameObject obj1 = HelperUI.ButtonTemplateWithIcon(memberGroup.transform, memberDim, iconDim, Vector3.zero, friend.Name, new Color(0.8f, 0.8f, 0.8f), 0.1f, 0.1f).gameObject;
                    obj1.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
                    obj1.GetComponentInChildren<TextMeshProUGUI>().fontSize = fontSize;
                    Task<Steamworks.Data.Image?> imageTask = friend.GetLargeAvatarAsync();
                    getIcons.Add(imageTask);
                    images.Add(obj1.GetComponentInChildren<Image>());
                }
            }
            else
            {
                ulong id = ulong.Parse(lobby.GetData("id"));
                SteamId steamId = new SteamId();
                steamId.Value = id;
                Friend friend = new Friend(steamId);
                GameObject obj1 = HelperUI.ButtonTemplateWithIcon(memberGroup.transform, memberDim, iconDim, Vector3.zero, friend.Name, new Color(0.8f, 0.8f, 0.8f), 0.1f, 0.1f).gameObject;
                obj1.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
                obj1.GetComponentInChildren<TextMeshProUGUI>().fontSize = fontSize;
                Task<Steamworks.Data.Image?> imageTask = friend.GetLargeAvatarAsync();
                getIcons.Add(imageTask);
                images.Add(obj1.GetComponentInChildren<Image>());
            }
            int maxPlayers = int.Parse(lobby.GetData("maxplayers"));
            for(int i=memberGroup.transform.childCount; i<maxPlayers; i++)
            {
                GameObject obj1 = HelperUI.ButtonTemplateWithIcon(memberGroup.transform, memberDim, iconDim, Vector3.zero, "[Vacant]", new Color(0.8f, 0.8f, 0.8f), 0.1f, 0.1f).gameObject;
                obj1.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
                obj1.GetComponentInChildren<TextMeshProUGUI>().fontSize = fontSize;
            }
            CompleteTasks(getIcons, images);
        }

        private async void CompleteTasks(List<Task<Steamworks.Data.Image?>> tasks, List<Image> images)
        {
            for(int i=0; i<tasks.Count; i++)
            {
                Steamworks.Data.Image? result = await tasks[i];
                if (result is Steamworks.Data.Image image && images[i] != null)
                {
                    images[i].sprite = FriendIcon.GetAvatarSprite(image);
                }
            }
        }
    }
}

