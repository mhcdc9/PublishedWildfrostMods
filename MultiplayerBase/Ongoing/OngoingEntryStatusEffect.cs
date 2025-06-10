using MultiplayerBase.Handlers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static StatusEffectApplyX;

namespace MultiplayerBase.Ongoing
{
    public class OngoingEntryStatusEffect : OngoingEffectSystem.Entry
    {
        /*
        [Flags]
        public enum ApplyToFlags
        {
            None = 0,
            Leader = 1,
            Boss = 2,
            Hand = 4,
            Allies = 8,
            AlliesInRow = 16,
            Enemies = 32,
            EnemiesInRow = 64,
            Slot = 128,
            Etc = 256,
        }
        */

        public ApplyToFlags applyToFlags;
        public StatusEffectData effectToApply;
        public TargetConstraint[] applyConstraints;

        public List<Entity> affected;

        public int id;
        int currentAmount;
        int owner;
        int rowIndex;
        int rowSlot;

        public OngoingEntryStatusEffect(StatusEffectData effectToApply, ApplyToFlags applyToFlags)
        {
            this.effectToApply = effectToApply;
            this.applyToFlags = applyToFlags;
            this.applyConstraints = new TargetConstraint[0];
        }

        public virtual OngoingEffectSystem.Entry Create(string[] info)
        {
            OngoingEntryStatusEffect entry = new OngoingEntryStatusEffect(this.effectToApply, this.applyToFlags);
            entry.id = int.Parse(info[1]);
            string[] extraInfo = HandlerSystem.DecodeMessages(info[3]);
            entry.owner = int.Parse(extraInfo[0]);
            entry.rowIndex = int.Parse(extraInfo[1]);
            entry.rowSlot = int.Parse(extraInfo[2]);
            return entry;

        }


        public virtual void Activate(int amount, string extraInfo)
        {
            currentAmount = amount;
            if (amount <= 0)
            {
                return;
            }
            ActionSequence action = new ActionSequence(ApplyStacks(amount))
            {
                note = $"Applying {amount} {effectToApply.name}"
            };
            HandlerBattle.instance.Queue(action);
        }

        public virtual IEnumerator ApplyStacks(int amount)
        {
            Routine.Clump clumpy = new Routine.Clump();
            List<Entity> targets = GetTargets();
            foreach (Entity target in targets)
            {
                clumpy.Add(StatusEffectSystem.Apply(target, target, effectToApply, amount, true));
            }
            yield return clumpy.WaitForEnd();
            yield return Sequences.Wait(0.1f);
            affected = targets;
        }

        public virtual bool ChangeAmount(int newAmount, string extraInfo)
        {
            Deactivate();
            if (newAmount <= 0)
            {
                return true;
            }
            Activate(newAmount, extraInfo);
            return false;
        }

        public virtual void Deactivate()
        {
            ActionSequence action = new ActionSequence(RemoveStacks(currentAmount))
            {
                note = $"Removing {currentAmount} {effectToApply.name}"
            };
            HandlerBattle.instance.Queue(action);
        }

        public virtual IEnumerator RemoveStacks(int amount)
        {
            Routine.Clump clumpy = new Routine.Clump();
            foreach(Entity target in affected)
            {
                StatusEffectData data = target.statusEffects.FirstOrDefault(s => s.name == effectToApply.name);
                if (data != null)
                {
                    clumpy.Add(data.RemoveStacks(amount, true));
                }
            }
            return clumpy.WaitForEnd();
        }

        public bool Equals(string[] data)
        {
            return (id == int.Parse(data[2]));
        }

        public virtual List<Entity> GetTargets()
        {
            List<Entity> targets = new List<Entity>();
            if (Battle.instance == null)
            {
                return targets;
            }

            Character player = (References.Player.team == owner) ? References.Player : Battle.GetOpponent(References.Player);
            Character opponent = Battle.GetOpponent(References.Player);

            if (AppliesTo(ApplyToFlags.Hand))
            {
                CardContainer handContainer = References.Player?.handContainer;
                if (handContainer != null && handContainer.Count > 0)
                {
                    targets.AddRange(References.Player.handContainer);//.Where(c => CheckConstraints(c)));
                }
            }
            if (AppliesTo(ApplyToFlags.RightCardInHand))
            {
                CardContainer handContainer = References.Player?.handContainer;
                if (handContainer != null && handContainer.Count > 0)
                {
                    targets.Add(handContainer[0]);
                }
            }
            if (AppliesTo(ApplyToFlags.Allies))
            {
                targets.AddRange(Battle.GetCardsOnBoard(player));
            }
            else if (AppliesTo(ApplyToFlags.AlliesInRow))
            {
                foreach(Entity entity in References.Battle.GetRow(player, rowIndex))
                {
                    if (!targets.Contains(entity))
                    {
                        targets.Add(entity);
                    }
                }
            }
            if (AppliesTo(ApplyToFlags.Enemies))
            {
                targets.AddRange(Battle.GetCardsOnBoard(opponent));
            }
            else if (AppliesTo(ApplyToFlags.EnemiesInRow))
            {
                foreach (Entity entity in References.Battle.GetRow(opponent, rowIndex))
                {
                    if (!targets.Contains(entity))
                    {
                        targets.Add(entity);
                    }
                }
            }

            return targets.Where(c => CheckConstraints(c)).ToList();
        }

        public bool AppliesTo(ApplyToFlags applyTo)
        {
            return ((applyToFlags & applyTo) != 0);
        }

        public bool CanAffect(Entity entity)
        {
            if (effectToApply.targetConstraints == null)
            {
                return true;
            }
            return effectToApply.targetConstraints.All(c => c.Check(entity));
        }

        public bool CheckConstraints(Entity entity)
        {
            if (CanAffect(entity))
            {
                return applyConstraints.All((TargetConstraint c) => c.Check(entity));
            }

            return false;
        }
    }

}
