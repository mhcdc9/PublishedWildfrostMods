using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Tokens
{
    public static class Extensions
    {
        [Flags]
        public enum CardPlaces
        {
            None = 0,
            Board = 1,
            Hand = 2,
            BoardAndHand = 3,
            Draw = 4,
            Discard = 8,
            Everywhere = 15
        }

        public static CardUpgradeDataBuilder AddTokenPool(this CardUpgradeDataBuilder builder, string @class)
        {
            return builder.SubscribeToAfterAllBuildEvent(
                (data) =>
                {
                    AddTokenPool(data, @class);
                });
        }

        public static void AddTokenPool(this CardUpgradeData data, string @class)
        {
            if (!TokenMain.TokenRewards.ContainsKey(@class))
            {
                TokenMain.TokenRewards[@class] = new List<CardUpgradeData> { data };
            }
            else
            {
                TokenMain.TokenRewards[@class].Add(data);
            }
        }

        public static bool Includes(this IStatusToken token, CardPlaces cardPlace) => (token.ValidPlaces & cardPlace) != 0;
        public static CardUpgradeDataBuilder CreateToken(this CardUpgradeDataBuilder b, string name, string title, int tier = 2)
        {
            return b.Create(name)
                .WithTitle(title)
                .SetCanBeRemoved(true)
                .WithType(CardUpgradeData.Type.Token)
                .WithTier(tier);
        }

        public static bool CorrectPlace(this IStatusToken token, Entity target)
        {
            if (token.Includes(CardPlaces.Board) && Battle.IsOnBoard(target))
            {
                return true;
            }
            if (token.Includes(CardPlaces.Hand) && References.Player.handContainer.Contains(target))
            {
                return true;
            }
            if (token.Includes(CardPlaces.Draw) && target.preContainers.Contains(References.Player.drawContainer))
            {
                return true;
            }
            if (token.Includes(CardPlaces.Discard) && target.preContainers.Contains(References.Player.discardContainer))
            {
                return true;
            }
            return false;
        }

        public static StatusEffectDataBuilder CreateStatusToken<T>(this StatusEffectDataBuilder b, string name, string type) where T : StatusEffectData
        {
            return b.Create<T>(name)
                .WithCanBeBoosted(false)
                .WithIconGroupName("counter")
                .WithIsStatus(true)
                .WithStackable(false)
                .WithType(type)
                .WithVisible(true);
        }

        public static KeywordDataBuilder CreateBasicKeyword(this WildfrostMod mod, string name, string title, string desc)
        {
            return new KeywordDataBuilder(mod)
                .Create(name)
                .WithTitle(title)
                .WithDescription(desc)
                .WithShowName(true);
        }

        public static KeywordDataBuilder ChooseColors(this KeywordDataBuilder builder, Color? titleColor = null, Color? bodyColor = null, Color? noteColor = null)
        {
            return builder.WithTitleColour(titleColor)
                .WithBodyColour(bodyColor)
                .WithNoteColour(noteColor);
        }

        public static KeywordData RegisterKeyword(this KeywordDataBuilder builder)
        {
            KeywordData data = builder.Build();
            AddressableLoader.AddToGroup<KeywordData>("KeywordData", data);
            return data;
        }

        public static GameObject CreateTokenIcon(string name, Sprite sprite, string type, string copyTextFrom, Color textColor)
        {
            GameObject gameObject = new GameObject(name);
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            gameObject.SetActive(false);
            StatusIconExt icon = gameObject.AddComponent<StatusIconExt>();
            Dictionary<string, GameObject> cardIcons = CardManager.cardIcons;
            icon.animator = gameObject.AddComponent<ButtonAnimator>();
            icon.button = gameObject.AddComponent<ButtonExt>();
            icon.animator.button = icon.button;
            if (!copyTextFrom.IsNullOrEmpty())
            {
                GameObject text = cardIcons[copyTextFrom].GetComponentInChildren<TextMeshProUGUI>().gameObject.InstantiateKeepName();
                text.transform.SetParent(gameObject.transform);
                icon.textElement = text.GetComponent<TextMeshProUGUI>();
                icon.textColour = textColor;
                icon.textColourAboveMax = textColor;
                icon.textColourBelowMax = textColor;
            }
            icon.onCreate = new UnityEngine.Events.UnityEvent();
            icon.onDestroy = new UnityEngine.Events.UnityEvent();
            icon.onValueDown = new UnityEventStatStat();
            icon.onValueUp = new UnityEventStatStat();
            icon.afterUpdate = new UnityEngine.Events.UnityEvent();
            UnityEngine.UI.Image image = gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            CardHover cardHover = gameObject.AddComponent<CardHover>();
            cardHover.enabled = false;
            cardHover.IsMaster = false;
            CardPopUpTarget cardPopUp = gameObject.AddComponent<CardPopUpTarget>();
            cardHover.pop = cardPopUp;
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.sizeDelta *= 0.008f;
            gameObject.SetActive(true);
            icon.type = type;
            cardIcons[type] = gameObject;

            gameObject.AddComponent<UINavigationItem>();

            gameObject.hideFlags = HideFlags.HideInInspector;

            return gameObject;
        }

        public static GameObject AddKeywords(this GameObject gameObject, params KeywordData[] keys)
        {
            CardPopUpTarget cardPopUp = gameObject.GetComponent<CardPopUpTarget>();
            cardPopUp.keywords = keys;
            return gameObject;
        }

        public static KeywordDataBuilder AddToIcons(this KeywordDataBuilder builder, string iconType, bool includeTokenKeyword = true)
        {
            return builder.SubscribeToAfterAllBuildEvent(
                (data) =>
                {
                    if (!CardManager.cardIcons.ContainsKey(iconType))
                    {
                        throw new Exception($"Could not find the icon: {iconType}");
                    }
                    GameObject icon = CardManager.cardIcons[iconType];
                    CardPopUpTarget cardPopUp = icon.GetComponent<CardPopUpTarget>();
                    if (includeTokenKeyword)
                    {
                        cardPopUp.keywords = new KeywordData[2] { TokenMain.TokenKeyword(), data };
                    }
                    else
                    {
                        cardPopUp.keywords = new KeywordData[1] { data };
                    }
                });
        }
    }

    
}
