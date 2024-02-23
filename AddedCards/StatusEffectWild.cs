using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AddedCards
{
    public class StatusEffectWild : StatusEffectData
    {
        [SerializeField]
        public TraitData trait;

        public List<Entity.TraitStacks> silenced;

        public Entity.TraitStacks added;

        public int addedAmount;

        public override bool HasStackRoutine => true;

        public override bool HasEndRoutine => true;

        public override bool Instant => true;

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
}
