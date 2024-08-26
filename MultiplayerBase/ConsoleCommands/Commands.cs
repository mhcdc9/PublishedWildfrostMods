﻿using MultiplayerBase.Handlers;
using MultiplayerBase.UI;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Console;

namespace MultiplayerBase.ConsoleCommands
{
    internal class Commands
    {
        public class CommandMultASK : Command
        {
            public override string id => "multiplayer";

            public override string format => "multiplayer <friend> <handler> <message>";

            public override string desc => "Sends an ASK message to a friend";

            public override bool IsRoutine => false;
            public override void Run(string args)
            {
                UnityEngine.Debug.Log(args);
                string[] parameters = args.Split(new char[] { ' ' }, StringSplitOptions.None);

                if (parameters.Length < 3)
                {
                    Fail("Wrong number of arguments");
                    return;
                }
                Friend? selectedFriend = null;
                bool found = false;
                foreach (Friend friend in HandlerSystem.friends)
                {
                    if (parameters[0].ToLower() == friend.Name.ToLower())
                    {
                        selectedFriend = friend;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Fail($"Friend {parameters[0].ToLower()} not found");
                    return;
                }

                if (HandlerSystem.HandlerRoutines.Keys.Contains(parameters[1].ToUpper()))
                {
                    HandlerSystem.SendMessage(parameters[1], (Friend)selectedFriend, string.Join(" ",parameters.Skip(2)));
                }
                else
                {
                    Fail($"No Handler by the name of {parameters[1].ToUpper()}");
                }
            }

            public override IEnumerator GetArgOptions(string currentArgs)
            {
                int length = currentArgs.Split(new Char[] { ' ' },options: StringSplitOptions.None).Length;
                if (length == 2)
                {
                    IEnumerable<string> keys = HandlerSystem.HandlerRoutines.Keys;
                    predictedArgs = keys.ToArray();
                    yield break;
                }

                if (length == 1)
                {
                    IEnumerable<string> friends = HandlerSystem.friends.Select(x => x.Name);
                    predictedArgs = friends.ToArray();
                    yield break;
                }
                
            }
        }

        public class CommandMultShuffle : Command
        {
            public override string id => "multShuffle";

            public override string format => "multShuffle <side>";

            public override string desc => "Shuffle the position of cards on the battle viewer";

            public override bool IsRoutine => false;
            public override void Run(string args)
            {
                if (!HandlerBattle.instance.Blocking)
                {
                    Fail("Battle viewer must be open");
                    return;
                }

                string message = "";
                switch(args.ToUpper())
                {
                    case "PLAYER":
                        message = EncodePositions(HandlerBattle.instance.playerLanes);
                        break;
                    case "ENEMY":
                        message = EncodePositions(HandlerBattle.instance.enemyLanes);
                        break;
                }
                if (message.IsNullOrWhitespace())
                {
                    Fail("No side selected");
                    return;
                }
                string message2 = HandlerSystem.ConcatMessage(true, "BOARD", args.ToUpper());

                HandlerSystem.SendMessage("BAT", (Friend)HandlerBattle.friend, HandlerSystem.ConcatMessage(false, message2, message));
            }

            public string EncodePositions(OtherCardViewer[] ocvs)
            {
                List<ulong> positions = new List<ulong>();
                for(int i = 0; i<3; i++)
                {
                    for(int j = 0; j < 2; j++)
                    {
                        if (ocvs[j].Count > i && ocvs[j][i] != null)
                        {
                            positions.Add(ocvs[j].Find(ocvs[j][i]).Item2);
                        }
                    }
                }

                return HandlerSystem.ConcatMessage(true, positions.InRandomOrder().Select(x =>  x.ToString()).ToArray());
            }

            public override IEnumerator GetArgOptions(string currentArgs)
            {
                string[] options = new string[] { "PLAYER", "ENEMY" };
                predictedArgs = options;
                yield break;
            }
        }

        public class CommandMultSac : Command
        {
            public override string id => "multSac";

            public override string format => "multSac";

            public override string desc => "Test";

            public override bool IsRoutine => false;

            public static ParticleSystem.Particle[] particles = new ParticleSystem.Particle[10];
            public static int child = 4;
            public static float duration = 0.5f;
            public override void Run(string args)
            {
                VfxDeathSystem system = GameObject.FindObjectOfType<VfxDeathSystem>();
                References.instance.StartCoroutine(SkullThingy(system.sacrificeFX.transform.GetChild(child)));
            }

            public IEnumerator SkullThingy(Transform transform)
            {
                GameObject obj = transform.gameObject.InstantiateKeepName();
                yield return new WaitForSeconds(duration);
                obj.GetComponent<ParticleSystem>().Pause();
                yield return new WaitForSeconds(2f);
                obj.Destroy();
            }

            public override IEnumerator GetArgOptions(string currentArgs)
            {
                yield break;
            }
        }
    }
}