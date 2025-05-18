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
        TweenUI exitTween;

        public static MemberView Create(Transform transform)
        {
            GameObject obj = new GameObject("Member View");
            obj.SetActive(false);
            obj.AddComponent<Image>().color = new Color(0f,0f,0f,0.8f);
            obj.GetComponent<RectTransform>().sizeDelta = dim;
            obj.transform.Translate(pos);

            //Tween1: Enter
            TweenUI tween = obj.AddComponent<TweenUI>();
            tween.target = obj;
            tween.property = TweenUI.Property.Move;
            tween.ease = LeanTweenType.easeOutQuart;
            tween.fireOnEnable = true;
            tween.duration = 0.75f;
            tween.to = pos;
            tween.hasFrom = true;
            tween.from = pos + new Vector3(-8,0,0);

            //Tween2: Exit
            tween = obj.AddComponent<TweenUI>();
            tween.target = obj;
            tween.property = TweenUI.Property.Move;
            tween.ease = LeanTweenType.easeOutQuart;
            tween.disableAfter = true;
            tween.duration = 0.5f;
            tween.to = pos + new Vector3(-9, 0, 0);

            MemberView mv = obj.AddComponent<MemberView>();
            mv.transform.SetParent(transform);
            mv.exitTween = tween;
            return mv;
        }

        static Vector2 dim = new Vector2(6f, 8.2f);
        static Vector2 dim2 = new Vector2(5f, 7.5f);
        static Vector3 pos = new Vector3(-6f, -0.4f, 0);
        static Vector2 memberDim = new Vector2(5f, 1f);
        static Vector2 iconDim = new Vector2(0.8f, 0.8f);
        static Vector2 textDim = new Vector2(3f, 0.8f);
        static float fontSize = 0.4f;
        static float spacing = 0.15f;

        public void OpenMemberView(Lobby lobby, bool joined, bool isHost)
        {
            gameObject.SetActive(true);
            if (memberGroup != null)
            {
                memberGroup.transform.DestroyAllChildren();
                memberGroup.Destroy();
            }
            memberGroup = HelperUI.VerticalGroup("Member Group", transform, dim2, spacing: spacing);
            //memberGroup.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 0f);
            VerticalLayoutGroup vertical = memberGroup.GetComponent<VerticalLayoutGroup>();
            vertical.childAlignment = TextAnchor.UpperLeft;
            GameObject textElement = new GameObject("Member Title");
            textElement.transform.SetParent(memberGroup.transform, false);
            TextMeshProUGUI text = textElement.AddComponent<TextMeshProUGUI>();
            text.text = "<size=0.6><color=#FC8>My Lobby</color></size>\n<size=0.4f>Members</size>";
            textElement.GetComponent<RectTransform>().sizeDelta = new Vector2(5f, 1f);
            List<Task<Steamworks.Data.Image?>> getIcons = new List<Task<Steamworks.Data.Image?>>();
            List<Image> images = new List<Image>();
            if (joined)
            {
                Debug.Log("[Multiplayer] Displaying Members");
                foreach(Friend friend in lobby.Members)
                {
                    GameObject obj1 = HelperUI.ButtonTemplateWithIcon(memberGroup.transform, memberDim, iconDim, Vector3.zero, friend.Name, new Color(0.3f, 0.3f, 0.3f), 0.1f, 0.1f).gameObject;
                    obj1.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
                    obj1.GetComponentInChildren<TextMeshProUGUI>().fontSize = fontSize;
                    obj1.GetComponentInChildren<TextMeshProUGUI>().outlineWidth = 0.15f;
                    Task<Steamworks.Data.Image?> imageTask = friend.GetLargeAvatarAsync();
                    getIcons.Add(imageTask);
                    images.Add(obj1.GetComponent<Image>());
                }
            }
            else
            {
                ulong id = ulong.Parse(lobby.GetData("id"));
                SteamId steamId = new SteamId();
                steamId.Value = id;
                Friend friend = new Friend(steamId);
                GameObject obj1 = HelperUI.ButtonTemplateWithIcon(memberGroup.transform, memberDim, iconDim, Vector3.zero, friend.Name, new Color(0.3f, 0.3f, 0.3f), 0.1f, 0.1f).gameObject;
                obj1.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
                obj1.GetComponentInChildren<TextMeshProUGUI>().fontSize = fontSize;
                Task<Steamworks.Data.Image?> imageTask = friend.GetLargeAvatarAsync();
                getIcons.Add(imageTask);
                images.Add(obj1.GetComponentInChildren<Image>());
            }
            int maxPlayers = int.Parse(lobby.GetData("maxplayers"));
            Debug.Log("[Multiplayer] Displaying Vacancies");
            for (int i=memberGroup.transform.childCount-1; i<maxPlayers; i++)
            {
                GameObject obj1 = HelperUI.ButtonTemplateWithIcon(memberGroup.transform, memberDim, iconDim, Vector3.zero, "[Vacant]", new Color(0.3f, 0.3f, 0.3f), 0.1f, 0.1f).gameObject;
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
                    Sprite sprite = FriendIcon.GetAvatarSprite(image);
                    images[i].sprite = sprite;
                    images[i].color = new Color(0.3f, 0.3f, 0.3f);
                    Image otherImage = images[i].transform.GetChild(1).GetComponent<Image>();
                    otherImage.sprite = sprite;
                    otherImage.color = Color.white;
                }
            }
        }

        public void CloseMemberView(bool disable = true)
        {
            exitTween.disableAfter = disable;
            exitTween.Fire();
        }
    }
}

