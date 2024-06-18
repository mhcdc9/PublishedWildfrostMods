using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SColor = Steamworks.Data.Color;
using SSprite = Steamworks.Data.Image;
using Color = UnityEngine.Color;
using UnityEngine.UI;
using Steamworks;
using MultiplayerBase.Handlers;
using TMPro;

namespace MultiplayerBase.UI
{
    public class FriendIcon : MonoBehaviour
    {
        protected Friend friend;
        protected TextMeshProUGUI textElement;
        public static FriendIcon Create(Transform transform, Vector2 dim, Vector3 pos, Friend friend)
        {
            Task<Steamworks.Data.Image?> imageTask = friend.GetLargeAvatarAsync();
            Button button = HelperUI.ButtonTemplate(transform, dim, pos, "", Color.white);
            FriendIcon icon = button.gameObject.AddComponent<FriendIcon>();
            icon.friend = friend;
            button.onClick.AddListener(icon.FriendIconPressed);
            Image image = button.GetComponent<Image>();
            icon.textElement = icon.GetComponentInChildren<TextMeshProUGUI>();
            icon.textElement.outlineColor = Color.black;
            icon.textElement.color = Color.white;
            icon.textElement.outlineWidth = 0.1f;
            icon.textElement.transform.localScale *= 0.5f;
            icon.textElement.alignment = TextAlignmentOptions.BottomRight;
            icon.textElement.transform.Translate(new Vector3(0.25f, -0.25f), icon.transform);
            SetSprite(image, imageTask);
            return icon;
        }

        public static async void SetSprite(Image image, Task<Steamworks.Data.Image?> imageTask)
        {
            Steamworks.Data.Image? img = await imageTask;
            if (img is Steamworks.Data.Image image2)
            {
                image.sprite = GetAvatarSprite(image2);
            }
        }

        public void FriendIconPressed()
        {
            Debug.Log($"[Multiplayer] Sending Message to {friend.Name}");
            if (InspectSystem.IsActive())
            {
                HandlerInspect.SelectDisp(FindObjectOfType<InspectSystem>(true).inspect);
            }
            else if (HandlerSystem.friendStates[friend] == PlayerState.Battle)
            {
                //HandlerBattle.instance.CreateController();
                HandlerBattle.instance.ToggleViewer(friend);
            }
            else if (HandlerSystem.friendStates[friend] == PlayerState.Event)
            {
                if (friend.Id == HandlerSystem.self.Id)
                {
                    foreach(Friend friend in HandlerSystem.friends)
                    {
                        //if (friend.Id != HandlerSystem.self.Id)
                        {
                            HandlerEvent.instance.SendData(friend);
                        }
                    }
                }
                HandlerEvent.instance.AskForData(friend);
            }
            else if (HandlerSystem.friendStates[friend] == PlayerState.Map)
            {
                HandlerMap.instance.ToggleViewer(friend);
            }
            else
            {
                HandlerSystem.SendMessage("CHT", friend, Dead.PettyRandom.Range(0f, 1f).ToString());
                HandlerInspect.instance.Clear();
            }

        }


        public static Sprite GetAvatarSprite(SSprite steamSprite)
        {
            Texture2D texture = new Texture2D((int)steamSprite.Width, (int)steamSprite.Height);
            for (int i = 0; i < texture.width; i++)
            {
                for (int j = 0; j < texture.height; j++)
                {
                    texture.SetPixel(i,j, GetColor(steamSprite.GetPixel(i,(int)steamSprite.Height - j - 1)));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }

        public static Color GetColor(SColor color) => new Color(color.r / (256f), color.g / (256f), color.b / (256f), color.a / (256f));
    }
}
