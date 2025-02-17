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

        public override string Description => "Every card played/placed from your deck transforms on battle end. Can you adapt to an ever-changing deck? Charms stay on if possible. Otherwise, they transform to a new charm of the same tier. ";

        public List<StatusEffectDataBuilder> effects = new List<StatusEffectDataBuilder>();
        public bool preloaded = false;

        public override void Load()
        {
            base.Load();
            Events.OnActionFinished += ActionFinished;
            Events.OnBattleStart += Reset;
            Events.OnBattleEnd += SwapCards;
        }

        public override void Unload()
        {
            base.Unload();
            Events.OnActionFinished -= ActionFinished;
            Events.OnBattleStart -= Reset;
            Events.OnBattleEnd -= SwapCards;
        }

        private void ActionFinished(PlayAction action)
        {
            if (Battle.instance == null)
            {
                return;
            }
            if (action is ActionMove move)
            {
                Entity entity = move.entity;
                if (entity.owner == References.Player && Battle.IsOnBoard(move.toContainers) && !cardsToReplace.Contains(entity.data))
                {
                    cardsToReplace.Add(entity.data);
                }
            }
            if (action is ActionTrigger trigger)
            {
                Entity entity = trigger.entity;
                if (entity.owner == References.Player && !cardsToReplace.Contains(entity.data))
                {
                    cardsToReplace.Add(entity.data);
                }
            }
        }

        private void Reset()
        {
            cardsToReplace.Clear();
        }

        private void SwapCards()
        {
            CharacterRewards rewards = References.Player.GetComponent<CharacterRewards>();
            PopulateIfNecessary(rewards);
            foreach(CardData card in cardsToReplace)
            {
                string type = "";
                CardData newCard;
                switch (card.cardType.name)
                {
                    case "Friendly":
                        type = "Units";
                        break;
                    case "Item":
                        if (card.name == "LuminVase")
                        {
                            continue;
                        }
                        type = "Items";
                        break;
                    case "Clunker":
                        type = "Items";
                        break;

                }
                if (type == "")
                {
                    continue;
                }
                newCard = rewards.Pull<CardData>(null, type, 1, true, match: c => ((CardData)c).playType != Card.PlayType.None).First().Clone();
                if (card.upgrades.Count > 0)
                {
                    foreach (CardUpgradeData upgrade in card.upgrades)
                    {
                        if (upgrade.CanAssign(newCard))
                        {
                            upgrade.Clone().Assign(newCard);
                        }
                        else if (upgrade.type == CardUpgradeData.Type.Charm)
                        {
                            CardUpgradeData newCharm = rewards.Pull<CardUpgradeData>(null, "Charms", 1, true, match: c => ((CardUpgradeData)c).tier == upgrade.tier).First();
                            References.PlayerData.inventory.upgrades.Add(newCharm.Clone());
                        }
                        else
                        {
                            References.PlayerData.inventory.upgrades.Add(upgrade);
                        }
                    }
                }
                References.PlayerData.inventory.deck.Remove(card);
                References.PlayerData.inventory.deck.Add(newCard);
            }
            cardsToReplace.Clear();
        }

        private void PopulateIfNecessary(CharacterRewards rewards)
        {
            if (rewards.poolLookup.ContainsKey("Units") && rewards.poolLookup.ContainsKey("Items") && rewards.poolLookup.ContainsKey("Charms"))
            {
                return;
            }
            rewards.Populate(References.PlayerData.classData);
        }

    }

}
