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
using TMPro;

namespace MultiplayerBase.Battles
{
    public class ActionPlayOtherCard : ActionDisplayCardAndSequence
    {
        protected Friend friend;
        protected Entity entity;
        private CardContainer container;
        private Vector3 startTextPosition = new Vector3(0f, 2.6f, 0f);

        public ActionPlayOtherCard(string[] messages, Friend friend, Entity entity, CardContainer container)
        {
            this.messages = messages;
            this.entity = entity;
            this.container = container;
            this.friend = friend;
            note = friend.Name ?? "???";
        }

        /*
         * ButtonAnimator (disabled color = 1,0.78,0.35,1), type=normal
         * hover: scale -> 1.15,1.15,1 - elastic
         * unhover: scale -> 1,1,1 - back
         * press: scale -> 0.95, 0.95, 1 - elastic
         * release: scale -> 1,1,1 - back
         */

        public override IEnumerator RunSequence()
        {
            displayedEntity.enabled = true;
            PlayAction action;
            if (entity == null && container == null)
            {
                action = new ActionTrigger(displayedEntity, References.Player.entity);
            }
            else
            {
                action = new ActionTriggerAgainst(displayedEntity, References.Player.entity, entity, container);
            }
            //HandlerSystem.CHT_Handler(friend, otherCard.transform.position.ToString());

            if (Events.CheckAction(action))
            {
                ActionQueue.Stack(action);
            }
            MultEvents.InvokeOtherCardPlayed(friend, entity);
            yield break;
        }

        /*
        public override IEnumerator Run()
        {
            Entity otherCard = CardEncoder.DecodeEntity1(null, References.Player, messages);
            yield return PrepareCard();
            
            
            PlayAction action;
            if (entity == null && container == null)
            {
                action = new ActionTrigger(otherCard, References.Player.entity);
            }
            else
            {
                action = new ActionTriggerAgainst(otherCard, References.Player.entity, entity, container);
            }
            //HandlerSystem.CHT_Handler(friend, otherCard.transform.position.ToString());
            
            if (Events.CheckAction(action))
            {
                ActionQueue.Add(action);
            }
            
            ActionQueue.Add(new ActionKill(otherCard));
            //DisplayOwner(otherCard);
        }
        */

        public void DisplayOwner(Entity otherCard)
        {
            Debug.Log($"[Multiplayer] {otherCard != null}, {otherCard?.canvas != null}, {friend}");
            GameObject obj = new GameObject("Owner Text");
            obj.transform.SetParent(otherCard.canvas.transform, false);
            Debug.Log($"[Multiplayer] Got past this part, at least");
            obj.transform.localPosition = startTextPosition;
            TextMeshProUGUI textElement = obj.AddComponent<TextMeshProUGUI>();
            textElement.fontSize = 0.4f;
            textElement.horizontalAlignment = HorizontalAlignmentOptions.Center;
            textElement.text = friend.Name;
            textElement.outlineColor = Color.black;
            textElement.outlineWidth = 0.06f;
            Debug.Log($"[Multiplayer] Text Element Stuff");
            obj.GetComponent<RectTransform>().sizeDelta = new Vector2(4f, 1f);
        }
    }
}
