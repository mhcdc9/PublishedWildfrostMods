using Deadpan.Enums.Engine.Components.Modding;
using Detours.Misc;
using Mono.Math;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst.Intrinsics;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

namespace Detours.Examples
{
    //Mini-Events that do not require a custom class for.
    public class IllusionOfChoice : DetourBasic
    {
        ScriptRemoveRandomCards script1;
        ScriptRandomizeDeck script2;
        public IllusionOfChoice(string name, WildfrostMod mod) : base(name, mod)
        {

            script1 = ScriptableObject.CreateInstance<ScriptRemoveRandomCards>();
            script1.countRange = new Vector2Int(1, 1);
            script1.cardTypes = new CardType[] { mod.Get<CardType>("Item") };

            script2 = ScriptableObject.CreateInstance<ScriptRandomizeDeck>();
            script2.cardsToRemove = Vector2Int.zero;

            SetTitle("The Illusion of Choice");
            SetFrame(frame: START, sprite: mod.ImagePath("choice.png").ToSprite(),
            text: "You find yourself outside of the storm and inside a... labyrinth? " +
            "There are two paths in front of you: the left path is marked \"Wild\" while the right is marked \"Frost\". " +
            "How should you proceed?",
            choices: new FrameChoice[]
            {
                new FrameChoice("WILD", "Take the \"Wild\" path. [+1 Wild]", END, mod),
                new FrameChoice("FROST", "Take the \"Frost\" path. [+1 Frost]", "FROST", mod),
                new FrameChoice("BORING", "Retrace your steps. [+1 Boring]", "BORING", mod),
            });
            SetFrame(frame: "BORING", sprite: DetourMain.instance.TryGet<CardData>("SpikeWall").mainSprite,
            text: "You turn around to find that there is no path behind you. Huh...",
            choices: new FrameChoice[]
            {
                new FrameChoice("BACKTRACK", "Turn back around.", START, mod)
            });
            SetFrame(frame: "FROST", sprite: DetourMain.instance.TryGet<CardData>("SpikeWall").mainSprite,
            text: "You follow the right path until you reach a dead end.",
            choices: new FrameChoice[]
            {
                new FrameChoice("BACKTRACK2", "Walk back to the fork.", START, mod)
            });
        }

        public override bool HasChoiceSelectedRoutine => true;
        public override IEnumerator ChoiceSelectedRoutine()
        {
            string choice = selectedChoice.name;
            switch (choice)
            {
                case "BACKTRACK":
                    yield return script1.Run();
                    break;
                case "BACKTRACK2":
                    yield return script2.Run();
                    break;
            }
        }
    }  

    public class CraneMachine : DetourBasic
    {
        public string upgradeName = "Charm";
        public CraneMachine(string name, WildfrostMod mod) : base(name, mod)
        {
            TargetConstraintHasTrait pull = ScriptableObject.CreateInstance<TargetConstraintHasTrait>();
            pull.trait = DetourMain.instance.TryGet<TraitData>("Pull");
            ChoiceCondition money = new ChoiceConditionCustom(mod, "At least 10 bling", "Need at least 10 bling.",
                () => References.PlayerData.inventory.gold.Value >= 10);

            ChoiceCondition yank = new ChoiceConditionInDeck(mod, "A card with yank", "Need a card with Yank.",
                new TargetConstraint[] { pull });

            FrameChoice[] choices = new FrameChoice[]
            {
                new FrameChoice("Attempt", "Try your best. [-10 Bling]", "TOO BAD", mod)
                .AddConditions(money),
                new FrameChoice("Yank", "Use your own tools. [-10 Bling]", "SUCCESS", mod)
                .AddConditions(money, yank).SetColor(UI.Cyan),
                new FrameChoice("Leave", "[Leave]", END, mod)
            };

            SetTitle("Skill Crane");
            SetFrame(frame: START, sprite: mod.ImagePath("crane.png").ToSprite(),
            text: "At the end of the line stands a unusual structure named \"Skill Crane\". Tantalizing treasures rest behind a plane of glass. " +
            "At the very top lies a fairly worn-out mechanical arm. " +
            "For a price, the contraption can be used to fish for treasure, " +
            "but the arm's condition makes it a trial of luck more than \"skill\". " +
            "You know the trade is not in your favor, yet the Skill Crane beckons you...",
            choices: choices);
            SetFrame(frame: "TOO BAD",
            text: "Just as victory was all but certain, your prize escapes the arm's clutches and returns to the pile. Maybe just one more try...",
            choices: choices);
            SetFrame(frame: "SUCCESS",
            text: "The claw fully retracts and then hands you the prize. Hmm, The <{upgradeName}> looked shinier behind the glass...",
            choices: new FrameChoice[] { choices[2] });
            SetFrame(frame: "SUCCESS|YANK", sprite: DetourMain.instance.TryGet<CardData>("Hooker").mainSprite,
            text: "Replacing the arm with your own tool was effective! It took no effort obtaining your prize. Hmm, The <{upgradeName}> looked shinier behind the glass...");
        }

        public override void Setup(CampaignNode node)
        {
            CharacterRewards rewards = References.Player.GetComponent<CharacterRewards>();
            CardUpgradeData upgrade = rewards.Pull<CardUpgradeData>(null, "Charms", 1, true, (DataFile d) => (d as CardUpgradeData).tier <= 2)[0];
            SetData(node, "reward", upgrade.name);
            base.Setup(node);
        }

        public override bool MissingData(CampaignNode node)
        {
            if (TryGetData<string>(node, "reward", out string value))
            {
                return DetourMain.instance.Get<CardUpgradeData>(value ?? "") == null;
            }
            return true;
        }

        public override bool RunChoiceSelected()
        {
            string choice = selectedChoice.name;
            switch (choice)
            {
                case "Attempt":
                    References.Player.SpendGold(10);
                    if (Dead.Random.Range(0f, 1f) < 0.2f)
                    {
                        nextFrame = "SUCCESS";
                    }
                    break;
                case "Yank":
                    References.Player.SpendGold(10);
                    subFrame = "LUCKY";
                    break;
            }
            return false;
        }

        public override bool RunPreFrame()
        {
            if (nextFrame == "SUCCESS" || nextFrame == "YANKED")
            {
                if (TryGetData<string>(node, "reward", out string value))
                {
                    CardUpgradeData upgrade = DetourMain.instance.Get<CardUpgradeData>(value);
                    References.PlayerData.inventory.upgrades.Add(upgrade);
                    upgradeName = upgrade?.title ?? "<nothing>";
                }
            }
            return false;
        }
    }

    public class AlienSpiders : DetourBasic
    {
        CardData crewmate;
        public string crewName = "";
        CardData blankMask;
        public string blankMaskName = "";
        public string card = "";
        public AlienSpiders(string name, WildfrostMod mod) : base(name, mod)
        {
            TargetConstraintIsCardType isClunker = ScriptableObject.CreateInstance<TargetConstraintIsCardType>();
            isClunker.allowedTypes = new CardType[] { mod.Get<CardType>("Clunker") };
            TargetConstraintIsCardType isCompanion = ScriptableObject.CreateInstance<TargetConstraintIsCardType>();
            isCompanion.allowedTypes = new CardType[] { mod.Get<CardType>("Friendly") };
            TargetConstraintHasEffectBasedOn shroom = ScriptableObject.CreateInstance<TargetConstraintHasEffectBasedOn>();
            shroom.basedOnStatusType = "shroom";
            TargetConstraintAttackMoreThan largeAttack = ScriptableObject.CreateInstance<TargetConstraintAttackMoreThan>();
            largeAttack.value = 4;
            TargetConstraintAttackMoreThan smallAttack = ScriptableObject.CreateInstance<TargetConstraintAttackMoreThan>();
            smallAttack.value = 4;
            smallAttack.not = true;
            TargetConstraintDoesAttack doesAttack = ScriptableObject.CreateInstance<TargetConstraintDoesAttack>();

            ChoiceCondition mimik = new ChoiceConditionInDeck(mod, "Anti-Personnel Drone", "",
                new TargetConstraint[]
                {
                    isClunker, doesAttack, smallAttack, 
                });

            ChoiceCondition icgm = new ChoiceConditionInDeck(mod, "Boarding Drone", "",
                new TargetConstraint[]
                {
                    isClunker, largeAttack,
                });
            ChoiceCondition hasShroom = new ChoiceConditionInDeck(mod, "Bio-Beam", "",
                new TargetConstraint[]
                {
                    shroom
                });
            ChoiceCondition sacrifice = new ChoiceConditionInDeck(mod, "Teammate", "",
                new TargetConstraint[]
                {
                    isCompanion
                });

            FrameChoice[] end = new FrameChoice[]
                {
                    new FrameChoice("spiders_leave", "[Leave]", END, mod)
                };

            SetTitle("Giant Ice Spiders");
            SetFrame(START, sprite: mod.ImagePath("spider.png").ToSprite(),
                text: "You find a number of travelers fleeing from a small cave. You wave at them, asking what's wrong: \"Help! We're being overrun by some sort of giant ice spiders!\"",
                choices: new FrameChoice[]
                {
                    new FrameChoice("spiders_nojoke", "Send the crew to help! Giant ice spiders are no joke.","LOSS",mod)
                    .AddConditions(sacrifice),
                    new FrameChoice("spiders_leave1", "Leave them alone.","TOO RISKY",mod),
                    new FrameChoice("spiders_weakbot","Send your [{c0}] in to help.","SUCCESS",mod)
                    .AddConditions(mimik).SetVisibleIfDisabled(false).SetColor(UI.Cyan),
                    new FrameChoice("spiders_strongbot", "Launch a [{c0}] into the tunnel.","SUCCESS",mod)
                    .AddConditions(icgm).SetVisibleIfDisabled(false).SetColor(UI.Cyan),
                    new FrameChoice("spiders_shroom", "Use the power of [shroom] to pick off the spiders.","SUCCESS",mod)
                    .AddConditions(hasShroom).SetVisibleIfDisabled(false).SetColor(UI.Cyan),
                });
            SetFrame("LOSS", sprite: mod.ImagePath("spider.png").ToSprite(),
                text: "Your party enters the cave, cautiously moving between tunnels. Suddenly a man-sized arachnid bursts from a hole in the ceiling, followed by countless more. You fight your way back to the entrance and are forced to leave before accounting for all party members. Not everybody made it back.",
                choices: new FrameChoice[]
                {
                    new FrameChoice("spiders_loss", "You lost [{crewName}]",END,mod)
                });
            SetFrame("CLONE",
                text: "As you return to your supplies, you find <{crewName}> is alive and well! That other <{crewName}> must have been a shade made by your <{blankMaskName}>.",
                choices: new FrameChoice[]
                {
                    new FrameChoice("spiders_clone", "[{crewName}] is alive, but [{blankMaskName}] is lost",END,mod)
                });
            SetFrame("TOO RISKY",
                text: "You can't risk fighting some unknown creatures on every backwater spot you come across. You prepare to leave.",
                choices: end);
            SetFrame("SUCCESS|WEAK",
                text: "You head to the mouth of the cave and release the <{card}> through the airlock. Within a short time the majority of the creatures are dead, with only a little collateral damage. They express their most sincere gratitude.");
            SetFrame("SUCCESS|STRONG",
                text: "You launch <{card}> and it crashes through their hull, leaving a huge breach. You watch as <{card}> tears through the creatures while debris and dead bodies fly out of the cave. The survivors are less than effusive when they thank you, and offer only a meager payment. Maybe it's a good time to leave...");
            SetFrame("SUCCESS|SHROOM", 
            text: "You instruct them to hold their breath, and you are able to kill the creatures without damaging the travelers. \"The monsters just started taking damage at the end of each turn. What a terrifying weapon... Here, take this for your help, friend.\"");
            SetFrame("SUCCESS",
                text: "Your party slowly creeps up on a cluster of the creatures from behind. Without warning, the giant arachnids turn and charge. However, your party stays in control and before long you've beaten them back. Returning back, the travelers are thrilled at your success and offer you a reward.",
                choices: end);
        }

        public override IEnumerator Run(CampaignNode node, string startFrame = "START")
        {
            //This could be checked a different way, but this way prevents deck manip strats :)
            IEnumerable<CardData> companions = References.PlayerData.inventory.deck.Where((c) => c.cardType.name == "Friendly");
            if (companions.Any())
            {
                crewmate = companions.InRandomOrder().First();
                crewName = crewmate.title;
            }
            
            return base.Run(node,startFrame);
        }

        public override bool RunChoiceSelected()
        {
            string choiceName = selectedChoice.name;
            switch(choiceName)
            {
                case "spiders_weakbot":
                    subFrame = "WEAK";
                    card = selectedChoice.GetContext(0);
                    break;
                case "spiders_strongbot":
                    subFrame = "STRONG";
                    card = selectedChoice.GetContext(0);
                    break;
                case "spiders_shroom":
                    subFrame = "SHROOM";
                    break;
                case "spiders_nojoke":
                    if (Dead.Random.Range(0f,1f) < 0.5f)
                    {
                        nextFrame = "SUCCESS";
                    }
                    break;
            }
            if (currentFrame == "LOSS" && blankMask != null)
            {
                nextFrame = "CLONE";
            }
            return false;
        }

        public override bool RunPreFrame()
        {
            switch(nextFrame)
            {
                case "LOSS":
                    blankMask = References.PlayerData.inventory.deck.Where((c) => c.name == "Dittostone").FirstOrDefault();
                    blankMaskName = blankMask?.title;
                    References.PlayerData.inventory.deck.Remove(blankMask ?? crewmate);
                    break;
                case "SUCCESS":
                    int reward = 60;
                    if (subFrame == "WEAK")
                        reward = 40;
                    if (subFrame == "STRONG")
                        reward = 20;
                    References.Player.GainGold(reward);
                    break;
            }
            return false;

        }
    }

    public class CardTrader : DetourBasic
    {
        public string stolenCard;
        public string selectedCard;
        public CardTrader(string name, WildfrostMod mod) : base(name, mod)
        {
            FrameChoice[] leaveArray = new FrameChoice[]
            {
                new FrameChoice("Leave", "[Leave]", END, mod)
            };

            SetTitle("Item Trader");
            SetFrame(START, sprite: DetourMain.instance.TryGet<CardData>("Spoof").mainSprite,
                text: "You come across a mysterious man who wants to perform a bizarre trade where we won't know what we are offering: \n\n\"One for you, one for me. What could be more fair?\"",
                choices: new FrameChoice[]
                {
                    new FrameChoiceSelectCard("trader_accept","Accept and look at his wares.","TRADE", mod, AvailableCards),
                    new FrameChoice("trade_refuse", "Politely refuse.", "DECLINE", mod)
                });
            SetFrame("TRADE",
                text: "\"Hope that <{selectedCard}> serves you well. I know that your <{stolenCard}> will for me!\"\n\nThe mysterious man disappears from sight. The entire ordeal leaves you slightly disoriented.",
                choices: new FrameChoice[]
                {
                new FrameChoice("getLost", "[Continue onwards]", SKIP, mod)
                });
            SetFrame("DECLINE",
                text: "\"You're no fun. Guess I'll take my leave then.\"\n\nA the snap of his fingers, a gust of wind whisks the mysterious man from sight.",
                choices: leaveArray);
        }

        public override IEnumerator Run(CampaignNode node, string startFrame)
        {
            cards = AddressableLoader.GetGroup<CardData>("CardData").Where((c) => c.cardType.name == "Item" || c.cardType.name == "Item").InRandomOrder().Take(15).Select((c) => c.name).ToArray();
            return base.Run(node, startFrame);
        }

        public override bool RunChoiceSelected()
        {
            if (selectedChoice is FrameChoiceSelectCard e)
            {
                CardData offering = References.PlayerData.inventory.deck.Where((c) => (c.cardType.name == "Item" || c.cardType.name == "Clunker") && c.name != "LuminSealant" && c.name != "BrokenVase").RandomItems(1)[0];
                References.PlayerData.inventory.deck.Remove(offering);
                References.PlayerData.inventory.deck.Add(e.selected);
                stolenCard = offering.title;
                selectedCard = e.selected.title;
            }
            return false;
        }

        public string[] cards = new string[] { "Vimifier", "Peppereaper", "ZapOrb", "Peppermaton", "LuminSealant", "Junk", "Junk", "Junk", "Junk", "Junk", "Deadweight", "Deadweight", "Deadweight", "Deadweight", "Deadweight" };
        public CardData[] AvailableCards()
        {
            return cards.Select((s) => DetourMain.instance.TryGet<CardData>(s).Clone()).ToArray();
        }
    }

    public class HiLowGame : DetourBasic
    {
        public static int edition;
        public static string leaderName = "Rayhorn Berrywood";
        public static int amount;
        public HiLowGame(string name, WildfrostMod mod) : base(name, mod)
        {
            edition = Dead.Random.Range(101, 1000);
            FrameChoice[] leave = new FrameChoice[] {
            new FrameChoice("spiders_leave", "[Leave]", END, mod)
            };


            SetTitle("Guessing Game");
            SetFrame(START, sprite: DetourMain.instance.ImagePath("quiz.png").ToSprite(),
                text: "\"Welcome to the {edition}-th edition of <color=#000000>Quizley's Quiz Show</color>! I am your wonderful host Quizley, and today we have a special visitor today. You are in the presence of the Wildfrost Warrior, the Snowdwell Saviour, the Pride of the Tribe... Give a round of applause to...\"\n\n\"(Quick: What's your name?)\"",
                choices: new FrameChoice[]
                {
                    new FrameChoiceInputField("quiz_name", "[Write your name]", "SNOW", mod)
                });
            SetFrame("SNOW",
                text: "\n<{leaderName}> the Sunbringer! Thank you for joining us today, <{leaderName}>. The game will consist of a single question:\n\nHow much snow have you applied this run? If can answer right, you will be set for life! So, what is your guess?\"",
                choices: new FrameChoice[]
                {
                    new FrameChoiceInputField("quiz_ask", "[Input a number]", "SUCCESS", mod)
                });
            SetFrame("TOO LOW",
                text: "\"BZZZT! WRONG! For a bold adventurer such as yourself, I'm surprised you would be so humble. However, modesty won't win you favors in the game because your guess was <color=#000000>too low</color>. Come back again once you are prepared to reach for the stars.\n\nSee you next time!\"",
                choices: leave);
            SetFrame("TOO HIGH",
                text: "\"BZZZT! WRONG! Why look towards the stars when you can shoot the moon! I admire the confidence, but your guess was <color=#000000>too high</color>. We will talk again once you ground your expectations a bit.\n\nSee you next time!\"",
                choices: leave);
            SetFrame("NAN",
                text: "\"That was your guess...\nListen, I try to give hints on where you need to improve, but you have to at least give me something to work with! I'm gonna give you some time to collect your thoughts, and then we will try this again. See you next time!\"",
                choices: leave);
            SetFrame("SUCCESS",
                text: "\"BINGO! Right on the mark! You applied exactly <{amount}> snow this run! Those enemies (or maybe even your allies) don't know what hit them. Congratulations!\"",
                choices: leave);
        }

        public override bool RunPreFrame()
        {
            if (nextFrame == START)
            {
                edition++;
                leaderName = References.PlayerData.inventory.deck[0].title;
                amount = StatsSystem.instance.stats.Get("statusesApplied", "snow", 0);
            }
            if (nextFrame == "SUCCESS")
            {
                References.Player.GainGold(1000);
            }
            return false;
        }

        public override bool RunChoiceSelected()
        {
            if (selectedChoice is FrameChoiceInputField e)
            {
                if (e.name == "quiz_name")
                {
                    leaderName = e.saved.Trim();
                    return false;
                }
                string s = e.saved.Trim();
                if (int.TryParse(s, out var i))
                {
                    if (i < amount)
                    {
                        nextFrame = "TOO LOW";
                    }
                    if (i > amount)
                    {
                        nextFrame = "TOO HIGH";
                    }
                }
                else
                {
                    nextFrame = "NAN";
                }
            }
            return false;
        }
    }
}
