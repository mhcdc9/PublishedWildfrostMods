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
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using Deadpan.Enums.Engine.Components.Modding;
using UnityEngine.Localization.Tables;

namespace MultiplayerBase.UI
{
    public class FriendIcon : MonoBehaviour
    {
        protected Friend friend;
        protected TextMeshProUGUI textElement;
        protected static  KeywordData keyword;
        protected bool popped = false;
        public static FriendIcon Create(Transform transform, Vector2 dim, Vector3 pos, Friend friend)
        {
            if (keyword == null)
            {
                keyword = AddressableLoader.Get<KeywordData>("KeywordData", "mhcdc9.wildfrost.multiplayer.friend");
            }
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
            EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener(b => { icon.Pop(); });
            trigger.triggers.Add(pointerEnter);
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener(b => { icon.Unpop(); });
            trigger.triggers.Add(pointerExit);
            return icon;
        }

        public void Pop()
        {
            if (popped) { return; }
            string[] adj = { "Hearty", "Wise", "Shady", "<sprite name=crown>'d", "Bootleg", 
                "Greedy", "Wild", "<sprite name=enemy crown>'d", "Scrappy", "Sunny", 
                "Spiced-up", "Frostblooded", "Hogheaded", "Zooming", "Faithful", 
                "Furious", "Frenzied", "Overburnt", "Soulbound", "Toothy", "Shelled", 
                "Sparked", "<sprite name=snow>'d", "Gnomish", "Datermined", "Charmless",
                "High-rolling", "Ribbiting"};
            string[] noun = { "Snowdweller", "Shademancer", "Clunkmaster", "Petmaster", "Bellringer", "Pengoon", "Makoko", "Gobling", "Gnomebot", "Woodhead", "Shopkeeper", "Sunbringer", "High Roller", "Frog"};
            string title = (friend.Id == HandlerSystem.self.Id) ? $"{friend.Name} (:3)" : $"{friend.Name}";
            StringTable collection = LocalizationHelper.GetCollection("Tooltips", SystemLanguage.English);
            collection.SetString(keyword.name + "_title", title);
            string text = $"The {adj.RandomItem()} {noun.RandomItem()}\n\n";
            if (friend.Id == HandlerSystem.self.Id)
            {
                text += $"You enlisted the help of others on your journey through the storm";
            }
            CardPopUp.AssignTo((RectTransform)transform, 1f, 0.25f);
            CardPopUp.AddPanel(keyword, text);
            popped = true;
        }

        public void Unpop()
        {
            CardPopUp.RemovePanel(keyword.name);
            popped = false;
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
