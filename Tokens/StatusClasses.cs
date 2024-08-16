using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Building;
using CardPlaces = Tokens.Extensions.CardPlaces;

namespace Tokens
{
    public interface IStatusToken
    {
        CardPlaces ValidPlaces { get; }
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
        public CardPlaces validPlaces = CardPlaces.Board | CardPlaces.Hand;
        public CardPlaces ValidPlaces { get => validPlaces; }
        public bool finiteUses = true;
        public bool endTurn = false;
        public float timing = 0.2f;
        public bool snowOverride = false;

        public virtual void RunButtonClicked()
        {
            if ((bool)References.Battle && References.Battle.phase == Battle.Phase.Play && this.CorrectPlace(target) && (!target.IsSnowed || snowOverride) && (!target.silenced) && target.owner == References.Player)
            {
                target.StartCoroutine(ButtonClicked());
            }
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
            base.OnCardMove += CheckPosition;
            Events.OnBattleTurnEnd += Remove;
        }

        public void OnDestroy()
        {
            Events.OnBattleTurnEnd -= Remove;
        }

        public IEnumerator CheckPosition(Entity entity)
        {
            if (entity == target && (target.containers.Contains(References.Player.drawContainer) || target.containers.Contains(References.Player.discardContainer)) )
            {
                yield return Remove();
            }
            yield break;
        }

        public virtual void Remove(int _)
        {
            target.StartCoroutine(Remove());
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

        public override void Remove(int _)
        {
            target.effectBonus -= GetAmount();
            base.Remove(_);
        }
    }

    public class StatusTokenMoveContainer : StatusEffectData, IStatusToken
    {
        //Standard Code
        public CardPlaces validPlaces;
        public CardPlaces ValidPlaces { get => validPlaces; }
        public bool finiteUses = true;
        public bool endTurn = false;
        public float timing = 0.2f;
        public bool snowOverride = false;

        public virtual void RunButtonClicked()
        {
            if ((bool)References.Battle && References.Battle.phase == Battle.Phase.Play && this.CorrectPlace(target) && (!target.IsSnowed || snowOverride) && (!target.silenced) && target.owner == References.Player)
            {
                target.StartCoroutine(ButtonClicked());
            }
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
            switch (toContainer)
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
                for (int i = 0; sequence.cards.Count > 0; i++)
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
            foreach (CardContainer c in target.preContainers)
            {
                c.TweenChildPositions();
            }
            if (!target.preContainers.Contains(cc))
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
        public static List<CardUpgradeData> data = new List<CardUpgradeData>();
        public override void Init()
        {
            base.OnEntityDestroyed += EntityDestroyed;
            base.Init();
        }

        private IEnumerator EntityDestroyed(Entity entity, DeathType deathType)
        {
            if (entity == target)
            {
                if (data.Count == 0)
                {
                    LoadTokens();
                }
                References.PlayerData.inventory.upgrades.Add(data[0].Clone());
                data.RemoveAt(0);
            }
            yield break;
        }

        protected void LoadTokens()
        {
            Debug.Log($"Is the player in References already {References.Player != null}");
            //Randomize token list
            List<CardUpgradeData> tokenList = new List<CardUpgradeData>();
            foreach (string key in TokenMain.TokenRewards.Keys)
            {
                Debug.Log(key);
                if (key == "General" || key == References.Player.title)
                {
                    for (int i = TokenMain.TokenRewards[key].Count - 1; i >= 0; i--)
                    {
                        Debug.Log(i);
                        if (TokenMain.TokenRewards[key][i] == null)
                        {
                            TokenMain.TokenRewards[key].RemoveAt(i);
                        }
                    }
                    tokenList.AddRange(TokenMain.TokenRewards[key]);
                }
            }
            data = tokenList.InRandomOrder().ToList();
        }
    }

    public class StatusEffectPrism : StatusEffectApplyX
    {
        public override void Init()
        {
            base.PostApplyStatus += Refract;
            base.Init();
        }

        public override bool RunPostApplyStatusEvent(StatusEffectApply apply)
        {
            if (apply.applier == null || apply.target == null || apply.effectData == null)
            {
                return false;
            }
            if (apply.count == 0 || apply.target != target)
            {
                return false;
            }
            if (apply.applier != target && apply.applier.FindStatus("prism") != null)
            {
                return false;
            }
            if (!apply.effectData.type.IsNullOrWhitespace() || apply.effectData.isStatus)
            {
                return true;
            }
            return false;
        }
        private IEnumerator Refract(StatusEffectApply apply)
        {
            effectToApply = apply.effectData;
            yield return Run(GetTargets(), apply.count);
            int amount = 1;
            Events.InvokeStatusEffectCountDown(this, ref amount);
            yield return CountDown(target, amount);
            target.display.promptUpdateDescription = true;
            target.PromptUpdate();
        }
    }

    public class StatusEffectConvertDamage : StatusEffectApplyX
    {
        public override void Init()
        {
            base.Init();
            base.OnHit += ConvertDamage;
        }

        public override bool RunHitEvent(Hit hit)
        {
            if (hit.attacker == target)
            {
                return true;
            }
            return false;
        }

        protected IEnumerator ConvertDamage(Hit hit)
        {
            int damage = hit.damage;
            hit.damage = 0;
            if (damage > 0)
            {
                hit.AddStatusEffect(new CardData.StatusEffectStacks(effectToApply, damage));
            }
            int amount = 1;
            Events.InvokeStatusEffectCountDown(this, ref amount);
            yield return CountDown(target, amount);
            target.display.promptUpdateDescription = true;
            target.PromptUpdate();
        }
    }

    public class StatusEffectInstantConvertDebuffs : StatusEffectInstantCleanse
    {
        public StatusEffectData effectToApply;
        public int initialStacks = 0;
        public override IEnumerator Process()
        {
            int stacks = initialStacks;
            int num = target.statusEffects.Count;
            for (int i = num - 1; i >= 0; i--)
            {
                StatusEffectData statusEffectData = target.statusEffects[i];
                if (statusEffectData.offensive && statusEffectData.visible && statusEffectData.isStatus)
                {
                    stacks += statusEffectData.count;
                    yield return statusEffectData.Remove();
                }
            }
            yield return StatusEffectSystem.Apply(target, target, effectToApply, stacks);
            target.display.promptUpdateDescription = true;
            target.PromptUpdate();
            yield return base.Process();
        }
    }
}



