using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static StatusEffectBonusDamageEqualToX;
using static UINavigationHistory;
using UnityEngine.XR;

namespace Tokens
{
    public class TokenMain : WildfrostMod
    {
        public TokenMain(string modDirectory) : base(modDirectory)
        {
            instance = this;
        }

        public override string GUID => "mhcdc9.wildfrost.tokens";

        public override string[] Depends => new string[0];

        public override string Title => "Tokens v2.1.3";

        public override string Description => "Tokens are a new type of card upgrade (token icons are clickable!). They can be obtained by asking Goblings nicely for them.\n\n\n" +
            "Think of tokens as equipment/consumables. " +
            "Clicking a token during a battle will provide a small effect. " +
            "They can also be moved around between battles. " +
            "There are 11 different tokens (8 general, 3 class-exclusive) that have varied effects (see icon picture). " +
            "Tokens may be a free action or end your turn.\n\n" +
            "The developer can be contacted through Steam and/or Discord (@Michael C).";

        public static bool OverrideDrag = false;

        public static readonly Dictionary<string, List<CardUpgradeData>> TokenRewards = new Dictionary<string, List<CardUpgradeData>>();

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

        public static TargetConstraint IsCardType(params string[] bannedTypes)
        {
            TargetConstraintIsCardType constraint = ScriptableObject.CreateInstance<TargetConstraintIsCardType>();
            constraint.allowedTypes = bannedTypes.Select(x => instance.Get<CardType>(x)).ToArray();
            constraint.not = true;
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
        private List<TraitData> traits;
        private bool preLoaded = false;

        public static TokenMain instance;
        private static KeywordData tokenKeyword;

        public static KeywordData TokenKeyword()
        {
            if (tokenKeyword == null)
            {
                tokenKeyword = Extensions.CreateBasicKeyword(instance, "token", "Token", "A clickable icon that can be assigned to and removed from cards|1 token per card\n(blocked by snow and ink)")
                    .RegisterKeyword();
            }
            return tokenKeyword;
        }

        private readonly string RemoveableString = "\n<size=0.21><color=#888>Removable</color></size>";
        private void CreateModAssets()
        {
            upgrades = new List<CardUpgradeDataBuilder>()
            {
                new CardUpgradeDataBuilder(this)
                .CreateToken("CardUpgradePotion", "Potion Token")
                .WithImage("potionToken.png")
                .WithText("Equip <keyword=mhcdc9.wildfrost.tokens.potiontoken>" + RemoveableString)
                .AddTokenPool("General")
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        data.targetConstraints = new TargetConstraint[]{ OnlyUnits(), HasHealth() };
                        data.effects = new CardData.StatusEffectStacks[]{SStack("Potion Token",4)};
                    }),

                new CardUpgradeDataBuilder(this)
                .CreateToken("CardUpgradeSword", "Sword Token")
                .WithImage("swordToken.png")
                .WithText("Equip <keyword=mhcdc9.wildfrost.tokens.swordtoken>" + RemoveableString)
                .AddTokenPool("General")
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        data.targetConstraints = new TargetConstraint[]{ OnlyUnits() };
                        data.effects = new CardData.StatusEffectStacks[]{SStack("Sword Token",2)};
                    }),

                new CardUpgradeDataBuilder(this)
                .CreateToken("CardUpgradeLumin", "Lumin Token")
                .WithImage("luminToken.png")
                .WithText("Equip <keyword=mhcdc9.wildfrost.tokens.lumintoken>" + RemoveableString)
                .AddTokenPool("General")
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        data.targetConstraints = new TargetConstraint[]{ IsBoostable() };
                        data.effects = new CardData.StatusEffectStacks[]{SStack("Lumin Token", 2) };
                    }),

                new CardUpgradeDataBuilder(this)
                .CreateToken("CardUpgradeBow", "Bow Token")
                .WithImage("bowToken.png")
                .WithText("Equip <keyword=mhcdc9.wildfrost.tokens.bowtoken>" + RemoveableString)
                .AddTokenPool("General")
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        data.targetConstraints = new TargetConstraint[]{ OnlyUnits(), DoesAttacks() };
                        data.effects = new CardData.StatusEffectStacks[]{SStack("Bow Token", 1) };
                    }),

                new CardUpgradeDataBuilder(this)
                .CreateToken("CardUpgradeFist", "Fist Token")
                .WithImage("fistToken.png")
                .WithText("Equip <keyword=mhcdc9.wildfrost.tokens.fisttoken>" + RemoveableString)
                .AddTokenPool("General")
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        data.targetConstraints = new TargetConstraint[]{ OnlyUnits(), DoesAttacks() };
                        data.effects = new CardData.StatusEffectStacks[]{SStack("Fist Token", 1) };
                    }),

                new CardUpgradeDataBuilder(this)
                .CreateToken("CardUpgradeDeck","Deck Token")
                .WithImage("deckToken.png")
                .WithText("Equip <keyword=mhcdc9.wildfrost.tokens.decktoken>" + RemoveableString)
                .AddTokenPool("General")
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        data.targetConstraints = new TargetConstraint[]{ IsCardType("Leader") };
                        data.effects = new CardData.StatusEffectStacks[]{SStack("Deck Token",1)};
                    }),

                new CardUpgradeDataBuilder(this)
                .CreateToken("CardUpgradePrism","Prism Token")
                .WithImage("prismToken.png")
                .WithText("Equip <keyword=mhcdc9.wildfrost.tokens.prismtoken>" + RemoveableString)
                .AddTokenPool("General")
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        data.targetConstraints = new TargetConstraint[]{ OnlyUnits() };
                        data.effects = new CardData.StatusEffectStacks[]{SStack("Prism Token",1)};
                    }),

                new CardUpgradeDataBuilder(this)
                .CreateToken("CardUpgradeFrost","Frost Token")
                .WithImage("frostToken.png")
                .WithText("Equip <keyword=mhcdc9.wildfrost.tokens.frosttoken>" + RemoveableString)
                .AddTokenPool("General")
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        data.targetConstraints = new TargetConstraint[]{ DoesAttacks() };
                        data.effects = new CardData.StatusEffectStacks[]{SStack("Frost Token",2)};
                    }),

                new CardUpgradeDataBuilder(this)
                .CreateToken("CardUpgradeSpice","Spice Token")
                .WithImage("spiceToken.png")
                .WithText("Equip <keyword=mhcdc9.wildfrost.tokens.spicetoken>" + RemoveableString)
                .AddTokenPool("Player (Basic)")
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        data.targetConstraints = new TargetConstraint[]{ OnlyUnits(), DoesAttacks() };
                        data.effects = new CardData.StatusEffectStacks[]{SStack("Spice Token",1)};
                    }),

                new CardUpgradeDataBuilder(this)
                .CreateToken("CardUpgradeTeeth","Teeth Token")
                .WithImage("teethToken.png")
                .WithText("Equip <keyword=mhcdc9.wildfrost.tokens.teethtoken>" + RemoveableString)
                .AddTokenPool("Player (Magic)")
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        data.targetConstraints = new TargetConstraint[]{ HasHealth() };
                        data.effects = new CardData.StatusEffectStacks[]{SStack("Teeth Token",1)};
                    }),

                new CardUpgradeDataBuilder(this)
                .CreateToken("CardUpgradeJunk","Junk Token")
                .WithImage("junkToken.png")
                .WithText("Equip <keyword=mhcdc9.wildfrost.tokens.junktoken>" + RemoveableString)
                .AddTokenPool("Player (Clunk)")
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        data.targetConstraints = new TargetConstraint[]{ OnlyUnits() };
                        data.effects = new CardData.StatusEffectStacks[]{SStack("Junk Token",1)};
                    }),
            };

            keywords = new List<KeywordDataBuilder>()
            {
                Extensions.CreateBasicKeyword(this, "potiontoken", "Pinkberry Tonic", "<End Turn>: Restore <keyword=health> equal to number of uses|Uses per battle: 4").AddToIcons("potionToken"),

                Extensions.CreateBasicKeyword(this, "swordtoken", "Trusty Sword", "<Free Action>: Deal <2> damage to front enemey|Uses per battle: 2").AddToIcons("swordToken"),

                Extensions.CreateBasicKeyword(this, "lumintoken", "Lumin Juice", "<Free Action>: Increase all effects by <1> until end of turn|Uses per battle: 2").AddToIcons("luminToken"),

                Extensions.CreateBasicKeyword(this, "bowtoken", "Berrywood Bow", "<Free Action>: Gain <keyword=longshot> until end of turn|Unlimited uses!").AddToIcons("bowToken"),

                Extensions.CreateBasicKeyword(this, "fisttoken", "Fighter's Mark", "<End Turn>: Gain <keyword=smackback> until end of turn|Uses per battle: 1").AddToIcons("fistToken"),

                Extensions.CreateBasicKeyword(this, "decktoken", "Hidden Ace", "<Free Action>: Move (from anywhere!) to top of draw pile|Uses per battle: 1").AddToIcons("deckToken"),

                Extensions.CreateBasicKeyword(this, "prismtoken", "Prism Stone", "<End Turn>: Gain <keyword=mhcdc9.wildfrost.tokens.prism>|Uses per battle: 1").AddToIcons("prismToken"),

                Extensions.CreateBasicKeyword(this, "frosttoken", "Frost Spike", "<Free Action>: Deal <keyword=frost> instead of damage for next attack|Uses per battle: 2").AddToIcons("frostToken"),

                Extensions.CreateBasicKeyword(this, "spicetoken", "Pepper Flip", "<Free Action>:\nGain <2><keyword=spice> and convert all debuffs into <keyword=spice>|Uses per battle: 1\n(Snow immune!)").AddToIcons("spiceToken"),

                Extensions.CreateBasicKeyword(this, "teethtoken", "Teeth Reprisal", "<Free Action>:\nGain <keyword=teeth> equal to missing <keyword=health>\n(max: <4>)|Uses per battle: 1").AddToIcons("teethToken"),

                Extensions.CreateBasicKeyword(this, "junktoken", "Dismantle Kit", "<Free Action>: Replace the rightmost card in hand with 2 <card=Junk>|Uses per battle: 1").AddToIcons("junkToken"),

                Extensions.CreateBasicKeyword(this, "prism", "Prism", "When a (valid) effect is applied, spread the effect to allies in row|Clears when activated")
                .WithCanStack(true),
                Extensions.CreateBasicKeyword(this, "froststrike", "Frost Strike", "Deals damage as <keyword=frost> instead|Counts down after each attack")
                .WithCanStack(true)
            };

            Extensions.CreateTokenIcon("potionToken", ImagePath("potionToken.png").ToSprite(), "potionToken", "snow", Color.white);
            Extensions.CreateTokenIcon("swordToken", ImagePath("swordToken.png").ToSprite(), "swordToken", "snow", Color.white);
            Extensions.CreateTokenIcon("luminToken", ImagePath("luminToken.png").ToSprite(), "luminToken", "snow", Color.white);
            Extensions.CreateTokenIcon("bowToken", ImagePath("bowToken.png").ToSprite(), "bowToken", "", Color.white);
            Extensions.CreateTokenIcon("fistToken", ImagePath("fistToken.png").ToSprite(), "fistToken", "", Color.white);
            Extensions.CreateTokenIcon("deckToken", ImagePath("deckToken.png").ToSprite(), "deckToken", "", Color.white);
            Extensions.CreateTokenIcon("prismToken", ImagePath("prismToken.png").ToSprite(), "prismToken", "", Color.white);
            Extensions.CreateTokenIcon("frostToken", ImagePath("frostToken.png").ToSprite(), "frostToken", "snow", Color.white);
            Extensions.CreateTokenIcon("spiceToken", ImagePath("spiceToken.png").ToSprite(), "spiceToken", "", Color.black);
            Extensions.CreateTokenIcon("teethToken", ImagePath("teethToken.png").ToSprite(), "teethToken", "", Color.black);
            Extensions.CreateTokenIcon("junkToken", ImagePath("junkToken.png").ToSprite(), "junkToken", "", Color.white);
            Extensions.CreateTokenIcon("mysteryToken", ImagePath("mysteryToken.png").ToSprite(), "mysteryToken", "", Color.white);

            effects = new List<StatusEffectDataBuilder>()
            {
                new StatusEffectDataBuilder(this)
                .CreateStatusToken <StatusTokenApplyX>("Potion Token", "potionToken")
                .FreeModify<StatusTokenApplyX>(
                    (data) =>
                    {
                        data.validPlaces = Extensions.CardPlaces.BoardAndHand;
                        data.doPing = false;
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                        data.targetConstraints = new TargetConstraint[0];
                        data.effectToApply = Get<StatusEffectData>("Heal");
                        data.endTurn = true;
                    }),

                new StatusEffectDataBuilder(this)
                .CreateStatusToken <StatusTokenApplyX>("Sword Token", "swordToken")
                .FreeModify<StatusTokenApplyX>(
                    (data) =>
                    {
                        data.validPlaces = Extensions.CardPlaces.Board;
                        data.hitDamage = 2;
                        data.doPing = false;
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.FrontEnemy;
                        data.targetConstraints = new TargetConstraint[0];
                        data.effectToApply = null;
                        data.applyEqualAmount = true;
                        data.endTurn = false;
                    }),

                new StatusEffectDataBuilder(this)
                .CreateStatusToken<StatusTokenApplyX>("Lumin Token", "luminToken")
                .FreeModify<StatusTokenApplyX>(
                    (data) =>
                    {
                        data.validPlaces = Extensions.CardPlaces.BoardAndHand;
                        data.fixedAmount = 1;
                        data.doPing = false;
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
                .CreateStatusToken<StatusTokenApplyX>("Bow Token", "bowToken")
                .FreeModify<StatusTokenApplyX>(
                    (data) =>
                    {
                        data.validPlaces = Extensions.CardPlaces.BoardAndHand;
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
                .CreateStatusToken<StatusTokenApplyX>("Fist Token", "fistToken")
                .FreeModify<StatusTokenApplyX>(
                    (data) =>
                    {
                        data.validPlaces = Extensions.CardPlaces.Board;
                        data.fixedAmount = 1;
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
                .WithType("mysteryToken")
                .WithIconGroupName("counter")
                .WithIsStatus(true)
                .WithVisible(true)
                .WithText("When destroyed, gain a <keyword=mhcdc9.wildfrost.tokens.token>"),

                new StatusEffectDataBuilder(this)
                .CreateStatusToken<StatusTokenMoveContainer>("Deck Token", "deckToken")
                .FreeModify<StatusTokenMoveContainer>(
                    (data) =>
                    {
                        data.validPlaces = Extensions.CardPlaces.Hand | Extensions.CardPlaces.Discard | Extensions.CardPlaces.Draw | Extensions.CardPlaces.Board;
                        data.finiteUses = true;
                        data.targetConstraints = new TargetConstraint[0];
                        data.toContainer = StatusTokenMoveContainer.Container.DrawPile;
                        data.top = true;
                    }),

                new StatusEffectDataBuilder(this)
                .CreateStatusToken<StatusTokenApplyX>("Prism Token", "prismToken")
                .FreeModify<StatusTokenApplyX>(
                    (data) =>
                    {
                        data.validPlaces = Extensions.CardPlaces.BoardAndHand;
                        data.endTurn = true;
                        data.fixedAmount = 1;
                        data.applyEqualAmount = true;
                        data.targetConstraints = new TargetConstraint[0];
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    })
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        StatusTokenApplyX data2 = (StatusTokenApplyX)data;
                        data2.effectToApply = Get<StatusEffectData>("Prism");
                    }),

                new StatusEffectDataBuilder(this)
                .Create<StatusEffectPrism>("Prism")
                .WithCanBeBoosted(false)
                .WithIsStatus(false)
                .WithStackable(true)
                .WithType("prism")
                .WithText("<keyword=mhcdc9.wildfrost.tokens.prism>")
                .FreeModify<StatusEffectPrism>(
                    (data) =>
                    {
                        data.applyEqualAmount = true;
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.AlliesInRow;
                        data.targetConstraints = new TargetConstraint[0];
                    }),

                new StatusEffectDataBuilder(this)
                .CreateStatusToken<StatusTokenApplyX>("Frost Token", "frostToken")
                .FreeModify<StatusTokenApplyX>(
                    (data) =>
                    {
                        data.validPlaces = Extensions.CardPlaces.BoardAndHand;
                        data.endTurn = false;
                        data.fixedAmount = 1;
                        data.applyEqualAmount = true;
                        data.targetConstraints = new TargetConstraint[0];
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    })
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        StatusTokenApplyX data2 = (StatusTokenApplyX)data;
                        data2.effectToApply = Get<StatusEffectData>("Frost Strike");
                    }),

                new StatusEffectDataBuilder(this)
                .Create<StatusEffectConvertDamage>("Frost Strike")
                .WithCanBeBoosted(true)
                .WithIsStatus(false)
                .WithStackable(true)
                .WithType("")
                .WithText("<keyword=mhcdc9.wildfrost.tokens.froststrike> <{a}>")
                .FreeModify<StatusEffectConvertDamage>(
                    (data) =>
                    {
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Target; //Meaningless
                        data.effectToApply = Get<StatusEffectData>("Frost");
                        data.targetConstraints = new TargetConstraint[0];
                    }),

                new StatusEffectDataBuilder(this)
                .CreateStatusToken<StatusTokenApplyX>("Spice Token", "spiceToken")
                .FreeModify<StatusTokenApplyX>(
                    (data) =>
                    {
                        data.validPlaces = Extensions.CardPlaces.BoardAndHand;
                        data.snowOverride = true;
                        data.endTurn = false;
                        data.fixedAmount = 1;
                        data.applyEqualAmount = true;
                        data.targetConstraints = new TargetConstraint[0];
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    })
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        StatusTokenApplyX data2 = (StatusTokenApplyX)data;
                        data2.effectToApply = Get<StatusEffectData>("Convert Debuffs Into Spice");
                    }),

                new StatusEffectDataBuilder(this)
                .Create<StatusEffectInstantConvertDebuffs>("Convert Debuffs Into Spice")
                .WithCanBeBoosted(false)
                .WithIsStatus(false)
                .WithStackable(true)
                .WithType("")
                .FreeModify<StatusEffectInstantConvertDebuffs>(
                    (data) =>
                    {
                        data.effectToApply = Get<StatusEffectData>("Spice");
                        data.targetConstraints = new TargetConstraint[0];
                        data.initialStacks = 2;
                    }),

                new StatusEffectDataBuilder(this)
                .CreateStatusToken<StatusTokenApplyX>("Teeth Token", "teethToken")
                .FreeModify<StatusTokenApplyX>(
                    (data) =>
                    {
                        data.effectToApply = Get<StatusEffectData>("Teeth");
                        data.validPlaces = Extensions.CardPlaces.BoardAndHand;
                        data.endTurn = false;
                        data.scriptableAmount = ScriptableAmountMissingHealth.CreateInstance(0,4);
                        data.targetConstraints = new TargetConstraint[0];
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    }),

                new StatusEffectDataBuilder(this)
                .CreateStatusToken<StatusTokenApplyX>("Junk Token", "junkToken")
                .FreeModify<StatusTokenApplyX>(
                    (data) =>
                    {
                        data.validPlaces = Extensions.CardPlaces.BoardAndHand;
                        data.endTurn = false;
                        data.doPing = false;
                        data.fixedAmount = 2;
                        data.applyEqualAmount = true;
                        data.targetConstraints = new TargetConstraint[0];
                        data.applyToFlags = StatusEffectApplyX.ApplyToFlags.RightCardInHand;
                    })
                .SubscribeToAfterAllBuildEvent(
                    (data) =>
                    {
                        StatusTokenApplyX data2 = (StatusTokenApplyX)data;
                        data2.effectToApply = Get<StatusEffectData>("Instant Destroy And Summon Junk");
                    }),

                new StatusEffectDataBuilder(this)
                .Create<StatusEffectInstantMultiple>("Instant Destroy And Summon Junk")
                .FreeModify<StatusEffectInstantMultiple>(
                    (data) =>
                    {
                        data.effects = new StatusEffectInstant[2]
                        {
                            Get<StatusEffectData>("Instant Summon Junk In Hand") as StatusEffectInstant,
                            Get<StatusEffectData>("Sacrifice Card In Hand") as StatusEffectInstant,
                        };
                    }),



            };

            preLoaded = true;
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
            StatusTokenApplyX.DefineStrings();
            Events.OnCardDataCreated += Gobling;
            Events.OnSceneLoaded += SceneLoaded;
            Events.OnPreCampaignPopulate += ResetPool;
            DisableDrag();
        }

        public override void Unload()
        {
            base.Unload();
            TokenPrefab.Destroy();
            Holder.Destroy();
            Events.OnCardDataCreated -= Gobling;
            Events.OnSceneLoaded -= SceneLoaded;
            Events.OnPreCampaignPopulate -= ResetPool;
        }

        public void ResetPool()
        {
            StatusEffectGiveUpgradeOnDeath.data.Clear();
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
                        //Debug.Log("[Tokens] Test 1");
                        break;
                    }
                }
                //Debug.Log("[Tokens] Test 2");
                Holder = GameObject.Instantiate(HolderPrefab, borderRight);
                Holder.transform.SetSiblingIndex(0);
                Holder.transform.localPosition = new Vector3(1.35f, 1.3f, 0f);
                Holder.GetComponent<TokenHolder>().dragHandler = deckDisplay.GetComponentInChildren<CardCharmDragHandler>(true);
                Holder.SetActive(true);
                //Debug.Log("[Tokens] Test 3");
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
                //Debug.Log("[Tokens] Test 4");
                ButtonAnimator animator = takeTokenButton.GetComponentInChildren<ButtonAnimator>();
                animator.baseColour = new Color(0.96f, 0.875f, 0.589f, 1);
                Button button = takeTokenButton.GetComponentInChildren<Button>();
                button.image.sprite = this.ImagePath("takeToken.png").ToSprite();
                button.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
                button.onClick.AddListener(TakeToken);

                
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

        internal static IEnumerator RemoveToken(Entity entity)
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

    [HarmonyPatch(typeof(EventRoutineCopyItem), "CopyRoutine")]
    internal static class PatchNoDuplicateTokens
    {
        static IEnumerator Postfix(IEnumerator __result, EventRoutineCopyItem __instance)
        {
            CardUpgradeData upgrade = null;
            for(int i = __instance.copyEntity.data.upgrades.Count - 1; i>=0; i--)
            {
                if (__instance.copyEntity.data.upgrades[i].type == CardUpgradeData.Type.Token)
                {
                    upgrade = __instance.copyEntity.data.upgrades[i].Clone();
                    yield return TokenMain.RemoveToken(__instance.copyEntity);
                    break;
                }
            }
            yield return __result;
            if (upgrade != null)
            {
                upgrade.Assign(__instance.copyEntity.data);
            }
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

        public override void SetInputOverrides()
        {
            for (int i = 0; i < base.transform.childCount; i++)
            {
                UpgradeDisplay component = base.transform.GetChild(i).GetComponent<UpgradeDisplay>();
                component.navigationItem.overrideInputs = true;
                component.navigationItem.inputDown = ((i > 0) ? base.transform.GetChild(i - 1).GetComponent<UpgradeDisplay>().navigationItem : null);
                component.navigationItem.inputUp = ((i < list.Count - 1) ? base.transform.GetChild(i + 1).GetComponent<UpgradeDisplay>().navigationItem : null);
            }
        }
    }
}
