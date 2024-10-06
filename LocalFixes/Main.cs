using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace LocalFixes
{
    /*
     * Instructions: 
     * (1) Place the dll as if its one of your own mods. 
     * (2) Makes sure its folder is the alphabetically the first one (I named the folder "!Test" to ensure this).
     * (3) Run the game and then turn on this mod.
     * (4) Look at the debug and check the list of GUIDs printed. Make sure none of your local mods are in that list.
     * (5) Turn the mod off and close out of the game.
     * (6) Check the folder where you put the dll. Open the new [log.txt] file and make sure it is nonempty.
     * (7) Wait a week or two.
     * (8) Open [log.txt] and report the results back to me (@Michael C)
     */

    public class LocalFixes : WildfrostMod
    {
        public override string GUID => "zzz.localfixes";

        public override string[] Depends => new string[0];

        public override string Title => "Local Fixes";

        public override string Description => "This mod stalls the game while waiting for the workshop mods to be found.";

        public static List<string> referenced = new List<string>();
        public static int timePassed = 0;
        public static int timeout = 40;
        public LocalFixes(string modDirectory) : base(modDirectory) 
        {
            while (Bootstrap.Mods.Count == 0 && timePassed < timeout)
            {
                Thread.Sleep(250);
                timePassed++;
                Debug.Log($"[LocalFix] {timePassed} seconds paused");
            }

            if (timePassed > 0)
            {
                string s = (timePassed == timeout) ? " (Timed Out)" : "";
                AppendFile($"{DateTime.Now} | {timePassed*250} ms {s}");
            }

            foreach (WildfrostMod mod in Bootstrap.Mods)
            {
                referenced.Add(mod.GUID);
            }
        }

        public override void Load()
        {
            string fileName = Path.Combine(ModDirectory, "log.txt");
            if (!System.IO.File.Exists(fileName))
            {
                CreateFile(fileName);
            }
            Debug.Log($"[LocalFix] {timePassed} seconds passed");
            foreach (string s in referenced)
            {
                Debug.Log($"[LocalFix] {s}");
            }
            base.Load();
        }

        public void AppendFile(string s)
        {
            string fileName = Path.Combine(ModDirectory, "log.txt");
            if (!System.IO.File.Exists(fileName))
            {
                CreateFile(fileName);
            }
            IEnumerable<string> enumerable = System.IO.File.ReadLines(fileName);
            StringBuilder sb = new StringBuilder();
            foreach (string line in enumerable)
            {
                sb.AppendLine(line);
            }
            sb.AppendLine(s);
            File.WriteAllText(fileName, sb.ToString());
        }

        public void CreateFile(string fileName)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Anytime the mod has to stall, it will be logged here! Please check back in a couple of days.");
            System.IO.File.WriteAllText(fileName, stringBuilder.ToString());
        }
    }
}
