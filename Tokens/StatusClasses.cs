using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static Building;

namespace Tokens
{
    public interface IStatusToken
    {
        void ButtonCreate(StatusIconExt icon);

        void RunButtonClicked();

        IEnumerator ButtonClicked();


    }

    public class StatusIconExt : StatusIcon
    {
        public ButtonAnimator animator;
        public ButtonExt button;
        private IStatusToken effectToken;

        

        public override void Assign(Entity entity)
        {
            base.Assign(entity);
            SetText();
            Ping();
            onValueDown.AddListener(delegate { Ping(); });
            onValueUp.AddListener(delegate { Ping(); });
            afterUpdate.AddListener(SetText);
            onValueDown.AddListener(CheckDestroy);
            onDestroy.AddListener(DisableDragBlocker);

            StatusEffectData effect = entity.FindStatus(type);
            if (effect is IStatusToken effect2)
            {
                effectToken = effect2;
                effect2.ButtonCreate(this);
                button.onClick.AddListener(effectToken.RunButtonClicked);
            }
        }

        public void DisableDragBlocker()
        {
            button.DisableDragBlocking();
        }
    }

    public class StatusTokenApplyX : StatusEffectApplyX, IStatusToken
    {
        //Standard Code I wish I can put into IStatusToken
        public bool fromBoard = true;
        public bool fromHand = false;
        public bool fromDraw = false;
        public bool fromDiscard = false;
        public bool finiteUses = false;
        public bool endTurn = false;
        public float timing = 0.2f;

        public virtual void RunButtonClicked()
        {
            if ((bool)References.Battle && References.Battle.phase == Battle.Phase.Play && CorrectPlace() && !target.IsSnowed && target.owner == References.Player)
            {
                target.StartCoroutine(ButtonClicked());
            }
        }

        public virtual bool CorrectPlace()
        {
            if (fromBoard && Battle.IsOnBoard(target))
            {
                return true;
            }
            if (fromHand && References.Player.handContainer.Contains(target))
            {
                return true;
            }
            if (fromDraw && target.preContainers.Contains(References.Player.drawContainer))
            {
                return true;
            }
            if (fromDiscard && target.preContainers.Contains(References.Player.discardContainer))
            {
                return true;
            }
            return false;
        }

        //Main Code
        public int fixedAmount = 0;
        public int hitDamage = 0;

        public IEnumerator ButtonClicked()
        {
            if (hitDamage != 0)
            {
                List<Entity> enemies = GetTargets();
                int trueAmount = (hitDamage == -1) ? count : hitDamage;
                foreach (Entity enemy in enemies)
                {
                    if (enemy.IsAliveAndExists())
                    {
                        Hit hit = new Hit(target, enemy, trueAmount);
                        hit.canRetaliate = false;
                        yield return hit.Process();
                    }

                }

            }
            yield return Run(GetTargets(), fixedAmount);
            target.display.promptUpdateDescription = true;
            yield return PostClick();
        }

        public virtual IEnumerator PostClick()
        {
            if (finiteUses)
            {
                count--;
                if (count == 0)
                {
                    yield return Remove();
                }
                target.promptUpdate = true;
            }
            if (endTurn)
            {
                yield return Sequences.Wait(timing);
                References.Player.endTurn = true;
            }
        }

        public void ButtonCreate(StatusIconExt icon)
        {
            return;
        }
    }

    public class StatusEffectUntilTurnEnd : StatusEffectInstant
    {
        public override void Init()
        {
            base.OnTurnEnd += Remove;
        }

        public virtual IEnumerator Remove(Entity entity)
        {
            return Remove();
        }

        public override IEnumerator Process()
        {
            yield break;
        }

    }

    public class StatusEffectTraitUntilTurnEnd : StatusEffectUntilTurnEnd
    {
        public TraitData trait;

        public Entity.TraitStacks added;

        public int addedAmount = 0;

        public override bool HasStackRoutine => true;

        public override bool HasEndRoutine => true;

        public override void Init()
        {

            base.Init();
        }

        public override IEnumerator BeginRoutine()
        {
            added = target.GainTrait(trait, count, temporary: true);
            yield return target.UpdateTraits();
            addedAmount += count;
            target.display.promptUpdateDescription = true;
            target.PromptUpdate();
        }

        public override IEnumerator StackRoutine(int stacks)
        {
            added = target.GainTrait(trait, stacks, temporary: true);
            yield return target.UpdateTraits();
            addedAmount += stacks;
            target.display.promptUpdateDescription = true;
            target.PromptUpdate();
        }

        public override IEnumerator EndRoutine()
        {
            if ((bool)target)
            {
                if (added != null)
                {
                    added.count -= addedAmount;
                    added.tempCount -= addedAmount;
                }

                addedAmount = 0;
                yield return target.UpdateTraits(added);
                target.display.promptUpdateDescription = true;
                target.PromptUpdate();
            }
        }
    }

    public class StatusEffectBoostUntilTurnEnd : StatusEffectUntilTurnEnd
    {
        public override void Init()
        {
            base.Init();
        }
        public override IEnumerator Process()
        {
            int amount = GetAmount();
            if ((bool)target.curveAnimator)
            {
                target.curveAnimator.Ping();
            }

            target.effectBonus += amount;
            target.PromptUpdate();


            return base.Process();
        }

        public override bool RunStackEvent(int stacks)
        {
            int amount = GetAmount();
            if ((bool)target.curveAnimator)
            {
                target.curveAnimator.Ping();
            }

            target.effectBonus += stacks;
            target.PromptUpdate();
            return base.RunStackEvent(stacks);
        }

        public override IEnumerator Remove(Entity entity)
        {
            target.effectBonus -= GetAmount();
            return base.Remove(entity);
        }
    }

    public class StatusTokenMoveContainer : StatusEffectData, IStatusToken
    {
        //Standard Code
        public bool fromBoard = true;
        public bool fromHand = false;
        public bool fromDraw = false;
        public bool fromDiscard = false;
        public bool finiteUses = false;
        public bool endTurn = false;
        public float timing = 0.2f;

        public virtual void RunButtonClicked()
        {
            if ((bool)References.Battle && References.Battle.phase == Battle.Phase.Play && CorrectPlace() && !target.IsSnowed && target.owner == References.Player)
            {
                target.StartCoroutine(ButtonClicked());
            }
        }

        public virtual bool CorrectPlace()
        {
            if (fromBoard && Battle.IsOnBoard(target))
            {
                return true;
            }
            if (fromHand && References.Player.handContainer.Contains(target))
            {
                return true;
            }
            if (fromDraw && target.preContainers.Contains(References.Player.drawContainer))
            {
                return true;
            }
            if (fromDiscard && target.preContainers.Contains(References.Player.discardContainer))
            {
                return true;
            }
            return false;
        }

        public virtual IEnumerator PostClick()
        {
            if (finiteUses)
            {
                count--;
                if (count == 0)
                {
                    yield return Remove();
                }
                target.promptUpdate = true;
            }
            if (endTurn)
            {
                yield return Sequences.Wait(timing);
                References.Player.endTurn = true;
            }
        }

        //Main Code
        public Container toContainer;
        public bool top = true;

        public enum Container
        {
            DrawPile,
            Hand,
            DiscardPile
        }

        public CardContainer FindContainer()
        {
            switch(toContainer)
            {
                case Container.DrawPile:
                    return References.Player.drawContainer;
                case Container.DiscardPile:
                    return References.Player.discardContainer;
                case Container.Hand:
                    return References.Player.handContainer;
            }
            throw new Exception("Did you forget to declare toContainer when building the StatusEffect?");
        }

        public IEnumerator ButtonClicked()
        {
            CardContainer cc = FindContainer();
            int index = cc.Count;
            CardPocketSequence sequence = UnityEngine.GameObject.FindObjectOfType<CardPocketSequence>();
            CardPocketSequence.Card card = null;
            if (sequence != null)
            {
                for(int i=0; sequence.cards.Count > 0; i++)
                {
                    if (sequence.cards[i].entity == target)
                    {
                        card = sequence.cards[i];
                        target.transform.SetParent(References.instance.transform, true);
                        sequence.cards.RemoveAt(i);
                        break;
                    }
                }
                sequence.promptEnd = true;
                yield return new WaitUntil(() => !sequence.isActiveAndEnabled);
                card.Reset();
                card.Return();
                yield return new WaitForSeconds(0.25f);
            }
            if (cc.Contains(target))
            {
                index -= 1;
            }
            if (!top)
            {
                index = 0;
            }
            yield return Sequences.CardMove(target, new CardContainer[1] { cc }, index);
            foreach(CardContainer c in target.preContainers) 
            {
                c.TweenChildPositions();
            }
            if(!target.preContainers.Contains(cc))
            {
                cc.TweenChildPositions();
            }
            yield return PostClick();
        }

        public void ButtonCreate(StatusIconExt icon)
        {
            return;
        }
    }

    //Ordinary Status Effects
    public class StatusEffectGiveUpgradeOnDeath : StatusEffectData
    {
        public List<CardUpgradeData> data;
        public override void Init()
        {
            base.OnEntityDestroyed += EntityDestroyed;
            base.Init();
        }

        private IEnumerator EntityDestroyed(Entity entity, DeathType deathType)
        {
            if (entity == target && data.Count > 0)
            {
                List<CardUpgradeData> actualData = AddressableLoader.Get<StatusEffectGiveUpgradeOnDeath>("StatusEffectData", name).data;
                CardUpgradeData token = actualData[0];
                References.PlayerData.inventory.upgrades.Add(token.Clone());
                actualData.RemoveAt(0);
                actualData.Add(token);
            }
            yield break;
        }
    }

    public class StatusEffectPrism : StatusEffectApplyX
    {
        public override void Init()
        {
            base.PostApplyStatus += Refract;
            base.Init();
        }

        private IEnumerator Refract(StatusEffectApply apply)
        {
            if (apply.count == 0 || apply.target != target)
            {
                yield break;
            }
            if (!apply.effectData.type.IsNullOrWhitespace() || apply.effectData.isStatus)
            {
                effectToApply = apply.effectData;
                yield return Run(GetTargets(),apply.count);
                yield return CountDown(target,1);
                target.display.promptUpdateDescription = true;
            }
        }
    }
}
