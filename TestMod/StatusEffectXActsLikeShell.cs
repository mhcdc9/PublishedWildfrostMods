using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking.Types;

namespace TestMod
{
    internal class StatusEffectXActsLikeShell : StatusEffectData
    {

        public override bool HasHitRoutine => true;
        public override bool HasPostApplyStatusRoutine => true;

        public string targetType = "";
        public string spriteName;

        public override IEnumerator PostApplyStatusRoutine(StatusEffectApply apply)
        {
            if (apply?.target?.owner == target.owner && apply.effectData.type == targetType)
            {
                yield return Sequences.Wait(apply.target.curveAnimator.Ping());
            }
        }

        public override IEnumerator HitRoutine(Hit hit)
        {
            if (hit?.target?.owner == target.owner && hit.target.FindStatus(targetType))
            {
                StatusEffectData targetEffect = hit.target.FindStatus(targetType);
                while (targetEffect.count > 0 && hit.damage > 0)
                {
                    targetEffect.count--;
                    hit.damage--;
                    hit.damageBlocked++;
                }

                if (targetEffect.count <= 0)
                {
                    yield return targetEffect.Remove();
                }

                target.PromptUpdate();
            }
        }
    }
}
