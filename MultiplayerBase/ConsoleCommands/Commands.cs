using MultiplayerBase.Handlers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Console;

namespace MultiplayerBase.ConsoleCommands
{
    internal class Commands
    {
        public class CommandMultASK : Command
        {
            public override string id => "multiplayer ASK";

            public override string format => "multiplayer ask <friend> <handler>";

            public override string desc => "Sends an ASK message to a friend";

            public override bool IsRoutine => false;
            public override void Run(string args)
            {
                UnityEngine.Debug.Log(args);
            }

            public override IEnumerator GetArgOptions(string currentArgs)
            {
                int length = currentArgs.Split(new Char[] { ' ' },options: StringSplitOptions.None).Length;
                if (length == 3)
                {
                    IEnumerable<string> keys = HandlerSystem.HandlerRoutines.Keys;
                    predictedArgs = keys.ToArray();
                    yield break;
                }

                if (length == 4)
                {
                    IEnumerable<string> friends = HandlerSystem.friends.Select(x => x.Name);
                    predictedArgs = friends.ToArray();
                    yield break;
                }
                
            }
        }
    }
}
