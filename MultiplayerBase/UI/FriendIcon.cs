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
using System.Collections;

namespace MultiplayerBase.UI
{
    public class FriendIcon : MonoBehaviour
    {
        protected Friend friend;
        protected TextMeshProUGUI textElement;
        protected static  KeywordData keyword;
        protected bool popped = false;

        protected static bool preventClicking = false;

        public string nickname = "(Unknown)";

        protected string currentScene = "";
        protected string currentInfo = "";
        public static Dictionary<string, string> ExplanationText_Ally = new Dictionary<string,string>(){ 
            {"Battle", "Click to view <{0}'s> battle against <{1}>" },
            {"MapNew", "Click to view <{0}'s> future map choices" },
            {"Event", "Click to view <{0}'s> event choices" },
            {"MainMenu", "<{0}> is enjoying the cliff-view"},
            {"Town", "<{0}> is preparing for the journey ahead"},
            {"CharacterSelect", "<{0}> is in the middle of self-reflection" },
            {"Default", "Where did <{0}> go?" },
        };

        public static Dictionary<string, string> ExplanationText_Self = new Dictionary<string, string>(){
            {"Battle", "Click to view... your own battle against <{1}>?" },
            {"MapNew", "Click to view... a map of your map?" },
            {"Event", "Click to ask for advice from your party" },
            {"MainMenu", "You answered the call to end the storm"},
            {"Town", "You should make sure everyone is ready before heading to the gate"},
            {"CharacterSelect", "Time for some self-reflection. Who are you?" },
            {"Default", "Where are you?" },
        };

        public static Dictionary<string, string> ExplanationText_Host = new Dictionary<string, string>(){
            {"MainMenu", "<{0}> enlisted your help on this journey to the frostlands"},
            {"Town", "<{0}> enlisted your help on this journey to the frostlands"}
        };

        public static Dictionary<string, string> ExplanationText_SelfHost = new Dictionary<string, string>(){
            {"MainMenu", "<{1}> people answered your call to the frostlands"},
            {"Town", "<{1}> people answered your call to the frostlands"}
        };

        public static FriendIcon Create(Transform transform, Vector2 dim, Vector3 pos, Friend friend)
        {
            if (keyword == null)
            {
                keyword = AddressableLoader.Get<KeywordData>("KeywordData", "mhcdc9.wildfrost.multiplayer.friend");
            }
            Task<Steamworks.Data.Image?> imageTask = friend.GetLargeAvatarAsync();
            Button button = HelperUI.BetterButtonTemplate(transform, dim, pos, "", Color.white);
            button.EditButtonAnimator(Color.white, Color.white, Color.white, Color.white);
            FriendIcon icon = button.gameObject.AddComponent<FriendIcon>();
            icon.friend = friend;
            button.onClick.AddListener(icon.FriendIconPressed);
            Image image = button.GetComponent<Image>();
            icon.textElement = icon.GetComponentInChildren<TextMeshProUGUI>();
            icon.textElement.outlineColor = Color.black;
            icon.textElement.color = Color.white;
            icon.textElement.outlineWidth = 0.1f;
            icon.textElement.transform.localScale *= 0.5f*dim.x;
            icon.textElement.alignment = TextAlignmentOptions.BottomRight;
            icon.textElement.transform.Translate(new Vector3(0.25f*dim.x, -0.25f*dim.y), icon.transform);
            if (friend.Id == HandlerSystem.self.Id)
            {
                icon.nickname = CreateNickname();
            }
            SetSprite(image, imageTask);
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
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

        public static string CreateNickname()
        {
            string[] adj = { "<sprite name=crown>'d", "<sprite name=enemy crown>'d", "<sprite name=snow>'d", "Bootleg",  "Charmless",
                "Determined", "Gnomish", "Greedy", "Faithful", "Frostblooded",
                "Frenzied", "Furious", "Hearty", "High-rolling", "Hogheaded",
                "Nyoooom", "Overburnt", "Ribbiting", "Scrappy", "Shady",
                "Shelled", "Soulbound", "Sparked", "Spiced-up", "Sunny",
                "Toothy", "Wild", "Wise", "Zoomin'"};
            string[] noun = { "Snowdweller", "Shademancer", "Clunkmaster", "Petmaster", "Bellringer", 
                "Pengoon", "Makoko", "Gobling", "Gnomebot", "Woodhead", 
                "Shopkeeper", "Sunbringer", "High Roller", "Frog", "Combo Seeker", 
                "Card Sharp", "Jimbo", "Ironclad", "Silent" };
            return string.Format("The {0} {1}", adj.RandomItem(), noun.RandomItem());
        }

        public void SetNickname(string nickname)
        {
            this.nickname = nickname; 
        }

        public void SceneChanged(string scene, string extra)
        {
            currentScene = scene;
            if (friend.Id == HandlerSystem.self.Id)
            {
                if (ExplanationText_Self.ContainsKey(scene))
                {
                    currentInfo = ExplanationText_Self[scene];
                }
                else
                {
                    currentInfo = ExplanationText_Self["Default"];
                }
            }
            else
            {
                if (ExplanationText_Ally.ContainsKey(scene))
                {
                    currentInfo = ExplanationText_Ally[scene];
                }
                else
                {
                    currentInfo = ExplanationText_Ally["Default"];
                }
            }
            if (scene == "Battle")
            {
                currentInfo = currentInfo.Format(friend.Name, extra);
            }
            else
            {
                currentInfo = currentInfo.Format(friend.Name);
            }
        }

        public void Pop()
        {
            if (popped) { return; }
            
            string title = (friend.Id == HandlerSystem.self.Id) ? $"{friend.Name} (:3)" : $"{friend.Name}";
            StringTable collection = LocalizationHelper.GetCollection("Tooltips", SystemLanguage.English);
            collection.SetString(keyword.name + "_title", title);
            string text = $"{nickname}\n\n";
            text += currentInfo;
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
            SfxSystem.OneShot("event:/sfx/ui/menu_click");
            if (preventClicking) { return; }

            if (HandlerMap.instance.Blocking && (friend.Id != HandlerMap.friend?.Id || HandlerSystem.friendStates[(Friend)HandlerMap.friend] != PlayerState.Map) )
            {
                HandlerMap.instance.CloseViewer();
            }
            if (HandlerBattle.instance.Blocking && (friend.Id != HandlerBattle.friend?.Id || HandlerSystem.friendStates[(Friend)HandlerBattle.friend] != PlayerState.Battle))
            {
                HandlerBattle.instance.CloseBattleViewer();
            }
            Debug.Log($"[Multiplayer] Sending Message to {friend.Name}");
            if (InspectSystem.IsActive())
            {
                HandlerInspect.SelectDisp(FindObjectOfType<InspectSystem>(true).inspect);
            }
            else if (SceneManager.IsLoaded("BossReward"))
            {
                HandlerEvent.instance.SendRewardData(friend);
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
                        if (friend.Id != HandlerSystem.self.Id)
                        {
                            HandlerEvent.instance.SendData(friend);
                        }
                    }
                }
                else
                {
                    HandlerEvent.instance.AskForData(friend);
                }
                
            }
            else if (HandlerSystem.friendStates[friend] == PlayerState.Map)
            {
                HandlerMap.instance.ToggleViewer(friend);
            }
            else
            {
                //HandlerSystem.SendMessage("CHT", friend, Dead.PettyRandom.Range(0f, 1f).ToString());
                //HandlerInspect.instance.Clear();
            }
            StartCoroutine(Cooldown(0.2f));
        }

        public static IEnumerator Cooldown(float seconds)
        {
            preventClicking = true;
            yield return new WaitForSeconds(seconds);
            preventClicking = false;
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
