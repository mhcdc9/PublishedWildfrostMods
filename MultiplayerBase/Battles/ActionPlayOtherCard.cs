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
    internal class ActionPlayOtherCard : PlayAction
    {
        public override bool IsRoutine => true;
        private readonly string[] messages;
        private readonly Friend friend;
        private Entity entity;
        private CardContainer container;

        private Vector3 startTextPosition = new Vector3(0f, 2.6f, 0f);

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
            yield return CardEncoder.DecodeEntity2(otherCard, messages);
            foreach (StatusEffectData effect in entity.statusEffects)
            {
                if (effect is StatusEffectFreeAction f)
                {
                    f.hasEffect = false;
                }
            }
            yield return otherCard.UpdateTraits();
            otherCard.display.promptUpdateDescription = true;
            otherCard.PromptUpdate();
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
            //HandlerSystem.CHT_Handler(friend, otherCard.transform.position.ToString());
            LeanTween.moveLocal(otherCard.gameObject, new Vector3(-6f,0,0), 0.5f).setEase(LeanTweenType.easeOutQuart);
            yield return new WaitForSeconds(0.5f);
            if (Events.CheckAction(action))
            {
                ActionQueue.Add(action);
            }
            HandlerBattle.InvokeOnPostPlayOtherCard(friend, entity);
            ActionQueue.Add(new ActionKill(otherCard));
            //DisplayOwner(otherCard);
        }

        private void DisplayOwner(Entity otherCard)
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
