using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Sync
{
    internal class TargetConstraintPlayableOnBoard : TargetConstraint
    {
        public override bool Check(Entity target)
        {
            return Check(target.data);
        }

        public override bool Check(CardData targetData)
        {
            if (targetData.canPlayOnBoard || !targetData.needsTarget || targetData.targetMode.TargetRow)
            {
                return !not;
            }
            return not;
        }

        private static void Diagnostics()
        {
            TargetConstraintPlayableOnBoard t = ScriptableObject.CreateInstance<TargetConstraintPlayableOnBoard>();
            List<CardData> cards = AddressableLoader.GetGroup<CardData>("CardData");
            foreach (CardData card in cards)
            {
                try
                {
                    Debug.Log($"[Sync] {card.title}: {t.Check(card)}");
                }
                catch(Exception e)
                {
                    Debug.Log("[Sync] Found an exception: " + e.Message);
                }
            }
        }
    }
}
