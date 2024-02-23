using Deadpan.Enums.Engine.Components.Modding;
using GameAnalyticsSDK;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Mono.Security.X509.X520;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization;
using Newtonsoft.Json.Linq;
using Extensions = Deadpan.Enums.Engine.Components.Modding.Extensions;

namespace AddedCards
{
    public class Tyrant : WildfrostMod
    {
        private List<CardDataBuilder> cards;
        private StatusEffectApplyX givewild;
        public StatusEffectWild wilder;
        public Tyrant(string modDirectory) : base(modDirectory)
        {
            StatusEffectWild wilder = ScriptableObject.CreateInstance<StatusEffectWild>();
            wilder.trait = this.Get<TraitData>("Wild");
            wilder.silenced = null;
            wilder.added = null;
            wilder.addedAmount = 0;
            UnityEngine.Debug.Log("[Michael] Wonder if it worked?");
            wilder.targetConstraints = new TargetConstraint[0];
            wilder.offensive = true;
            wilder.isKeyword = false;
            StringTable collection = LocalizationHelper.GetCollection("Card Text", SystemLanguage.English);
            collection.SetString(wilder.name + "_text", "Am I Wild?");
            wilder.textKey = collection.GetString(wilder.name + "_text");
            wilder.ModAdded = this;
            wilder.textInsert = "Wild Party!";
            wilder.name = "Apply Wild Trait";
            wilder.applyFormat = "";
            wilder.type = "";
            //wilder.textKey = this.Get<StatusEffectData>("Instant Gain Aimless").textKey;
            wilder.applyFormatKey = new LocalizedString();
            AddressableLoader.AddToGroup<StatusEffectData>("StatusEffectData", wilder);

            cards = new List<CardDataBuilder>()
            {
                new CardDataBuilder(this).CreateUnit("tyrantrum", "Tyrantrum")
                .SetSprites("tyrantrum.png", "tyrantrum BG.png")
                .SetStats(6, 4, 5)
                .SetAttackEffect(new CardData.StatusEffectStacks(wilder, 1 ))
                .SetStartWithEffect(new CardData.StatusEffectStacks(Get<StatusEffectData>("MultiHit"), 1 ))
                .SetTraits(new CardData.TraitStacks(Get<TraitData>("Aimless"), 1), new CardData.TraitStacks(Get<TraitData>("Wild"), 1))
            };

            //
        }

        /*
         * new CardDataBuilder(this).Create("tyrantrum").WithTitle("Tyrantrum").AsUnit()
                .CanPlayOnEnemy()
                .CanPlayOnBoard()
                .WithTargetMode("TargetModeBasic")
                .WithBloodProfile("BloodProfileNormal")
                .WithIdleAnimationProfile("SwayAnimationProfile")
                .WithCardType("Friendly")
                .SetStats(6, 4, 5)
                .SetSprites("tyrantrum.png", "tyrantrum BG.png")
                .SetStartWithEffect(new CardData.StatusEffectStacks(Get<StatusEffectData>("Gain Frenzy When Wild Unit Killed"), 1))
                )
        */

        protected override void Load()
        {
            base.Load();
            Events.OnCardDataCreated += Wildparty;
            Events.OnBattlePhaseStart += ScrapPileToHand;
            //Events.OnCampaignStart += AddTyrantrum;
            UnityEngine.Debug.Log("[Michael] Mod Almost Loaded.");
            for (int i = 0; i < References.Classes.Length; i++)
            {
                //References.Classes[i].startingInventory.deck.Add(Get<CardData>("tyrantrum"));
                for (int j = 0; j < References.Classes[i].startingInventory.deck.Count; j++)
                {
                    UnityEngine.Debug.Log(References.Classes[i].startingInventory.deck[j].name);
                }
            }
            UnityEngine.Debug.Log("[Michael] Mod Loaded!");
            UnityEngine.Debug.Log("[MICHAEL]" + " " + Extensions.PrefixGUID("EyeDrops", this));
            CoroutineManager.Start(SceneManager.Load("Console", SceneType.Persistent));
        }

        private void ScrapPileToHand(Battle.Phase arg0)
        {
            if (References.Player.handContainer.Count == 0)
                return;
            CardData cardData = Get<CardData>("Sword").Clone();
            Card card = CardManager.Get(cardData, References.Player.handContainer[0].display.hover.controller, References.Player, true, true);
            Debug.Log("Got card hopefully");
            References.Player.handContainer.Add(card.entity);
            Debug.Log("Got card in hand");
        }
        

        private void Wildparty(CardData cardData)
        {
            //UnityEngine.Debug.Log("[Michael] Initializing CardData.");
            //CardData cardData = entity.gameObject.GetComponent<CardData>();
            //UnityEngine.Debug.Log("[Michael] That worked.");
            /*if (cardData != null && cardData.cardType.name == "Leader")
            {
                StatusEffectData wilder = Get<StatusEffectData>("Apply Wild Trait");
                UnityEngine.Debug.Log("[Michael] Adding to " + cardData.title + ", " + wilder.name);
                if (cardData.attackEffects == null)
                {
                    UnityEngine.Debug.Log("[Michael] Add attack array.");
                }
                cardData.attackEffects = cardData.attackEffects.AddItem(new CardData.StatusEffectStacks(wilder, 1)).ToArray();
                UnityEngine.Debug.Log("[Michael] Success? " + wilder.name);
            }*/
            if (cardData.name == "Sword")
            {
                cardData.traits.Add(new CardData.TraitStacks(Get<TraitData>("Wild"),1));
                cardData.traits.Add(new CardData.TraitStacks(Get<TraitData>("Consume"), 1));
            }
        }

        protected override void Unload()
        {
            base.Unload();
            Events.OnCardDataCreated -= Wildparty;
            Events.OnBattlePhaseStart -= ScrapPileToHand;
            //Events.OnEntityChosen -= ModifyName;
            UnityEngine.Debug.Log("Mod Unloaded. -Michael.");
        }

        public override List<T> AddAssets<T, Y>()
        {
            var typeName = typeof(Y).Name;
            UnityEngine.Debug.Log("[Michael] " + typeName + " " + typeof(T).Name);
            if (typeName == nameof(CardData))
            {
                return cards.Cast<T>().ToList();
            }
            /*switch (typeName)
            {
                case "CardData": //return AddCards().Cast<T>().ToList();
                    UnityEngine.Debug.Log("[Michael] Copy Hog.");
                    CardData cardData = Get<CardData>("Wildling").Clone();
                    cardData.hp = 6;
                    cardData.damage = 4;
                    cardData.counter = 5;
                    UnityEngine.Debug.Log("[Michael] Set Stats.");
                    cardData.forceTitle = "tyrantrum";
                    cardData.cardType = Get<CardData>("Bear").cardType;
                    cardData.targetMode = Get<CardData>("DemonPet").targetMode;
                    UnityEngine.Debug.Log("[Michael] Mimic Loki.");
                    cardData.AddPool(Extensions.GetRewardPool("GeneralUnitPool"));
                    cardData.ModAdded = this;
                    AddressableLoader.AddToGroup<CardData>(typeName, cardData);
                    break;
            }*/
            return null;
        }

        public override string GUID => "mhcdc9.wildfrost.tyrantrum";
        public override string[] Depends => new string[] { };
        public override string Title => "Tyrantrum Soars";
        public override string Description => "For the wild!";
    }
}
