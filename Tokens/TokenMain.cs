using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tokens
{
    public class TokenMain : WildfrostMod
    {
        public TokenMain(string modDirectory) : base(modDirectory)
        {
        }

        public override string GUID => "mhcdc9.wildfrost.tokens";

        public override string[] Depends => new string[0];

        public override string Title => "Tokens v1.0";

        public override string Description => "Tokens are a new type of card upgrade (token icons are clickable!). They can be obtained by asking Goblings nicely for them.";

        public static bool OverrideDrag = false;

        public static readonly List<CardUpgradeData> tokenList = new List<CardUpgradeData>();

        public CardData.StatusEffectStacks SStack(string name, int count) => new CardData.StatusEffectStacks(Get<StatusEffectData>(name), count);

        public static TargetConstraint OnlyUnits()
        {
            TargetConstraintPlayType constraint = ScriptableObject.CreateInstance<TargetConstraintPlayType>();
            constraint.targetPlayType = Card.PlayType.Place;
            return constraint;
        }

        public static TargetConstraint OnlyItems()
        {
            TargetConstraintPlayType constraint = ScriptableObject.CreateInstance<TargetConstraintPlayType>();
            constraint.targetPlayType = Card.PlayType.Play;
            return constraint;
        }

        public static TargetConstraint HasHealth()
        {
            return ScriptableObject.CreateInstance<TargetConstraintHasHealth>();
        }

        public static TargetConstraint DoesAttacks()
        {
            return ScriptableObject.CreateInstance<TargetConstraintDoesAttack>();
        }

        public static TargetConstraint IsBoostable()
        {
            return ScriptableObject.CreateInstance<TargetConstraintCanBeBoosted>();
        }

        public static GameObject TokenPrefab;
        public static GameObject HolderPrefab;
        public static GameObject Holder;
        public static GameObject takeTokenButton;

        private static DeckSelectSequence deckSelect;

        private List<CardUpgradeDataBuilder> upgrades;
        private List<StatusEffectDataBuilder> effects;
        private List<KeywordDataBuilder> keywords;
        private bool preLoaded = false;
        private void CreateModAssets()
        {
            upgrades = new List<CardUpgradeDataBuilder>()
            {
                new CardUpgradeDataBuilder(this)
                .Create("potiontoken")
                .SetCanBeRemoved(true)
                .WithImage("potionToken.png")
                .WithText("Restore <keyword=health> equal to number of uses\nUses: <4>\n(Ends turn!)")
                .WithTier(2)
                .WithTitle("Pinkberry Tonic")
                .WithType(CardUpgradeData.Type.Token)
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        data.targetConstraints = new TargetConstraint[]{ OnlyUnits(), HasHealth() };
                        data.effects = new CardData.StatusEffectStacks[]{new CardData.StatusEffectStacks(Get<StatusEffectData>("Potion Token"),4)};
                        tokenList.Add(data);
                    }),

                new CardUpgradeDataBuilder(this)
                .Create("swordtoken")
                .SetCanBeRemoved(true)
                .WithImage("swordToken.png")
                .WithText("Deal <2> damage to front enemey\nUses: <2>\n(Free action)")
                .WithTier(2)
                .WithTitle("Trusty Sword")
                .WithType(CardUpgradeData.Type.Token)
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        data.targetConstraints = new TargetConstraint[]{ OnlyUnits() };
                        data.effects = new CardData.StatusEffectStacks[]{new CardData.StatusEffectStacks(Get<StatusEffectData>("Sword Token"),2)};
                        tokenList.Add(data);
                    }),

                new CardUpgradeDataBuilder(this)
                .Create("lumintoken")
                .SetCanBeRemoved(true)
                .WithImage("luminToken.png")
                .WithText("Increase all effects by <1> until end of turn\nUses: <2>\n(Free action)")
                .WithTier(2)
                .WithTitle("Lumin Juice")
                .WithType(CardUpgradeData.Type.Token)
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        data.targetConstraints = new TargetConstraint[]{ IsBoostable() };
                        data.effects = new CardData.StatusEffectStacks[]{new CardData.StatusEffectStacks(Get<StatusEffectData>("Lumin Token"),2)};
                        tokenList.Add(data);
                    }),

                new CardUpgradeDataBuilder(this)
                .Create("bowtoken")
                .SetCanBeRemoved(true)
                .WithImage("bowToken.png")
                .WithText("Gain <keyword=longshot> until end of turn\nUnlimited Uses\n(Free action)")
                .WithTier(2)
                .WithTitle("Berrywood Bow")
                .WithType(CardUpgradeData.Type.Token)
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        data.targetConstraints = new TargetConstraint[]{ OnlyUnits(), DoesAttacks() };
                        data.effects = new CardData.StatusEffectStacks[]{new CardData.StatusEffectStacks(Get<StatusEffectData>("Bow Token"),1)};
                        tokenList.Add(data);
                    }),

                new CardUpgradeDataBuilder(this)
                .Create("fisttoken")
                .SetCanBeRemoved(true)
                .WithImage("fistToken.png")
                .WithText("Gain <keyword=smackback> until end of turn\nUses: <1>\n(Ends turn!)")
                .WithTier(2)
                .WithTitle("Fighter's Mark")
                .WithType(CardUpgradeData.Type.Token)
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        data.targetConstraints = new TargetConstraint[]{ OnlyUnits(), DoesAttacks() };
                        data.effects = new CardData.StatusEffectStacks[]{new CardData.StatusEffectStacks(Get<StatusEffectData>("Fist Token"),1)};
                        tokenList.Add(data);
                    }),

                new CardUpgradeDataBuilder(this)
                .CreateToken("decktoken","Hidden Ace")
                .WithImage("deckToken.png")
                .WithText("Move this item (from anywhere!) to the top of your draw pile\nUses: <1>\n(Free action)")
                .WithTier(2)
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        data.targetConstraints = new TargetConstraint[]{ OnlyItems() };
                        data.effects = new CardData.StatusEffectStacks[]{SStack("Deck Token",1)};
                        tokenList.Add(data);
                    }),

            };

            CreateTokenIcon("potionToken", this.ImagePath("potionToken.png").ToSprite(), "potionToken", "snow", Color.white);
            CreateTokenIcon("swordToken", this.ImagePath("swordToken.png").ToSprite(), "swordToken", "snow", Color.white);
            CreateTokenIcon("luminToken", this.ImagePath("luminToken.png").ToSprite(), "luminToken", "snow", Color.white);
            CreateTokenIcon("bowToken", this.ImagePath("bowToken.png").ToSprite(), "bowToken", "", Color.white);
            CreateTokenIcon("fistToken", this.ImagePath("fistToken.png").ToSprite(), "fistToken", "snow", Color.white);
            CreateTokenIcon("deckToken", this.ImagePath("deckToken.png").ToSprite(), "deckToken", "snow", Color.white);

            effects = new List<StatusEffectDataBuilder>()
            {
                new StatusEffectDataBuilder(this)
                .Create<StatusTokenApplyX>("Potion Token")
                .WithCanBeBoosted(false)
                .WithIconGroupName("counter")
                .WithIsStatus(true)
                .WithStackable(false)
                .WithType("potionToken")
                .WithVisible(true)
                .FreeModify<StatusTokenApplyX>(
                    (data) =>
                    {
                        data.fromHand = true;
                        data.finiteUses = true;
                        data.doPing = false;
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                        data.targetConstraints = new TargetConstraint[0];
                        data.effectToApply = Get<StatusEffectData>("Heal");
                        data.endTurn = true;
                    }),

                new StatusEffectDataBuilder(this)
                .Create<StatusTokenApplyX>("Sword Token")
                .WithCanBeBoosted(false)
                .WithIconGroupName("counter")
                .WithIsStatus(true)
                .WithStackable(false)
                .WithType("swordToken")
                .WithVisible(true)
                .FreeModify<StatusTokenApplyX>(
                    (data) =>
                    {
                        data.hitDamage = 2;
                        data.doPing = false;
                        data.finiteUses = true;
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.FrontEnemy;
                        data.targetConstraints = new TargetConstraint[0];
                        data.effectToApply = null;
                        data.applyEqualAmount = true;
                        data.endTurn = false;
                    }),

                new StatusEffectDataBuilder(this)
                .Create<StatusTokenApplyX>("Lumin Token")
                .WithCanBeBoosted(false)
                .WithIconGroupName("counter")
                .WithIsStatus(true)
                .WithStackable(false)
                .WithType("luminToken")
                .WithVisible(true)
                .FreeModify<StatusTokenApplyX>(
                    (data) =>
                    {
                        data.fromHand = true;
                        data.fixedAmount = 1;
                        data.doPing = false;
                        data.finiteUses = true;
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                        data.targetConstraints = new TargetConstraint[0];
                        data.applyEqualAmount = true;
                        data.endTurn = false;
                    })
                .SubscribeToAfterAllBuildEvent(
                    delegate(StatusEffectData data)
                    {
                        StatusTokenApplyX data2 = (StatusTokenApplyX) data;
                        data2.effectToApply = Get<StatusEffectData>("Boost Effects Until Turn End");
                    }),

                new StatusEffectDataBuilder(this)
                .Create<StatusEffectBoostUntilTurnEnd>("Boost Effects Until Turn End")
                .WithCanBeBoosted(false)
                .WithIsStatus(false)
                .WithStackable(true)
                .WithType("")
                .WithVisible(false)
                .FreeModify<StatusEffectBoostUntilTurnEnd>(
                    (data) =>
                    {
                        data.targetConstraints = new TargetConstraint[0];
                    }),

                new StatusEffectDataBuilder(this)
                .Create<StatusTokenApplyX>("Bow Token")
                .WithCanBeBoosted(false)
                .WithIconGroupName("counter")
                .WithIsStatus(true)
                .WithStackable(false)
                .WithType("bowToken")
                .WithVisible(true)
                .FreeModify<StatusTokenApplyX>(
                    (data) =>
                    {
                        data.fromHand = true;
                        data.fixedAmount = 1;
                        data.finiteUses = false;
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                        data.targetConstraints = new TargetConstraint[0];
                        data.applyEqualAmount = true;
                        data.endTurn = false;
                    })
                .SubscribeToAfterAllBuildEvent(
                    delegate(StatusEffectData data)
                    {
                        StatusTokenApplyX data2 = (StatusTokenApplyX) data;
                        data2.effectToApply = Get<StatusEffectData>("Longshot Until Turn End");
                    }),

                new StatusEffectDataBuilder(this)
                .Create<StatusTokenApplyX>("Fist Token")
                .WithCanBeBoosted(false)
                .WithIconGroupName("counter")
                .WithIsStatus(true)
                .WithStackable(false)
                .WithType("fistToken")
                .WithVisible(true)
                .FreeModify<StatusTokenApplyX>(
                    (data) =>
                    {
                        data.fixedAmount = 1;
                        data.finiteUses = true;
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                        data.targetConstraints = new TargetConstraint[0];
                        data.applyEqualAmount = true;
                        data.endTurn = true;
                    })
                .SubscribeToAfterAllBuildEvent(
                    delegate(StatusEffectData data)
                    {
                        StatusTokenApplyX data2 = (StatusTokenApplyX) data;
                        data2.effectToApply = Get<StatusEffectData>("Smackback Until Turn End");
                    }),

                new StatusEffectDataBuilder(this)
                .Create<StatusEffectTraitUntilTurnEnd>("Longshot Until Turn End")
                .WithCanBeBoosted(false)
                .WithIsStatus(false)
                .WithStackable(true)
                .WithType("")
                .WithVisible(false)
                .FreeModify<StatusEffectTraitUntilTurnEnd>(
                    (data) =>
                    {
                        data.trait = Get<TraitData>("Longshot");
                        data.targetConstraints = new TargetConstraint[0];
                    }),

                new StatusEffectDataBuilder(this)
                .Create<StatusEffectTraitUntilTurnEnd>("Smackback Until Turn End")
                .WithCanBeBoosted(false)
                .WithIsStatus(false)
                .WithStackable(true)
                .WithType("")
                .WithVisible(false)
                .FreeModify<StatusEffectTraitUntilTurnEnd>(
                    (data) =>
                    {
                        data.trait = Get<TraitData>("Smackback");
                        data.targetConstraints = new TargetConstraint[0];
                    }),

                new StatusEffectDataBuilder(this)
                .Create<StatusEffectGiveUpgradeOnDeath>("Give Token When Destroyed")
                .WithCanBeBoosted(false)
                .WithStackable(false)
                .WithType("")
                .WithText("When destroyed, gain a <keyword=mhcdc9.wildfrost.tokens.token>."),

                new StatusEffectDataBuilder(this)
                .CreateStatusToken<StatusTokenMoveContainer>("Deck Token", "deckToken")
                .FreeModify<StatusTokenMoveContainer>(
                    (data) =>
                    {
                        data.fromBoard = false;
                        data.fromDiscard = true;
                        data.fromHand = true;
                        data.fromDraw = true;
                        data.finiteUses = true;
                        data.targetConstraints = new TargetConstraint[0];
                        data.toContainer = StatusTokenMoveContainer.Container.DrawPile;
                        data.top = true;
                    })
            };

            keywords = new List<KeywordDataBuilder>()
            {
                new KeywordDataBuilder(this)
                .Create("token")
                .WithTitle("Token")
                .WithDescription("A clickable icon that can be assigned (and removed) from cards|Max 1 token per card")
                .WithShowName(true),
            };

            preLoaded = true;
        }

        private GameObject CreateTokenIcon(string name, Sprite sprite, string type, string copyTextFrom, Color textColor)
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

            return gameObject;
        }

        public override List<T> AddAssets<T, Y>()           
        {
            var typeName = typeof(Y).Name;
            switch (typeName)                               
            {
                case nameof(CardUpgradeData):
                    return upgrades.Cast<T>().ToList();
                case nameof(StatusEffectData):
                    return effects.Cast<T>().ToList();
                case nameof(KeywordData):
                    return keywords.Cast<T>().ToList();
                default:
                    return null;
            }
        }

        public override void Load()
        {
            if (!preLoaded) { CreateModAssets(); }
            base.Load();
            CreateTokenPrefab();
            CreateTokenHolder();
            Events.OnCardDataCreated += Gobling;
            Events.OnSceneLoaded += SceneLoaded;
            DisableDrag();
        }

        public override void Unload()
        {
            base.Unload();
            TokenPrefab.Destroy();
            Holder.Destroy();
            Events.OnCardDataCreated -= Gobling;
            Events.OnSceneLoaded -= SceneLoaded;
        }

        public void DisableDrag()
        {
            if (!OverrideDrag)
            {
                Events.OnCheckEntityDrag += ButtonExt.DisableDrag;
            }  
        }

        private void Gobling(CardData cardData)
        {
            if (cardData.name == "Gobling")
            {
                cardData.startWithEffects = CardData.StatusEffectStacks.Stack(cardData.startWithEffects, new CardData.StatusEffectStacks[]
                {
                    new CardData.StatusEffectStacks(Get<StatusEffectData>("Give Token When Destroyed"),1)
                });
            }
        }

        private void CreateTokenPrefab()
        {
            TokenPrefab = new GameObject();
            TokenPrefab.SetActive(false);
            TokenPrefab.name = "Token";
            //((RectTransform)TokenPrefab.transform).sizeDelta = new Vector2(1, 1);
            Image image = TokenPrefab.AddComponent<Image>();
            image.sprite = this.ImagePath("tokenTest.png").ToSprite();
            //UINavigationItem
            UINavigationItem item = TokenPrefab.AddComponent<UINavigationItem>();
            item.selectionPriority = UINavigationItem.SelectionPriority.Highest;
            item.clickHandler = TokenPrefab;
            //TouchHandler
            TouchHandler touchHandler = TokenPrefab.AddComponent<TouchHandler>();
            touchHandler.hoverBeforePress = false;
            //UpgradeDisplay
            UpgradeDisplay display = TokenPrefab.AddComponent<UpgradeDisplay>();
            display.navigationItem = item;
            display.image = image;
            //CardCharmInteraction
            CardCharmInteraction interaction = TokenPrefab.AddComponent<CardCharmInteraction>();
            interaction.canDrag = true;
            interaction.canHover = true;
            interaction.image = TokenPrefab;
            //interaction.dragHandler Obtained later
            UnityEngine.Object.DontDestroyOnLoad(TokenPrefab);
            RectTransform rect = TokenPrefab.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0.8f, 0.8f);
        }

        private void CreateTokenHolder()
        {
            HolderPrefab = new GameObject();
            HolderPrefab.SetActive(false);
            HolderPrefab.name = "TokenHolder";
            HolderPrefab.AddComponent<TokenHolder>();
            UnityEngine.Object.DontDestroyOnLoad(HolderPrefab);
        }

        private void SceneLoaded(Scene scene)
        {
            if (scene.name == "UI")
            {
                GameObject deckDisplay = GameObject.FindObjectOfType<DeckDisplaySequence>(true)?.gameObject;
                Transform borderRight = null;
                foreach (Transform transform in deckDisplay.GetComponentsInChildren<Transform>())
                {
                    if (transform.name == "BorderLeft")
                    {
                        borderRight = transform;
                        break;
                    }
                }
                Holder = GameObject.Instantiate(HolderPrefab, borderRight);
                Holder.transform.SetSiblingIndex(0);
                Holder.transform.localPosition = new Vector3(1.35f, 1.3f, 0f);
                Holder.GetComponent<TokenHolder>().dragHandler = deckDisplay.GetComponentInChildren<CardCharmDragHandler>(true);
                Holder.SetActive(true);

                deckSelect = GameObject.FindObjectOfType<DeckSelectSequence>(true);
                foreach (Transform transform in deckSelect.GetComponentsInChildren<Transform>())
                {
                    if (transform.name == "TakeCrown")
                    {
                        takeTokenButton = transform.gameObject.InstantiateKeepName();
                        takeTokenButton.name = "TakeToken";
                        takeTokenButton.transform.SetParent(transform.parent);
                        takeTokenButton.transform.SetSiblingIndex(2);
                        takeTokenButton.transform.localScale = new Vector3(0.8f, 0.8f, 1);
                        break;
                    }
                }
                ButtonAnimator animator = takeTokenButton.GetComponentInChildren<ButtonAnimator>();
                animator.baseColour = new Color(0.96f, 0.875f, 0.589f, 1);
                Button button = takeTokenButton.GetComponentInChildren<Button>();
                button.image.sprite = this.ImagePath("takeToken.png").ToSprite();
                button.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
                button.onClick.AddListener(TakeToken);

                //Randomize token list
                for(int i=tokenList.Count-1; i>=0; i--)
                {
                    if (tokenList[i] == null)
                    {
                        tokenList.RemoveAt(i);
                    }
                }
                ((StatusEffectGiveUpgradeOnDeath)Get<StatusEffectData>("Give Token When Destroyed")).data = tokenList.InRandomOrder().ToList();
            }
        }

        public static bool EntityHasRemoveableToken(CardData cardData)
        {
            CardUpgradeData token = GetToken(cardData);
            if ((bool)token)
            {
                return token.canBeRemoved;
            }

            return false;
        }

        private static void TakeToken()
        {
            Entity entity = deckSelect.entity;
            CardUpgradeData token = GetToken(entity.data).Clone();
            if ((object)token != null)
            {
                entity.StartCoroutine(RemoveToken(entity));
                References.PlayerData.inventory.upgrades.Add(token);
                TokenHolder tokenHolder = Holder.GetComponent<TokenHolder>();
                tokenHolder.Create(token);
                tokenHolder.SetPositions();
            }
        }

        private static CardUpgradeData GetToken(CardData data)
        {
            return data.upgrades.Find((CardUpgradeData a) => a.type == CardUpgradeData.Type.Token);
        }

        private static IEnumerator RemoveToken(Entity entity)
        {
            CardData data = entity.data;
            CardUpgradeData token = GetToken(data);
            List<CardData.StatusEffectStacks> effectsApplied = new List<CardData.StatusEffectStacks>();
            foreach(CardData.StatusEffectStacks stacks in token.effects)
            {
                foreach(CardData.StatusEffectStacks stacks2 in entity.data.startWithEffects)
                {
                    if (stacks.data == stacks2.data)
                    {
                        effectsApplied.Add(stacks2);
                        break;
                    }
                }
            }
            token.startWithEffectsApplied = effectsApplied;
            GetToken(data).UnAssign(data);
            
            yield return entity.ClearStatuses();
            if (entity.display is Card card)
            {
                yield return card.UpdateData();
            }
        }
    }
    //BorderRight
    //UINavigation
    //-priority = highest
    //-clickhandler = self(gameObject)
    //TouchHandler
    //-Hoverbeforepressed = false
    //UpgradeDisplay
    //-data
    //-image
    //-navigation item
    //CardCharmInteraction
    //-canDrag
    //-canHover
    //-DragHandler (CardCharmDragHandler)
    //-image (GameObject)

    //CharacterDisplay
    //-> DeckDisplay (DeckDisplaySequence)
    //-> BorderRight

    //CardCharmDragHandler is on DeckDisplay
    //Scale = Vector3(0.01, 0.01, 1)
    //Local position = (-1.5, 2.5, 0)

    /*
     * Crown Button
     * name: TakeCrown (DeckDisplay -> AboveDeckpackIcon -> Select Companion -> Group -> ButtonGroup -> TakeCrown)
     * TakeCrown has an animator has a button.
     * Two persistent calls: 
     * 0 - DeckSelectSequence -> TakeCrown
     * 1 - UISequence -> End
     * 0.9412 0.7059 0.2667 1
     * */
    [HarmonyPatch(typeof(DeckSelectSequence),"SetEntity",new Type[] { typeof(Entity), typeof(bool) })]
    internal static class TakeTokenButton
    {
        internal static void Postfix(DeckSelectSequence __instance)
        {
            TokenMain.takeTokenButton.SetActive((bool)__instance.entity && TokenMain.EntityHasRemoveableToken(__instance.entity.data) && (!References.Battle || References.Battle.ended));
        }
    }

    [HarmonyPatch(typeof(DeckDisplaySequence), "Run", new Type[] {})]
    internal static class AddHolder
    {
        internal static void Prefix()
        {
            TokenHolder tokenHolder = TokenMain.Holder.GetComponent<TokenHolder>();
            tokenHolder.Clear();
            foreach (CardUpgradeData upgrade in References.Player.data.inventory.upgrades)
            {
                switch (upgrade.type)
                {
                    case CardUpgradeData.Type.Token:
                        tokenHolder.Create(upgrade);
                        break;
                }
            }
            tokenHolder.SetPositions();
        }
    }

    [HarmonyPatch(typeof(CardUpgradeData), "Display", new Type[] { typeof(Entity) })]
    internal static class DisplayOverride
    {
        internal static bool Prefix(CardUpgradeData __instance)
        {
            if (__instance.type == CardUpgradeData.Type.Token)
            {
                return false;
            }
            return true;
        }
    }

    public class TokenHolder : UpgradeHolder
    {
        [SerializeField]
        private float xGap = 0.7f;

        [SerializeField]
        private float yGap = -0.7f;

        public override UpgradeDisplay Create(CardUpgradeData upgradeData)
        {
            //AsyncOperationHandle<GameObject> asyncOperationHandle = prefabRef.InstantiateAsync(base.transform, false);
            //asyncOperationHandle.WaitForCompletion();
            GameObject token = TokenMain.TokenPrefab.InstantiateKeepName();
            UpgradeDisplay component = token.GetComponent<UpgradeDisplay>();
            component.gameObject.SetActive(value: true);
            component.SetData(upgradeData);
            component.name = upgradeData.name;
            if ((bool)dragHandler)
            {
                CardCharmInteraction component2 = component.GetComponent<CardCharmInteraction>();
                if ((object)component2 != null)
                {
                    component2.dragHandler = dragHandler;
                    component2.onDrag.AddListener(dragHandler.Drag);
                    component2.onDragEnd.AddListener(dragHandler.Release);
                }
            }

            Add(component);
            return component;
        }

        public override void SetPositions()
        {
            Vector2 zero = Vector2.zero;
            Vector3 zero2 = Vector3.zero;
            int alternate = 0;
            foreach (RectTransform item in base.transform)
            {
                item.anchoredPosition = zero;
                item.localEulerAngles = zero2;

                zero += new Vector2(-2*(alternate-0.5f)*xGap , alternate * yGap);
                alternate = 1-alternate;
            }
        }
    }
}
