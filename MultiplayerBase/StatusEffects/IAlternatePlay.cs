using MultiplayerBase.Battles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerBase.StatusEffects
{
    public interface IAlternatePlay
    {
        //If ALL Preprocesses run true, then the card will be sent via ActionSendCardToPlay.
        bool PreProcess(object context, ActionSendCardToPlay.TargetType type);

        //Process always run inside of a ActionSendCardToPlay
        IEnumerator Process();
    }
}
