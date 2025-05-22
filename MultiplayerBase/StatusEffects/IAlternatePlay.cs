using MultiplayerBase.Battles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerBase.StatusEffects
{
    /* The IAlternatePlay interface allows a modder to define what happens when your card targets something in the battle viewer.
     * The default is that your card gets played on another board, but implementing this interface for your status effect will do otherwise.
     * 
     * Notes:
     * - Experimental & Untested: Tell me if it works!
     */
    public interface IAlternatePlay
    {
        //If ALL Preprocesses run true, then the card will be sent via ActionSendCardToPlay.
        bool PreProcess(object context, ActionSendCardToPlay.TargetType type);

        //Process always run inside of a ActionSendCardToPlay
        IEnumerator Process();
    }
}
