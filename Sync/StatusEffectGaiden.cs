using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiplayerBase.Handlers;
using UnityEngine;
using Net = MultiplayerBase.Handlers.HandlerSystem;

namespace Sync
{
    public class StatusEffectGaiden : StatusEffectData
    {
        //lol
    }

    public class StatusEffectSideQuest : StatusEffectData
    {
        public override void Init()
        {
            GaidenSystem.OnRecallWanderer += Check;
            Events.OnBattleLoaded += LeaveAfterLoad;
        }

        public void OnDestroy()
        {
            GaidenSystem.OnRecallWanderer -= Check;
            Events.OnBattleLoaded -= LeaveAfterLoad;
        }

        public override bool RunEntityDestroyedEvent(Entity entity, DeathType deathType)
        {
            if (entity == target && target.data.customData != null && target.data.customData.ContainsKey("ActualId"))
            {
                string s = Net.ConcatMessage(false, "GAIDEN", "INJURE", (string)target.data.customData["ActualId"]);
                Net.SendMessageToAll("SYNC", s);
            }
            return false;
        }

        public void Check(string nameId)
        {
            if (Battle.instance != null && target.IsAliveAndExists() && target.data.customData != null && target.data.customData.ContainsKey("ActualId") && (string)target.data.customData["ActualId"] == nameId)
            {
                ActionQueue.Stack(new ActionSequence(Leave()));
            }
        }

        public void LeaveAfterLoad()
        {
            ActionQueue.Stack(new ActionSequence(Leave()));
        }

        public IEnumerator Leave()
        {
            if (!Battle.IsOnBoard(target))
            {
                LeanTween.move(target.gameObject, Vector3.zero, 0.4f).easeInOutQuart();
                yield return Sequences.Wait(0.4f);
                if (target.flipper.flipped)
                {
                    target.flipper.FlipUp();
                    yield return Sequences.Wait(0.5f);
                }
            }
            target.curveAnimator.Ping();
            yield return Sequences.Wait(0.5f);
            ActionFlee flee = new ActionFlee(target);
            yield return flee.Run();
        }
    }
}
