using MultiplayerBase.Handlers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Rewired.ComponentControls.Data.CustomControllerElementSelector;
using UnityEngine;
using Steamworks;

namespace MultiplayerBase.Battles
{
    internal class ActionPlayOtherCard : PlayAction
    {
        public override bool IsRoutine => true;
        private string[] messages;
        private Friend friend;
        private Entity entity;
        private CardContainer container;

        public ActionPlayOtherCard(string[] messages, Friend friend, Entity entity, CardContainer container)
        {
            this.messages = messages;
            this.entity = entity;
            this.container = container;
            this.friend = friend;
        }

        public override IEnumerator Run()
        {
            Entity otherCard = CardEncoder.DecodeEntity1(null, References.Player, messages);
            otherCard.transform.SetParent(HandlerInspect.instance.transform, false);
            foreach(StatusEffectData effect in entity.statusEffects)
            {
                if (effect is StatusEffectFreeAction f)
                {
                    f.hasEffect = false;
                }
            }
            yield return otherCard.UpdateTraits();
            otherCard.display.promptUpdateDescription = true;
            otherCard.PromptUpdate();
            yield return CardEncoder.DecodeEntity2(otherCard, messages);
            otherCard.flipper.FlipUp(true);
            References.Player.handContainer.Add(otherCard);
            HandlerBattle.InvokeOnPlayOtherCard(friend, entity);
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
            HandlerBattle.InvokeOnPostPlayOtherCard(friend, entity);
            ActionQueue.Add(new ActionKill(otherCard));
        }
    }
}
