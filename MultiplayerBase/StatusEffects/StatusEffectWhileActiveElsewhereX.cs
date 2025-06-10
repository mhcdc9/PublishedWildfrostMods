using MultiplayerBase.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MultiplayerBase.Ongoing.OngoingEffectSystem;

namespace MultiplayerBase.StatusEffects
{
    /*
    internal class StatusEffectWhileActiveElsewhereX : StatusEffectApplyX, Agent
    {
        public enum ToWhom
        {
            All,
            AllOthers,
            Select,
        }

        public bool active;
        public string entryName;

        public override void Init()
        {

        }

        public override bool RunBeginEvent()
        {
            if (target.enabled && CanActivate())
            {
                Activate();
            }
            return false;
        }

        public override bool RunEnableEvent(Entity entity)
        {
            if (!active && entity==target && CanActivate())
            {

            }
        }

        public virtual bool CanActivate()
        {
            return Battle.IsOnBoard(target);
        }

        public override bool RunCardMoveEvent(Entity entity)
        {
            
        }

        public void Activate()
        {
            if (active) return;

            string info = GetInfo();

            active = true;
            string s = HandlerSystem.ConcatMessage(true, "ONGOING", "ACTIVATE", info);
            HandlerSystem.SendMessageToAll("MSC", s);
        }

        public void UpdateEffect()
        {
            
        }

        public void Deactivate()
        {
            if (!active) return;

            string info = HandlerSystem.ConcatMessage(true, entryName, "0", "0", ""); ;

            active = false;
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

            return HandlerSystem.ConcatMessage(true, entryName, "0", GetAmount().ToString(), extraInfo);
        }

        public string UpdateOngoing()
        {
            return "";
        }
    }
    */
}
