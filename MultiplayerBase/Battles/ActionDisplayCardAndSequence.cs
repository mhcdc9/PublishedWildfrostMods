using MultiplayerBase.Handlers;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerBase.Battles
{
    public class ActionDisplayCardAndSequence : PlayAction
    {
        public override bool IsRoutine => true;

        //displayedCardData and messages is mutually exclusive
        protected CardData displayedCardData;
        protected string[] messages;
        protected Entity displayedEntity;

        //displayedCardData and messages is mutually exclusive
        protected Func<Entity, IEnumerator> sequence;
        protected PlayAction playAction;

        public bool includeFrenzy = true;

        public float beforeDelay = 0f;
        public float afterDelay = 0f;

        protected ActionDisplayCardAndSequence() { }

        protected ActionDisplayCardAndSequence(CardData displayedCardData, string[] messages, Func<Entity, IEnumerator> sequence, PlayAction playAction, float beforeDelay = 0f, float afterDelay = 0f)
        {
            this.displayedCardData = displayedCardData;
            this.messages = messages;
            this.sequence = sequence;
            this.playAction = playAction;
            this.beforeDelay = beforeDelay;
            this.afterDelay = afterDelay;
        }

        public ActionDisplayCardAndSequence(CardData displayedCardData, Func<Entity, IEnumerator> sequence, float beforeDelay = 0f, float afterDelay = 0f)
            : this(displayedCardData, null, sequence, null, beforeDelay, afterDelay) { }

        public ActionDisplayCardAndSequence(string[] messages, Func<Entity, IEnumerator> sequence, float beforeDelay = 0f, float afterDelay = 0f)
            : this(null, messages, sequence, null, beforeDelay, afterDelay) { }

        public ActionDisplayCardAndSequence(CardData displayedCardData, PlayAction playAction, float beforeDelay = 0f, float afterDelay = 0f)
            : this(displayedCardData, null, null, playAction, beforeDelay, afterDelay) { }

        public ActionDisplayCardAndSequence(string[] messages, PlayAction playAction, float beforeDelay = 0f, float afterDelay = 0f)
            : this(null, messages, null, playAction, beforeDelay, afterDelay) { }

        public override IEnumerator Run()
        {
            ActionQueue.Stack(new ActionSequence(Disappear())
            {
                note = "End Card Display"
            }); //Runs after all others

            yield return PrepareCard();
            yield return Sequences.Wait(beforeDelay);
            yield return MoveToPosition();
            yield return Sequences.Wait(afterDelay);
            yield return RunSequence();
        }

        public virtual IEnumerator PrepareCard()
        {
            if (messages != null)
            {
                displayedEntity = CardEncoder.DecodeEntity1(Battle.instance.playerCardController, References.Player, messages);
                displayedEntity.transform.SetParent(HandlerInspect.instance.transform, false);
                yield return CardEncoder.DecodeEntity2(displayedEntity, messages);
                displayedEntity.silenceCount -= 100;
                foreach (StatusEffectData effect in displayedEntity.statusEffects)
                {
                    if (effect is StatusEffectFreeAction f)
                    {
                        f.hasEffect = false;
                    }
                }
                Debug.Log($"[Multiplayer] Before UpdateTraits - {displayedEntity.enabled}");
                yield return displayedEntity.UpdateTraits();
            }
            else
            {
                Card card = CardManager.Get(displayedCardData, null, References.Player, inPlay: false, isPlayerCard: true);
                displayedEntity = card.entity;
                yield return card.UpdateData();
            }
            displayedEntity.display.promptUpdateDescription = true;
            displayedEntity.PromptUpdate();
            displayedEntity.flipper.FlipUp(true);
            Debug.Log($"[Multiplayer] After flip up - {displayedEntity.enabled}");
        }

        public virtual IEnumerator MoveToPosition()
        {
            References.Player.handContainer.Add(displayedEntity);
            LeanTween.moveLocal(displayedEntity.gameObject, new Vector3(-6f, 0, 0), 0.5f).setEase(LeanTweenType.easeOutQuart);
            Debug.Log($"[Multiplayer] Before Waiting time - {displayedEntity.enabled}");
            yield return new WaitForSeconds(0.5f);
        }

        public virtual IEnumerator RunSequence()
        {
            int count = (includeFrenzy ? FindNumberOfTriggers() : 1);
            if (sequence != null)
            {
               for(int i=0; i<count; i++)
               {
                    yield return sequence(displayedEntity);
               }
            }
            else
            {
                for (int i = 0; i < FindNumberOfTriggers(); i++)
                {
                    yield return playAction.Run();
                }
            }
        }

        public int FindNumberOfTriggers()
        {
            StatusEffectData multiHit = displayedEntity.statusEffects.FirstOrDefault(s => s.name == "MultiHit");
            return (1 + (multiHit == null ? 0 : multiHit.GetAmount()));
        }

        public virtual IEnumerator Disappear()
        {
            displayedEntity.RemoveFromContainers();
            displayedEntity.transform.SetParent(HandlerBattle.instance.transform, true);
            displayedEntity.gameObject.AddComponent<CardDestroyedConsume>();
            yield return Sequences.Wait(0.3f);
            /*
            LeanTween.move(displayedEntity.gameObject, new Vector3(0, -12f, 0), 0.5f).setEase(LeanTweenType.easeOutQuart);
            yield return Sequences.Wait(0.5f);
            displayedEntity.RemoveFromContainers();
            */
        }
    }
}
