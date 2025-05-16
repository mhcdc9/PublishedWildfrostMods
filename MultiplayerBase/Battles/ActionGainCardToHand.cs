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
        public ActionGainCardToHand(CardData cardData, float beforeDelay = 0f, float afterDelay = 0f)
            : base(cardData, null, null, null, beforeDelay, afterDelay)
        {
            note = cardData.title;
        } 

        public ActionGainCardToHand(string[] messages, float beforeDelay = 0f, float afterDelay = 0f)
            : base(null, messages, null, null, beforeDelay, afterDelay) 
        {
            note = messages[0];
        }

        public override IEnumerator RunSequence()
        {
            References.Battle.cards.Add(displayedEntity);
            displayedEntity.display.hover.controller = Battle.instance.playerCardController;
            displayedEntity.curveAnimator.Ping();
            displayedEntity.inPlay = true;
            yield return Sequences.Wait(0.4f);
            displayedEntity.RemoveFromContainers();
            yield return Sequences.CardMove(displayedEntity, new CardContainer[] { References.Player.handContainer });
            ActionQueue.Stack(new ActionRunEnableEvent(displayedEntity));
        }

        public override IEnumerator Disappear()
        {
            yield break;
        }
    }
}
