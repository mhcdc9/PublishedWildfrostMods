using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detours.Examples
{
    internal class MokoStoryline : Storyline
    {
        static int currentIndex = 1;

        public MokoStoryline(WildfrostMod mod, string name, bool active = true, int copies = 1) : base(mod, name, active, copies)
        {
            this.Add(new MokoDojo("Moko Dojo", mod));
        }

        public override void Setup()
        {
            currentIndex = 1;
            SetData("index", currentIndex);
        }
        public override bool CanActivate(CampaignNode node)
        {
            if (TryGetData<int>("index", out int value))
            {
                currentIndex = value;
            }
            return (currentIndex <= 4 && node.type.isBattle);
        }
        public override IEnumerator Run(CampaignNode node, string startFrame = "START")
        {
            if (TryGetData<int>("index", out int value))
            {
                currentIndex = value;
            }
            this[0].Setup(node);
            yield return DetourHolder.StartDetour(node, this[0], startFrame);
            SetData("index", currentIndex);
            Campaign.PromptSave();
        }

        internal class MokoDojo : DetourBasic
        {
            public string a;
            public MokoDojo(string name, WildfrostMod mod) : base(name, mod)
            {
                SetTitle("Moko Dojo Part {a}");
                SetFrame(START, sprite: DetourMain.instance.TryGet<CardData>("MonkeyKing").mainSprite,
                    text: "\"Welcome to the Moko Dojo. Are you prepared to steel your mind, body, and spirit to walk the path of the Makoko?\"",
                    choices: new FrameChoice[]
                    {
                        new FrameChoice("moko_begin", "Follow the path of the Makoko","TRAIN", mod),
                        new FrameChoice("moko_decline", "Follow your own path", "DECLINE", mod),
                    });
                SetFrame("TRAIN|GROG", sprite: DetourMain.instance.TryGet<CardData>("Grog").mainSprite,
                    text: "\"The first lesson is the resilience of the body. Although you want the strength defeat your enemies, remember that the enemies have the same wish. Be like the Grog: withstand the enemy's deadliest attack in to prepare to return the strike tenfold.\"",
                    choices: new FrameChoice[]
                    {
                        new FrameChoice("moko_grog", "Reflect on King Moko's teachings [health set to 8]", END, mod),
                    });
                SetFrame("TRAIN|GRUMPS", sprite: DetourMain.instance.TryGet<CardData>("Chunky").mainSprite,
                    text: "\"The second lesson is the clearing of the mind. Though enemies may block your path, you harbor no ill will for them. Let the lesson sink in as you spar with the Grumps a bit.\"",
                    choices: new FrameChoice[]
                    {
                        new FrameChoice("moko_grumps", "Reflect on King Moko's teachings [attack set to 0]", END, mod),
                    });
                SetFrame("TRAIN|MINIMOKO", sprite: DetourMain.instance.TryGet<CardData>("Minimoko").mainSprite,
                    text: "\"The third lesson is the emboldening of the spirit. No matter the enemy, you have the power to rise above them. Be like the Minimoko: though small in initial strength, no foe has survived while underestimating his power.\"",
                    choices: new FrameChoice[]
                    {
                        new FrameChoice("moko_minimoko", "Reflect on King Moko's teachings [gain Minimoko's effect]", END, mod),
                    });
                SetFrame("TRAIN|MAKOKO", sprite: DetourMain.instance.TryGet<CardData>("Makoko").mainSprite,
                    text: "\"You have mastered the mind, body, and spirit. The final lesson is to always keep a balance between all three. When this is achieved, everthing is attainable. This is what it means to be a Makoko.\"",
                    choices: new FrameChoice[]
                    {
                        new FrameChoice("moko_makoko", "Reflect on King Moko's teachings [set counter to 1]", END, mod),
                    });
                SetFrame("DECLINE", 
                    text: "\"So be it. We will meet again: mostly as enemies, but sometimes as friends. In those rare moments, know that the road to scaling damage is always open.\"",
                    choices: new FrameChoice[]
                    {
                        new FrameChoice("moko_leave", "[Leave]", END, mod),
                    });
            }

            public override bool RunPreFrame()
            {
                if (nextFrame == START)
                {
                    a = currentIndex.ToString();
                }
                if (nextFrame == "TRAIN")
                {
                    switch(currentIndex)
                    {
                        case 1:
                            subFrame = "GROG";
                            break;
                        case 2:
                            subFrame = "GRUMPS";
                            break;
                        case 3:
                            subFrame = "MINIMOKO";
                            break;
                        case 4:
                            subFrame = "MAKOKO";
                            break;
                    }
                }
                return false;
            }

            public override bool RunChoiceSelected()
            {
                CardData leader = References.PlayerData.inventory.deck[0];
                if (nextFrame == END)
                {
                    switch(selectedChoice.name)
                    {
                        case "moko_grog":
                            leader.hp = 8;
                            currentIndex++;
                            break;
                        case "moko_grumps":
                            leader.damage = 0;
                            currentIndex++;
                            break;
                        case "moko_minimoko":
                            leader.startWithEffects = leader.startWithEffects.AddItem(DetourMain.instance.SStack("On Turn Apply Attack To Self", 1)).ToArray();
                            currentIndex++;
                            break;
                        case "moko_makoko":
                            leader.counter = 1;
                            currentIndex++;
                            break;

                    }
                }
                return false;
            }
        }
    }
}
