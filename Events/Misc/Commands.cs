using Deadpan.Enums.Engine.Components.Modding;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Console;

namespace Detours.Misc
{
    internal static class Commands
    {
        internal static void CheckAnotherConsoleMod(WildfrostMod mod)
        {
            if (mod.GUID == "hope.wildfrost.console")
            {
                CoroutineManager.Start(AddCustomCommands(mod));
            }
        }

        internal static void FindAnotherConsoleMod()
        {
            List<WildfrostMod> mods = Bootstrap.Mods.ToList();
            foreach (WildfrostMod mod in mods)
            {
                if (mod.GUID == "hope.wildfrost.console" && mod.HasLoaded)
                {
                    CoroutineManager.Start(AddCustomCommands(mod));
                }
            }
        }

        private static IEnumerator AddCustomCommands(WildfrostMod mod)
        {
            yield return new WaitUntil(() => SceneManager.Loaded.ContainsKey("MainMenu"));
            if (commands != null)
            {
                commands.Add(new CommandSTART());
                commands.Add(new CommandTOFRAME());
            }
        }

        public class CommandSTART : Command
        {

            public static List<Transform> transforms = new List<Transform>();
            public override string id => "detour start";

            public override string format => "detour start <name>";

            public override string desc => "Start a detour with desired name";

            public override bool IsRoutine => true;
            public override IEnumerator Routine(string args)
            {
                if (!SceneManager.IsLoaded("MapNew"))
                {
                    Fail("Cannot be used here. Detours happen on the map.");
                    yield break;
                }

                string name = args.ToLower().Trim();

                string trueName = DetourSystem.allDetours.Keys.FirstOrDefault((n) => n.ToLower().Trim() == name);

                if (trueName.IsNullOrEmpty())
                {
                    Fail("Invalid Name");
                    yield break;
                }
                CampaignNode node = Campaign.FindCharacterNode(References.Player);
                node.data.Remove(DetourSystem.detourTitle);
                node.data.Add(DetourSystem.detourTitle, trueName);
                CharacterRewards rewards = References.Player.GetComponent<CharacterRewards>();
                if (rewards.poolLookup.Count == 0)
                {
                    rewards.Populate(References.PlayerData.classData);
                }
                DetourSystem.allDetours[trueName].Setup(node);
                yield return DetourSystem.StartDetour(node);

            }

            public override IEnumerator GetArgOptions(string currentArgs)
            {
                string[] args = currentArgs.Split(new Char[] { ' ' }, options: StringSplitOptions.None);
                int length = args.Length;

                predictedArgs = DetourSystem.allDetours.Keys.Where((s) => s.ToLower().Contains(currentArgs.ToLower())).ToArray();
                yield break;
            }
        }

        public class CommandTOFRAME : Command
        {

            public static List<Transform> transforms = new List<Transform>();
            public override string id => "detour frame";

            public override string format => "detour frame <name>";

            public override string desc => "While in a detour, jump to a specific frame (Warning: possibly buggy)";
            public override void Run(string args)
            {
                if (!SceneManager.IsLoaded("MapNew") || !DetourSystem.active)
                {
                    Fail("Cannot be used here. A Detour must be active.");
                    return;
                }

                Detour detour = DetourHolder.current;

                if (detour is DetourBasic d)
                {
                    d.PromptUpdate(new FrameChoice("", "", args, DetourMain.instance), args);
                }
                else
                {
                    Fail("Detour does not inherit from DetourBasic. Create your own command.");
                }

            }

            public override IEnumerator GetArgOptions(string currentArgs)
            {
                string[] args = currentArgs.Split(new Char[] { ' ' }, options: StringSplitOptions.None);
                int length = args.Length;

                //predictedArgs = DetourSystem.allDetours.Keys.Select((s) => s).ToArray();
                yield break;
            }
        }
    }
}
