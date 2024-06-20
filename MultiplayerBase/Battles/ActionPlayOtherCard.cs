using MultiplayerBase.Handlers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Rewired.ComponentControls.Data.CustomControllerElementSelector;
using UnityEngine;

namespace MultiplayerBase.Battles
{
    internal class ActionPlayOtherCard : PlayAction
    {
        public override bool IsRoutine => true;
        private string[] messages;
        private Entity entity;
        private CardContainer container;

        public ActionPlayOtherCard(string[] messages, Entity entity, CardContainer container)
        {
            this.messages = messages;
            this.entity = entity;
            this.container = container;
        }

        public override IEnumerator Run()
        {
            Entity otherCard = CardEncoder.DecodeEntity1(null, References.Player, messages);
            otherCard.transform.SetParent(HandlerInspect.instance.transform, false);
            yield return CardEncoder.DecodeEntity2(otherCard, messages);
            otherCard.flipper.FlipUp(true);
            References.Player.handContainer.Add(otherCard);
            PlayAction action;
            if (entity == null && container == null)
            {
                action = new ActionTrigger(otherCard, References.Player.entity);
            }
            else
            {
                action = new ActionTriggerAgainst(otherCard, References.Player.entity, entity, container);
            }
            LeanTween.moveLocal(otherCard.gameObject, new Vector3(-6f,0,0), 1f).setEase(LeanTweenType.easeOutQuart);
            yield return new WaitForSeconds(1f);
            if (Events.CheckAction(action))
            {
                ActionQueue.Add(action);
            }
            ActionQueue.Add(new ActionKill(otherCard));
        }
    }
}
