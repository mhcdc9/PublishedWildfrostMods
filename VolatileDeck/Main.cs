using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VolatileDeck
{
    public class VolatileDeck : WildfrostMod
    {
        public static List<CardData> cardsPlayed = new List<CardData>();

        public static List<CardData> cardsToReplace = new List<CardData>();

        public VolatileDeck(string modDirectory) : base(modDirectory)
        {
        }

        public override string GUID => "mhcdc9.wildfrost.volatiledeck";

        public override string[] Depends => new string[0];

        public override string Title => "Volatile Deck";

        public override string Description => "Every card played from your deck changes on battle end. Can you adapt to an ever-changing deck?";

        public List<StatusEffectDataBuilder> effects = new List<StatusEffectDataBuilder>();
        public bool preloaded = false;

        private void CreateModAssets()
        {
            effects.Add(
                new StatusEffectDataBuilder(this)
                .Create<StatusEffectCheckCardsPlayed>("Check Cards Played")
                .WithCanBeBoosted(false)
                .WithStackable(false)
                .WithVisible(false)
                .WithType("")
                );

            preloaded = true;
        }

        protected override void Load()
        {
            if (!preloaded) { CreateModAssets(); };
            base.Load();
            Events.OnCardDataCreated += CardDataCreated;
            Events.OnBattleEnd += SwapCards;
        }

        protected override void Unload()
        {
            base.Unload();
            Events.OnCardDataCreated -= CardDataCreated;
            Events.OnBattleEnd -= SwapCards;
        }

        public override List<T> AddAssets<T, Y>()
        {
            var typeName = typeof(Y).Name;
            switch (typeName)
            {
                case nameof(StatusEffectData):
                    return effects.Cast<T>().ToList();
                default:
                    return null;
            }
        }

        private void CardDataCreated(CardData card)
        {
            if (card.cardType.name == "Leader")
            {
                card.startWithEffects = CardData.StatusEffectStacks.Stack(card.startWithEffects, 
                    new CardData.StatusEffectStacks[1] { new CardData.StatusEffectStacks(Get<StatusEffectData>("Check Cards Played"), 1) }
                    );
            }
        }

        private void SwapCards()
        {
            References.Player.GetComponent<CharacterRewards>().Populate(References.PlayerData.classData);
            foreach(CardData card in cardsToReplace)
            {
                string type = "";
                switch (card.cardType.name)
                {
                    case "Friendly":
                        type = "Units";
                        break;
                    case "Item":
                    case "Clunker":
                        type = "Items";
                        break;

                }
                if (type == "")
                {
                    continue;
                }
                CardData[] newCards = References.Player.GetComponent<CharacterRewards>().Pull<CardData>(null, type, 1);
                if (newCards.Length != 0)
                {
                    References.PlayerData.inventory.deck.Remove(card);
                    References.PlayerData.inventory.deck.Add(newCards[0].Clone());
                    foreach(CardUpgradeData upgradeData in card.upgrades)
                    {
                        References.PlayerData.inventory.upgrades.Add(Get<CardUpgradeData>(upgradeData.name).Clone());
                    }
                }
            }
            cardsPlayed.Clear();
            cardsToReplace.Clear();
        }

    }

    public class StatusEffectCheckCardsPlayed : StatusEffectData
    {
        public override bool RunCardPlayedEvent(Entity entity, Entity[] targets)
        {
            CardData entityCard = entity.data;
            if (!VolatileDeck.cardsPlayed.Contains(entityCard))
            {
                foreach(CardData card in References.PlayerData.inventory.deck)
                {
                    if (card.id == entityCard.id)
                    {
                        VolatileDeck.cardsPlayed.Add(entityCard);
                        VolatileDeck.cardsToReplace.Add(card);
                        return false;
                    }
                }
            }
            return false;
        }
    }
}
