using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using MultiplayerBase.Handlers;
using MultiplayerBase.UI;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

using static Console;

namespace MultiplayerBase.ConsoleCommands
{
    internal class Commands
    {
        public static IEnumerator AddCustomCommands(WildfrostMod _)
        {
            yield return new WaitUntil(() => SceneManager.Loaded.ContainsKey("MainMenu"));
            commands.Add(new CommandMultASK());
            commands.Add(new CommandMultShuffle());
            commands.Add(new CommandSeeIDs());
            //commands.Add(new CommandMultSac());
            commands.Add(new CommandMultCHAT());
            //commands.Add(new CommandMultEMOTE());
        }

        public class CommandMultASK : Command
        {
            public override string id => "msend";

            public override string format => "msend <friend> <handler> <message>";

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
                    string s = string.Join(" ", parameters.Skip(2));
                    if (hover != null)
                    {
                        s = s.Replace("{{this}}", HandlerSystem.ConcatMessage(true, CardEncoder.Encode(hover)));
                        s = s.Replace("{this}", HandlerSystem.ConcatMessage(false, CardEncoder.Encode(hover)));
                        s = s.Replace("{id}", hover.data.id.ToString());
                    }
                    HandlerSystem.SendMessage(parameters[1], (Friend)selectedFriend, s);
                }
                else
                {
                    Fail($"No Handler by the name of {parameters[1].ToUpper()}");
                }
            }

            public override IEnumerator GetArgOptions(string currentArgs)
            {
                string[] args = currentArgs.Split(new Char[] { ' ' },options: StringSplitOptions.None);
                int length = args.Length;

                if (length <= 1)
                {
                    IEnumerable<string> friends = HandlerSystem.friends.Select(x => x.Name);
                    predictedArgs = friends.ToArray();
                    yield break;
                }

                else if (length == 2)
                {
                    IEnumerable<string> keys = HandlerSystem.HandlerRoutines.Keys;
                    predictedArgs = keys.Select(s => $"{args[0]} {s}").ToArray();
                    yield break;
                }

                else
                {
                    predictedArgs = new string[0];
                    yield break;
                }



            }
        }

        public class CommandMultCHAT : Command
        {
            public override string id => "chat";

            public override string format => "chat <friend> <message>";

            public override string desc => "Sends an chat message to a friend";

            public override bool IsRoutine => false;
            public override void Run(string args)
            {
                UnityEngine.Debug.Log(args);
                string[] parameters = args.Split(new char[] { ' ' }, StringSplitOptions.None);

                if (parameters.Length < 2)
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

                HandlerSystem.SendMessage(parameters[1], (Friend)selectedFriend, string.Join(" ", parameters.Skip(1)));
            }

            public override IEnumerator GetArgOptions(string currentArgs)
            {
                string[] args = currentArgs.Split(new char[] { ' ' }, options: StringSplitOptions.None);
                int length = args.Length;

                if (length <= 1)
                {
                    IEnumerable<string> friends = HandlerSystem.friends.Select(x => x.Name);
                    predictedArgs = friends.ToArray();
                    yield break;
                }

                else
                {
                    predictedArgs = new string[0];
                    yield break;
                }
            }
        }

        public class CommandSeeIDs : Command
        {
            public override string id => "id";

            public override string format => "id <on/off>";

            public override string desc => "See the ids of all cards on board/hand";

            public List<GameObject> activeTexts = new List<GameObject>();
            public List<GameObject> inactiveTexts = new List<GameObject>();

            public override bool IsRoutine => false;
            public override void Run(string args)
            {
                if (args.Trim() == "on")
                {
                    On();
                }
                else
                {
                    Off();
                }
            }

            public void On()
            {
                if (activeTexts.Count > 0)
                {
                    Off();
                }

                if (Battle.instance != null)
                {
                    Battle.instance.cards.Where(e => e?.display?.GetCanvas() != null)
                        .Do(e =>
                        {
                            if (!PullText(e))
                            {
                                CreateTextAndAssign(e);
                            }
                        });
                }
            }

            public void Off()
            {
                foreach (GameObject obj in activeTexts)
                {
                    if (obj != null)
                    {
                        obj.transform.SetParent(null, false);
                        inactiveTexts.Add(obj);
                    }
                }

                activeTexts.Clear();
            }

            public bool PullText(Entity entity)
            {
                while(inactiveTexts.Count > 0)
                {
                    if (inactiveTexts[0] == null)
                    {
                        inactiveTexts.RemoveAt(0);
                    }
                    else
                    {
                        GameObject obj = inactiveTexts[0];
                        obj.transform.SetParent(entity.display.GetCanvas().transform, false);
                        TextMeshProUGUI textElement = obj.GetComponent<TextMeshProUGUI>();
                        textElement.text = entity.data?.id.ToString() ?? "???";
                        activeTexts.Add(obj);
                        inactiveTexts.Remove(obj);
                        return true;
                    }
                }
                return false;
            }

            public static Vector3 defaultTextPosition = new Vector3(0f, 4f, 0f);
            public void CreateTextAndAssign(Entity entity)
            {
                GameObject obj = new GameObject("ID Tooltip");
                obj.transform.SetParent(entity.display.GetCanvas().transform, false);
                TextMeshProUGUI textElement = obj.AddComponent<TextMeshProUGUI>();
                textElement.fontSize = 0.5f;
                textElement.horizontalAlignment = HorizontalAlignmentOptions.Center;
                textElement.text = entity.data?.id.ToString() ?? "???";
                textElement.outlineColor = Color.black;
                textElement.outlineWidth = 0.1f;
                obj.GetComponent<RectTransform>().sizeDelta = new Vector2(4f, 1f);
                activeTexts.Add(obj);
            }

            public string EncodePositions(OtherCardViewer[] ocvs)
            {
                List<ulong> positions = new List<ulong>();
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        if (ocvs[j].Count > i && ocvs[j][i] != null)
                        {
                            positions.Add(ocvs[j].Find(ocvs[j][i]).Item2);
                        }
                    }
                }

                return HandlerSystem.ConcatMessage(true, positions.InRandomOrder().Select(x => x.ToString()).ToArray());
            }

            public override IEnumerator GetArgOptions(string currentArgs)
            {
                string[] options = new string[] { "on", "off" };
                predictedArgs = options;
                yield break;
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

        /*
        public class CommandMultSac : Command
        {
            public override string id => "multSac";

            public override string format => "multSac";

            public override string desc => "Places a death marker";

            public override bool IsRoutine => false;

            public static ParticleSystem.Particle[] particles = new ParticleSystem.Particle[10];
            public static int child = 4;
            public static float duration = 0.5f;
            public override void Run(string args)
            {
                if ((bool)ConsoleMod.hover)
                {
                    MarkerManager marks = GameObject.FindObjectOfType<MarkerManager>();
                    if (marks != null)
                    {
                        foreach(OtherCardViewer ocv in HandlerBattle.instance.enemyLanes)
                        {
                            foreach (Entity entity in ocv.entities)
                            {
                                if (entity?.data == ConsoleMod.hover)
                                {
                                    marks.CreateDeathMarker("ENEMY", entity.gameObject.transform.position);
                                    return;
                                }
                            }
                        }

                        foreach (OtherCardViewer ocv in HandlerBattle.instance.playerLanes)
                        {
                            foreach (Entity entity in ocv.entities)
                            {
                                if (entity?.data == ConsoleMod.hover)
                                {
                                    marks.CreateDeathMarker("PLAYER", entity.gameObject.transform.position);
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            public override IEnumerator GetArgOptions(string currentArgs)
            {
                yield break;
            }
        }
        */


    }

    /*
    public class CommandMultEMOTE : Command
    {
        public override string id => "multEmote";

        public override string format => "multEmote <type>";

        public override string desc => "Places a VFX";

        public override bool IsRoutine => false;

        public static ParticleSystem.Particle[] particles = new ParticleSystem.Particle[10];
        public static int child = 4;
        public static float duration = 0.5f;
        public override void Run(string args)
        {
            if ((bool)ConsoleMod.hover)
            {
                MarkerManager marks = GameObject.FindObjectOfType<MarkerManager>();
                if (marks != null)
                {
                    foreach (OtherCardViewer ocv in HandlerBattle.instance.enemyLanes)
                    {
                        foreach (Entity entity in ocv.entities)
                        {
                            if (entity?.data == ConsoleMod.hover)
                            {
                                if (!CreateEffect(args.Trim(), entity))
                                {
                                    Fail($"No apply effect found with name \"{args}\"");
                                }
                                return;
                            }
                        }
                    }

                    foreach (OtherCardViewer ocv in HandlerBattle.instance.playerLanes)
                    {
                        foreach (Entity entity in ocv.entities)
                        {
                            if (entity?.data == ConsoleMod.hover)
                            {
                                if (!CreateEffect(args.Trim(), entity))
                                {
                                    Fail($"No apply effect found with name \"{args}\"");
                                }
                                return;
                            }
                        }
                    }
                }
            }
        }
    

        private bool CreateEffect(string type, Entity entity)
        {
            VfxStatusSystem system = GameObject.FindObjectOfType<VfxStatusSystem>();
            if (system == null || !system.profileLookup.ContainsKey(type))
            {
                return false;
            }

            system.CreateEffect(system.profileLookup[type].applyEffectPrefab, entity.transform.position, entity.transform.lossyScale);
            return true;
        }

        public override IEnumerator GetArgOptions(string currentArgs)
        {
            VfxStatusSystem system = GameObject.FindObjectOfType<VfxStatusSystem>();
            if (system != null)
            {
                predictedArgs = system.profileLookup.Keys.ToArray();
                yield break;
            }
        }
    }
    */
}
