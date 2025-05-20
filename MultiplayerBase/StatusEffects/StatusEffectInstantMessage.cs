using MultiplayerBase.Handlers;
using MultiplayerBase.UI;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerBase.StatusEffects
{
    public class StatusEffectInstantMessage : StatusEffectInstant
    {
        public enum ToWhom
        {
            Self,
            All,
            Random,
            Select,
            Custom, //Future proofing
            Misc //Future proofing
        }

        public string handler;
        public ToWhom toWhom = ToWhom.Self;
        public string[] parts = new string[0];
        public bool includeSelf = false;

        public override IEnumerator Process()
        {
            if (HandlerSystem.enabled)
            {
                string fullMessage = HandlerSystem.ConcatMessage(true, parts);
                yield return SendMessage(fullMessage);
            }
            yield return base.Process();
        }

        public virtual string Convert(string original)
        {
            return string.Format(original, count, CardEncoder.Encode(target), target.data.id.ToString());
        }

        public virtual IEnumerator SendMessage(string fullMessage)
        {
            Debug.Log($"[Multiplayer]: {fullMessage}");
            switch(toWhom)
            {
                case ToWhom.Self: HandlerSystem.SendMessage(handler, HandlerSystem.self, fullMessage);
                    break;
                case ToWhom.All: HandlerSystem.SendMessageToAll(handler, fullMessage, includeSelf);
                    break;
                case ToWhom.Random: HandlerSystem.SendMessageToRandom(handler, fullMessage, includeSelf);
                    break;
                case ToWhom.Select: 
                    yield return Dashboard.SelectFriend(includeSelf);
                    if (Dashboard.selectedFriend is Friend f)
                    {
                        HandlerSystem.SendMessage(handler, f, fullMessage);
                    }
                    break;
                default: Debug.Log("[Multiplayer] Message not send (did you forget to set ToWhom?)");
                    break;
            }
        }
    }
}
