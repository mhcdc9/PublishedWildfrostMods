using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerBase.Battles
{
    public class ActionGainCardToHand : ActionDisplayCardAndSequence
    {
        public enum Location
        {
            None,
            PlayerBoard,
            EnemyBoard,
            Hand,
            Draw,
            Discard,
            Custom
        }

        public Location location = Location.Hand;

        public ActionGainCardToHand(CardData cardData, Location location = Location.Hand, float beforeDelay = 0f, float afterDelay = 0f)
            : base(cardData, null, null, null, beforeDelay, afterDelay)
        {
            this.location = location;
            note = cardData.title;
        } 

        public ActionGainCardToHand(string[] messages, Location location = Location.Hand, float beforeDelay = 0f, float afterDelay = 0f)
            : base(null, messages, null, null, beforeDelay, afterDelay) 
        {
            this.location = location;
            note = messages[0];
        }

        public override IEnumerator RunSequence()
        {
            References.Battle.cards.Add(displayedEntity);
            displayedEntity.display.hover.controller = Battle.instance.playerCardController;
            displayedEntity.curveAnimator.Ping();
            displayedEntity.inPlay = true;
            yield return Sequences.Wait(0.4f);
            //displayedEntity.RemoveFromContainers();
            CardContainer container = FindContainer();
            if (container == null)
            {
                yield return base.Disappear();
            }
            else
            {
                yield return Sequences.CardMove(displayedEntity, new CardContainer[] { container });
                if (location == Location.PlayerBoard || location == Location.EnemyBoard || location == Location.Hand)
                {
                    ActionQueue.Stack(new ActionRunEnableEvent(displayedEntity));
                }
            }
            
        }

        public CardContainer FindContainer()
        {
            switch (location)
            {
                case Location.PlayerBoard:
                    return FindSpotOnBoard(Battle.instance.rows[References.Player]);
                case Location.EnemyBoard:
                    return FindSpotOnBoard(Battle.instance.rows[Battle.GetOpponent(References.Player)]);
                case Location.Hand:
                    return References.Player.handContainer;
                case Location.Draw:
                    return References.Player.drawContainer;
                case Location.Discard:
                    return References.Player.discardContainer;
            }
            return null;
        }

        public virtual CardContainer FindSpotOnBoard(List<CardContainer> lanes)
        {
            foreach (CardContainer lane in lanes)
            {
                if (lane is CardSlotLane slotLane)
                {
                    foreach (CardSlot slot in  slotLane.slots)
                    {
                        if (slot.Count == 0)
                        {
                            return slot;
                        }
                    }
                }
            }
            return References.Player.handContainer;
        }

        public override IEnumerator Disappear()
        {
            yield break;
        }
    }
}
