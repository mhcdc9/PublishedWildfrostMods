using MultiplayerBase.Ongoing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerBase.Battles
{
    public class ActionOngoingStatusEffect : PlayAction
    {
        OngoingEntryStatusEffect entry;
        bool remove;
        int amount;

        public ActionOngoingStatusEffect(OngoingEntryStatusEffect entry, int amount, bool remove = false)
        {
            this.entry = entry;
            this.amount = amount;
            note = remove ? $"Removing {entry.effectToApply.name}" : $"Applying {entry.effectToApply.name}";
            this.remove = remove;
        }

        public override IEnumerator Run()
        {
            if (!remove)
            {
                yield return entry.ApplyStacks(amount);
            }
            
        }
    }
}
