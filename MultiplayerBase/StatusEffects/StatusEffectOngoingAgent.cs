using MultiplayerBase.Handlers;
using MultiplayerBase.Ongoing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MultiplayerBase.Ongoing.OngoingEffectSystem;

namespace MultiplayerBase.StatusEffects
{
    public class StatusEffectOngoingAgent : StatusEffectData, Agent
    {
        public enum ToWhom
        {
            All,
            AllOthers,
            Select,
        }

        protected bool active;
        protected int agentId = 0;
        
        public string entryName;

        public override bool RunBeginEvent()
        {
            if (target.enabled)
            {
                Activate();
            }
            return false;
        }

        public void OnDestroy()
        {
            Deactivate();
        }

        public void Activate()
        {
            if (active) return;

            if (agentId == 0)
            {
                agentId = idMax;
                idMax++;
            }
            string info = GetInfo();

            active = true;
            OngoingEffectSystem.agents.Add(this);
            string s = HandlerSystem.ConcatMessage(true, "ONGOING", "ACTIVATE", info);
            HandlerSystem.SendMessageToAll("MSC", s);
        }

        public void Deactivate()
        {
            if (!active) return;

            string info = HandlerSystem.ConcatMessage(true, entryName, agentId.ToString(), "0", ""); ;

            active = false;
            OngoingEffectSystem.agents.Remove(this);
            string s = HandlerSystem.ConcatMessage(true, "ONGOING", "UPDATE", info);
            HandlerSystem.SendMessageToAll("MSC", s);
        }

        public string GetInfo()
        {
            string extraInfo = "0! 0! 0!";
            if (target.actualContainers[0] is CardSlot slot && slot.Group is CardSlotLane lane)
            {
                extraInfo = HandlerSystem.ConcatMessage(true,
                    target.owner.team.ToString(),
                    Battle.instance.GetRowIndex(lane).ToString(),
                    lane.slots.IndexOf(slot).ToString());
            }

            return HandlerSystem.ConcatMessage(true, entryName, agentId.ToString(), GetAmount().ToString(), extraInfo);
        }

        public string UpdateOngoing()
        {
            return "";
        }
    }
}
