using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EyeEyeEye
{
    public class EyeEyeEye : WildfrostMod
    {
        //==============================================================================
        //Example Code
        public static void Eyes()
        {
            //WARNING: The EyeData will NOT be removed upon unload. Call Eyes() underneath CreateModAssets() in the Load method. 
            List<EyeData> list = new List<EyeData>()
            {
                //Put the output code here!
            };

            AddressableLoader.AddRangeToGroup("EyeData", list);
        }

        public static EyeData Eyes(string cardName, params (float, float, float, float, float)[] data)
        {
            EyeData eyeData = ScriptableObject.CreateInstance<EyeData>();
            eyeData.cardData = cardName;
            eyeData.name = eyeData.cardData + "_EyeData";
            eyeData.eyes = data.Select((e) => new EyeData.Eye
            {
                position = new Vector2(e.Item1, e.Item2),
                scale = new Vector2(e.Item3, e.Item4),
                rotation = e.Item5
            }).ToArray();

            return eyeData;
        }
        //End of Example Code
        //===============================================================================


        public override string GUID => "mhcdc9.wildfrost.eyeeyeeye";

        public override string[] Depends => new string[0];

        public override string Title => "Eye Command";

        public static Dictionary<string, List<(float, float, float, float, float)>> eyeData = new Dictionary<string, List<(float, float, float, float, float)>>();

        public override string Description => "Adds multiple commands that lets you put an eye on a target. Useful for modders seeking to make their cards have eye data. \n\n" +
            "[h3] Commands [/h3] \n\n" +
            "eye here [/code]: places the eye where your cursor is. Also opens the Unity Explorer to do small tweaks. \n\n" + 
            "eye <posX> <posY> <scaleX> <scaleY> <rotation> [/code]: places the eye on the hovered or inspected card. Also opens the Unity Explorer to do small tweaks. \n\n" +
            "eyereset [/code]: removes all eyes made by the prior commands. \n\n" +
            "recordeyes [/code]: records all eyes mode onto the hovered card, then removes all eyes. \n\n" +
            "outputeyes <fileName> [/code]: outputs all eyeData in a file named <fileName>.txt \n\n\n" +
            "[h3]Ideal Workflow:[/h3] \n" +
            "[olist]" +
            " [*] Turn on the mod and open the journal." +
            " [*] Select a card you want to add EyeData for. The card should be huge (see 1st picture)." +
            " [*] Type \"~\" to open the command console. Align your cursor to the card's eye and type \"eye here\" or the longer variant." +
            " [*] Move windows around until you have it like the 2nd picture. Edit the marked values manually or via the sliders/buttons to the right." +
            " [*] Once you're happy with that eye, repeat steps 3-4 for each other eye on the card." +
            " [*] Type \"recordeyes\" to record the data and erase all current eyes." +
            " [*] Repeat steps 2-6 for all cards you want eyes for." +
            " [*] Use the \"outputeyes\" command to create a txt file with the formatted data. (To find it, click the folder button to the left of this mod's load bell)." +
            " [*] Use the example code (or equivalent) and paste the formatted data into it." +
            " [*] Eyes for all!" +
            "[/olist]\n[hr][/hr]\n\n" + 
            "The developer can be reached on Steam or Discord (@Michael C) if there are questions.";

        public static EyeEyeEye instance;

        public EyeEyeEye(string modDirectory):base(modDirectory)
        {
            instance = this;
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



        [HarmonyPatch(typeof(JournalCardDisplay), "UpdateCard")]
        class PatchEnlargeCard
        {
            static IEnumerator Postfix(IEnumerator result, Card ___current)
            {
                yield return result;
                ___current.transform.localScale = 3f * Vector3.one;
                Transform transform = ___current.transform.parent.parent.GetComponentsInChildren<Transform>().FirstOrDefault((t) => t.name == "Details");
                transform?.gameObject.SetActive(false);
            }
        }
    }
}
