using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EyeEyeEye
{
    public class EyeEyeEye : WildfrostMod
    {
        public override string GUID => "mhcdc9.wildfrost.eyeeyeeye";

        public override string[] Depends => new string[0];

        public override string Title => "Eye Command";

        public override string Description => "Adds a single command that lets you put an eye on a target. Useful for modders seeking to make their cards have eyeData (coding EyeData not included). \n" +
            "The commands are the following: \n\n" +
            "eye here: places the eye where your cursor is. Also opens the Unity Explorer to do small tweaks. \n\n" + 
            "eye <posX> <posY> <scaleX> <scaleY> <rotation>: places the eye on the hovered or inspected card. Also opens the Unity Explorer to do small tweaks. \n\n" +
            "eyereset: removes all eyes made by the prior commands. \n\n\n" +
            "The developer can be reached on Steam or Discord (@Michael C) if there are questions.";

        public EyeEyeEye(string modDirectory):base(modDirectory)
        {

        }

        public override void Load()
        {
            FindAnotherConsoleMod();
            Events.OnModLoaded += CheckAnotherConsoleMod;
            base.Load();
        }

        private void CheckAnotherConsoleMod(WildfrostMod mod)
        {
            if (mod.GUID == "hope.wildfrost.console")
            {
                CoroutineManager.Start(Commands.AddCustomCommands(mod));
            }
        }

        private void FindAnotherConsoleMod()
        {
            List<WildfrostMod> mods = Bootstrap.Mods.ToList();
            foreach (WildfrostMod mod in mods)
            {
                if (mod.GUID == "hope.wildfrost.console" && mod.HasLoaded)
                {
                    CoroutineManager.Start(Commands.AddCustomCommands(mod));
                }
            }
        }
    }
}
