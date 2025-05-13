using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace BattleEditor
{
    public class Main : WildfrostMod
    {
        public static List<WildfrostMod> resetModList = new List<WildfrostMod>();
        public static bool ListeningReset = false;

        public static bool ListeningGobbler = false;

        public static Main instance;
        public Main(string modDirectory) : base(modDirectory)
        {
            instance = this;
        }

        public void ExampleCode()
        {
            new BattleDataEditor(this, "Spare Shells")
            .SetSprite("Spare Shells.png")
            .SetNameRef("The Other Shelled Husks")
            .EnemyDictionary(('C', "Conker"), ('W', "ShellWitch"), ('P', "Pecan"), ('K', "Prickle"), ('B', "Bolgo"))
            .StartWavePoolData(0, "Wave 1: The first of the husks")
            .ConstructWaves(3, 0, "CWW", "CWP")
            .StartWavePoolData(1, "Wave 2: Some more husks")
            .ConstructWaves(3, 1, "PCW", "CPW", "CKW", "KCW")
            .StartWavePoolData(2, "Wave 3: Bolgo is here!")
            .ConstructWaves(3, 9, "BPW", "BCW")
            .AddBattleToLoader().LoadBattle(7, exclusivity: BattleStack.Exclusivity.removeUnmodded)
            .LoadBattle(8, exclusivity: BattleStack.Exclusivity.removeUnmodded)
            .GiveMiniBossesCharms(new string[] { "Bolgo" }, "CardUpgradeAcorn", "CardUpgradeShellOnKill")
            .GiveGobblers();

            BattleDataEditor.DisplayOutput();
        }

        public void ExampleCode2(bool active)
        {
            BattleDataEditor.ToggleBattle(this, Get<BattleData>("Spare Shells"), active);
            BattleDataEditor.DisplayOutput();
        }

        public void ExampleCode3()
        {
            new BattleDataEditor(this, "Spikers")
                .AddCardsToWavePool(0, "WoollyDrek", "Makoko");
        }

        public void Debug1()
        {
            new BattleDataEditor(this, "The Glitchy Debuggers")
                .GiveMiniBossesCharms(new string[] { "Makoko" }, "CardUpgradePlink");

            Get<CardData>("Makoko").cardType = Get<CardType>("Miniboss");
        }

        public override string GUID => "mhcdc9.wildfrost.battle";

        public override string[] Depends => new string[0];

        public override string Title => "Battle Data Editor";

        public override string Description => "[Backend] Provides useful functions to the creation/editing of battles.\r\n\r\n\r\n\r\n\r\n" +
            "For modders who want to use this, here is an example code on how you would make the Shelled Husks fight and make it the first mandatory fight. " +
            "This code should be placed some time after all of the cards are loaded (if you are following the documentation tutorial, this code should run after base.Load):\r\n\r\n" +
            "new BattleDataEditor(this, \"Spare Shells\")\r\n" +
            ".SetSprite(\"Spare Shells.png\")\r\n" +
            ".SetNameRef(\"The Other Shelled Husks\")\r\n" +
            ".EnemyDictionary(('C', \"Conker\"), ('W', \"ShellWitch\"), ('P', \"Pecan\"), ('K', \"Prickle\"), ('B', \"Bolgo\"))\r\n" +
            ".StartWavePoolData(0, \"Wave 1: The first of the husks\")\r\n.ConstructWaves(3, 0, \"CWW\", \"CWP\")\r\n" +
            ".StartWavePoolData(1, \"Wave 2: Some more husks\")\r\n" +
            ".ConstructWaves(3, 1, \"PCW\", \"CPW\", \"CKW\", \"KCW\")\r\n.StartWavePoolData(2, \"Wave 3: Bolgo is here!\")\r\n" +
            ".ConstructWaves(3, 9, \"BPW\", \"BCW\")\r\n.AddBattleToLoader().LoadBattle(0, exclusivity: BattleEditor.BattleStack.Exclusivity.removeUnmodded)\r\n" +
            ".GiveMiniBossesCharms(new string[] { \"Bolgo\" }, \"CardUpgradeAcorn\", \"CardUpgradeShellOnKill\");\r\n\r\n" +
            "The sprite size should be 105x120px IF you are using a pixelDensity of 100 (this can be changed with SetSprite). \r\n" +
            "If you have further questions, reach out to me on the Wildfrost Discord (@Michael C).\r\n\r\n" +
            "Have fun!";

        public readonly static string[,] VanillaBattles =
        {
            { "Pengoons", "Snowbos", "BabyBerries", "Bombers" },
            { "Berries", "Frosters", "Shroomers", "Yeti" },
            { "Frenzy Boss", "Split Boss", "", "" },
            { "Goats", "Husks", "Spice Monkeys", "" },
            { "Drek", "Spikers", "Inkers", "" },
            { "Clunker Boss", "Toadstool Boss", "", "" },
            { "Blockers", "Wildlings", "Mimiks", "" },
            { "Final Boss", "", "", "" },
            {"Final Final Boss", "", "", ""}
        };

        public override void Load()
        {
            //MapNodeBattle/NameRibbon/BattleNameRibbon/
            //MapNodeBattle/Scaler/Animator/
            SpriteSetterCustom ss;

            //Final Boss
            MapNode finalNode = Get<CampaignNodeType>("CampaignNodeFinalBoss").mapNodePrefab;
            ss = finalNode.gameObject.AddComponent<SpriteSetterCustom>();
            finalNode.spriteSetter = ss;
            ss.icon = finalNode.spriteRenderer;
            ss.battleNameString = finalNode.transform.Find("NameRibbon").Find("Text").GetComponent<LocalizeStringEvent>();

            //Final Final Boss
            finalNode = Get<CampaignNodeType>("CampaignNodeFinalFinalBoss").mapNodePrefab;
            ss = finalNode.gameObject.AddComponent<SpriteSetterCustom>();
            finalNode.spriteSetter = ss;
            ss.icon = finalNode.spriteRenderer;
            ss.battleNameString = finalNode.transform.Find("NameRibbon").Find("Text").GetComponent<LocalizeStringEvent>();

            base.Load();
        }

        public override void Unload()
        {
            base.Unload();

            Get<CampaignNodeType>("CampaignNodeFinalBoss")?.mapNodePrefab?.gameObject?.GetComponent<SpriteSetterCustom>()?.Destroy();
            Get<CampaignNodeType>("CampaignNodeFinalFinalBoss")?.mapNodePrefab?.gameObject?.GetComponent<SpriteSetterCustom>()?.Destroy();
        }

        public void CheckReset(WildfrostMod mod)
        {
            if (Main.resetModList.Contains(mod))
            {
                Main.resetModList.Remove(mod);
                //ResetAllTiers();
                RunReset(mod);
            }
        }

        [Obsolete("Do not ever use this method.")]
        public static void ResetTier(int tier, string gameMode = "GameModeNormal")
        {
            GameMode game = AddressableLoader.Get<GameMode>("GameMode", gameMode);
            if (!game)
            {
                Debug.LogWarning("Gamemode does not exist.");
            }
            List<BattleData> data = new List<BattleData>();
            for (int i = 0; i < VanillaBattles.GetLength(1); i++)
            {
                if (VanillaBattles[tier, i] == "") continue;

                BattleData bd = AddressableLoader.Get<BattleData>("BattleData", VanillaBattles[tier, i]);
                if (bd == null)
                {
                    Debug.LogWarning($"[BattleEditor] Could not find a vanilla fight named {VanillaBattles[tier, i]}. Go yell at Michael.");
                }
                else
                {
                    Debug.Log($"[BattleEditor] Found the {VanillaBattles[tier, i]} battle!");
                    data.Add(bd);
                }
            }
            game.populator.tiers[tier].battlePool = data.ToArray();
            Debug.Log($"[BattleEditor] Tier {tier} reset.");
        }

        [Obsolete("Do not ever use this method")]
        public static void ResetAllTiers(string gameMode = "GameModeNormal")
        {
            for (int i = 0; i < VanillaBattles.GetLength(0); i++)
            {
                ResetTier(i, gameMode);
            }
        }

        private void RunReset(WildfrostMod mod)
        {
            string[] keys = BattleDataEditor.changelog.Keys.ToArray();
            foreach(string key in keys)
            {
                if (Get<GameMode>(key) == null)
                {
                    BattleDataEditor.changelog.Remove(key);
                }
                else
                {
                    foreach(BattleStack stack in BattleDataEditor.changelog[key])
                    {
                        stack.Remove(mod);
                    }
                }
            }

            for(int i = BattleDataEditor.edits.Count-1; i>=0; i--)
            {
                if (BattleDataEditor.edits[i].data == null || BattleDataEditor.edits[i].data.ModAdded == mod)
                {
                    BattleDataEditor.edits.RemoveAt(i);
                }
                else if(BattleDataEditor.edits[i].mod == mod)
                {
                    BattleDataEditor.edits[i].Undo();
                    BattleDataEditor.edits.RemoveAt(i);
                }
            }
        }

        public static void CheckGobblerProfiles(WildfrostMod mod)
        {
            foreach (HardModeModifierData hardModeModifierData in References.instance.hardModeModifiers)
            {
                if (hardModeModifierData.name == "9.MoreEnemiesInBossBattles")
                {
                    ScriptAddEnemies script = (ScriptAddEnemies)(hardModeModifierData.modifierData.startScripts[0]);
                    List<ScriptAddEnemies.Profile> list = script.profiles.ToList();
                    for(int i = list.Count-1; i>=0; i--)
                    {
                        ScriptAddEnemies.Profile profile = script.profiles[i];
                        if (profile.battleData == null || profile.battleData.ModAdded == mod)
                        {
                            list.RemoveAt(i);
                        }
                    }
                    script.profiles = list.ToArray();
                }
            }

            Debug.Log("[BattleEditor] Removing excess gobbler profiles");
        }
    }

    public class BattleDataEditor
    {
        private readonly WildfrostMod mod;
        public BattleData bd;
        private bool newBattle = false;
        private List<CardData> enemies = new List<CardData>();
        private Dictionary<char, CardData> dictionary = new Dictionary<char, CardData>();
        private int wavePoolIndex = 0;

        internal static Dictionary<string, List<BattleStack>> changelog = new Dictionary<string, List<BattleStack>>();
        internal static List<Edit> edits = new List<Edit>();
        public BattleDataEditor(WildfrostMod mod)
        {
            this.mod = mod;
        }

        /// <summary>
        /// Starts a battle data editor for the desired battle. 
        /// If the battle name does not exist in the game, a new battle is created.
        /// The optional parameter affects number of Goblings that spawn.
        /// </summary>
        /// <param name="m"> </param> 
        /// <param name="name"></param> 
        /// <param name="goldGivers"></param>
        public BattleDataEditor(WildfrostMod m, string name, int goldGivers = 1) 
        {
            mod = m;
            Create<BattleData>(name, goldGivers);
        }

        public BattleDataEditor Create<T>(string name, int goldGivers = 1) where T : BattleData
        {
            bd = mod.Get<BattleData>(name);
            if (bd == null)
            {
                //Debug.Log("[BattleEditor] Cound not find BattleData for " + name + ". Creating new BattleData instead.");
                bd = ScriptableObject.CreateInstance<T>();
                bd.name = string.Concat(mod.GUID, ".", name);
                bd.bonusUnitPool = new CardData[0];
                bd.bonusUnitRange = new Vector2Int(0, 0);
                bd.generationScript = null; //Decides how the wave lists turn into actual waves. All vanilla fights use a similar generation script
                bd.goldGivers = goldGivers;
                bd.goldGiverPool = new CardData[0];
                if (goldGivers != 0)
                {
                    bd.goldGiverPool = new CardData[1] { mod.Get<CardData>("Gobling") };
                }
                bd.title = name;
                bd.setUpScript = ScriptableObject.CreateInstance<ScriptBattleSetUp>();
                //bd.nameRef = null; //Localized String, done in a different method
                bd.pointFactor = 1;
                bd.pools = null; //Wave Info, done in a different method
                //bd.sprite = null;
                bd.waveCounter = 5;
                bd.pools = new BattleWavePoolData[0];
                bd.ModAdded = mod;
                newBattle = true;
            }
            return this;
        }
            

        public BattleDataEditor GiveMiniBossesCharms(string[] cardNames, params string[] upgradeNames)
        {
            for(int i=0; i<cardNames.Length; i++)
            {
                CardData data = mod.Get<CardData>(cardNames[i]);
                if (data != null)
                {
                    cardNames[i] = data.name;
                }
            }
            List<CardUpgradeData> upgradeData = new List<CardUpgradeData>();
            foreach(string name in upgradeNames)
            {
                CardUpgradeData upgrade = mod.Get<CardUpgradeData>(name);
                if (upgrade != null)
                {
                    upgradeData.Add(upgrade);
                }
                else
                {
                    Debug.LogWarning($"[BattleDataEditor] Could not find a CardUpgrade named {name}");
                }
            }
            ScriptUpgradeMinibosses.Profile profile = new ScriptUpgradeMinibosses.Profile();
            profile.cardDataNames = cardNames;
            profile.possibleUpgrades = upgradeData.ToArray(); ;
            foreach (HardModeModifierData hardModeModifierData in References.instance.hardModeModifiers)
            {
                if (hardModeModifierData.name == "10.BossesHaveCharms")
                {
                    ((ScriptUpgradeMinibosses)hardModeModifierData.modifierData.startScripts[0]).profiles = ((ScriptUpgradeMinibosses)hardModeModifierData.modifierData.startScripts[0]).profiles.Append(profile).ToArray();
                }
            }
            return this;
        }

        public BattleDataEditor GiveGobblers(int add = 1, int toWave = 1, bool randomPosition = false, CardData[] pool = null)
        {
            ScriptAddEnemies.Profile profile;
            profile = new ScriptAddEnemies.Profile();
            profile.battleData = bd;
            profile.add = add;
            profile.toWave = toWave;
            profile.randomPosition = randomPosition;
            profile.pool = pool == null ? new CardData[] { mod.Get<CardData>("Gobbler") } : pool;

            foreach (HardModeModifierData hardModeModifierData in References.instance.hardModeModifiers)
            {
                if (hardModeModifierData.name == "9.MoreEnemiesInBossBattles")
                {
                    ((ScriptAddEnemies)hardModeModifierData.modifierData.startScripts[0]).profiles = ((ScriptAddEnemies)hardModeModifierData.modifierData.startScripts[0]).profiles.Append(profile).ToArray();
                }
            }

            if (!Main.ListeningGobbler)
            {
                Events.OnModUnloaded += Main.CheckGobblerProfiles;
                Main.ListeningGobbler = true;
            }

            return this;
        }

        /// <summary>
        /// Sets the sprite seen on the map.
        /// </summary>
        /// <param name="sprite"></param>
        /// <returns></returns>
        public BattleDataEditor SetSprite(Sprite sprite)
        {
            bd.sprite = sprite;
            return this;
        }

        /// <summary>
        /// Assumes a 105x120px image
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="pixelDensity"></param>
        /// <returns></returns>
        public BattleDataEditor SetSprite(string sprite, int pixelDensity = 100)
        {
            Texture2D tex = mod.ImagePath(sprite).ToTex();
            return SetSprite(Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f), pixelDensity));
        }

        /// <summary>
        /// Sets the interval between waves. Default is 5.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public BattleDataEditor SetWaveCounter(int amount) 
        {
            bd.waveCounter = amount;
            return this;
        }

        public BattleDataEditor SetWavePools(params BattleWavePoolData[] pools)
        {
            bd.pools = pools;
            return this;
        }

        public BattleDataEditor SetNameRef(string nameRef)
        {
            return SetNameRef(nameRef, SystemLanguage.English);
        }

        /// <summary>
        /// Sets the name that shows up on the map page (e.g. "The Teethy Shades" or "The Spike Mokos").
        /// </summary>
        /// <param name="nameRef"></param>
        /// <returns></returns>
        public BattleDataEditor SetNameRef(string nameRef, SystemLanguage lang)
        {
            UnityEngine.Localization.Tables.StringTable collection = LocalizationHelper.GetCollection("Cards", lang);
            collection.SetString(bd.name + "_text", nameRef);
            bd.nameRef = collection.GetString(bd.name + "_text");
            return this;
        }

        /// <summary>
        /// Adds the list enemies to every wave of that wavepool. Should be used mainly for editing existing battles.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="cardNames"></param>
        /// <returns></returns>
        public BattleDataEditor AddCardsToWavePool(int index, params string[] cardNames)
        {
            CardData[] cards = new CardData[cardNames.Length];
            for (int i = 0; i < cardNames.Length; i++)
            {
                cards[i] = mod.Get<CardData>(cardNames[i]);
                if (!cards[i])
                {
                    Debug.LogWarning("[BattleEditor] The card " + cardNames[i] + " does not exist. Check the name again.");
                }
            }
            return AddCardsToWavePool(index, cards);
        }

        /// <summary>
        /// Adds the list enemies to every wave of that wavepool. Should be used mainly for editing existing battles.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="cardNames"></param>
        /// <returns></returns>
        public BattleDataEditor AddCardsToWavePool(int index, params CardData[] cards)
        {
            edits.Add(new Edit(mod, bd, index, cards));
            if (!Main.ListeningReset)
            {
                Events.OnModUnloaded += Main.instance.CheckReset;
                Main.ListeningReset = true;
            }
            Main.resetModList.Add(mod);
            return this;
            /*BattleWavePoolData.Wave[] waves = GetWavesPools(index).waves;
            for (int i = 0; i < waves.Length; i++)
            {
                waves[i].units.AddRange(cards);
                waves[i].maxSize += cards.Length;
            }
            return this;*/
        }

        /// <summary>
        /// Makes a dictionary of enemies indexed by shortened character codes that can be used in formations for ConstructWaves
        /// </summary>
        /// <param name="keyValuePairs"></param>
        /// <returns></returns>
        public BattleDataEditor EnemyDictionary(params (char, string)[] keyValuePairs)
        {
            enemies = new List<CardData>(keyValuePairs.Length);
            dictionary.Clear();
            for (int i = 0; i < keyValuePairs.Length; i++)
            {
                CardData card = mod.Get<CardData>(keyValuePairs[i].Item2) ?? throw new ArgumentException("CardData name is not valid (GUID optional): ", keyValuePairs[i].Item2);
                if (card.cardType == null)
                {
                    throw new ArgumentException("CardData does not have a cardType: ", keyValuePairs[i].Item2);

                }
                enemies.Add(card);
                dictionary.Add(keyValuePairs[i].Item1, enemies[i]);
            }
            return this;
        }

        [Obsolete("Use EnemyDictionary instead")]
        public BattleDataEditor PossibleEnemies(params string[] cardNames)
        {
            enemies = new List<CardData>(cardNames.Length);
            dictionary.Clear();
            for (int i = 0; i < cardNames.Length; i++)
            {
                CardData card = mod.Get<CardData>(cardNames[i]) ?? throw new ArgumentException("CardData name is not valid.", cardNames[i]);
                enemies.Add(card);
            }
            return this;
        }

        [Obsolete("Use EnemyDictionary instead")]
        public BattleDataEditor EnemyKeys(params char[] enemyKeys)
        {
            dictionary.Clear();
            for (int i=0; i<Math.Min(enemies.Count(), enemyKeys.Count()); i++)
            {
                dictionary.Add(enemyKeys[i], enemies[i]);
            }
            return this;
        }

        public BattleWavePoolData GetWavesPools(int index)
        {
            if (bd.pools.Length <= index)
            {
                Debug.Log("[BattleEditor] Index does not exist.");
                return null;
            }
            return bd.pools[index];
        }

        public BattleWavePoolData.Wave GetWave(int wavePoolIndex, int waveIndex)
        {
            if(bd.pools.Length <= wavePoolIndex)
            {
                Debug.Log("[BattleEditor] Wave Pool Index does not exist.");
                return bd.pools[0].waves[0];
            }
            if(bd.pools[wavePoolIndex].waves.Length <= waveIndex)
            {
                Debug.Log("[BattleEditor] Wave Index does not exist.");
                return bd.pools[0].waves[0];
            }
            return bd.pools[wavePoolIndex].waves[waveIndex];
        }

        public BattleDataEditor SetGenerationScript(BattleGenerationScript bgs)
        {
            bd.generationScript = bgs;
            return this;
        }

        public BattleDataEditor FreeModify<T>(Action<T> action) where T : BattleData
        {
            action((T)bd);
            return this;
        }


        /// <summary>
        /// Creates WavePoolData at the desired index and sets the default WavePoolData index here. 
        /// If WavePoolData already exists, it is replaced. 
        /// If the index is above the length, the method adds a new WavePoolData at the end.
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="n"></param>
        /// <param name="forcePulls"> Minimum number of times the battle set-up pulls from this data.</param>
        /// <param name="maxPulls"> Minimum number of times the battle set-up pulls from this data.</param>
        /// <returns></returns>
        public BattleDataEditor StartWavePoolData(int index, string n, int forcePulls = 1, int maxPulls = 1)
        {
            
            if (index >= bd.pools.Length)
            {
                //Debug.Log("[BattleEditor] Adding a new WavePool.");
                bd.pools = bd.pools.AddItem(null).ToArray();
                index = bd.pools.Length-1;
            }
            wavePoolIndex = index;
            BattleWavePoolData bwpd = ScriptableObject.CreateInstance<BattleWavePoolData>();
            bwpd.name = n;
            bwpd.forcePulls = forcePulls;
            bwpd.maxPulls = maxPulls;
            bwpd.pullCount = 0;
            bwpd.weight = 1;
            bd.pools[wavePoolIndex] = bwpd;
            return this;
        }

        /// <summary>
        /// Sets the default WavePoolData index. 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public BattleDataEditor SetWavePoolIndex(int index)
        {
            if (index < bd.pools.Length)
                wavePoolIndex = index;
            return this;
        }

        /// <summary>
        /// Construct the waves in WavePoolData. 
        /// MaxSize should be at least the number of enemies in the wave (more if you want gobling to spawn). 
        /// Formations expects strings of integers that represent cards listed in PossibleEnemies.
        /// </summary>
        /// <param name="bwpd"></param>
        /// <param name="maxSize"></param>
        /// <param name="positionPriority"></param>
        /// <param name="formations"></param>
        /// <param name="cards"></param>
        /// <returns></returns>
        public BattleDataEditor ConstructWaves(int maxSize, int positionPriority, params string[] formations)
        {
            return ConstructWaves(maxSize, positionPriority, formations, true);
        }

        public BattleDataEditor ConstructWaves(int maxSize, int positionPriority, string[] formations, bool removeExistingWaves = true)
        {
            BattleWavePoolData.Wave[] waves = new BattleWavePoolData.Wave[formations.Length];
            for (int i = 0; i < formations.Length; i++)
            {
                string formation = formations[i];
                BattleWavePoolData.Wave wave = new BattleWavePoolData.Wave();
                wave.value = 100; //I don't know what this does.
                wave.positionPriority = positionPriority; //Same here
                wave.fixedOrder = true;
                wave.maxSize = maxSize; //Making this value larger than your formation gives Gobling a place to spawn.
                wave.units = new List<CardData>(formation.Length);
                for (int j = 0; j < formation.Length; j++)
                {
                    string c = formation.Substring(j, 1);
                    if (dictionary.ContainsKey(c[0]))
                    {
                        wave.units.Add(dictionary[c[0]]);
                    }
                    else
                    {
                        wave.units.Add(enemies[int.Parse(c)]);
                    }
                }
                waves[i] = wave;
            }
            if (removeExistingWaves)
            {
                bd.pools[wavePoolIndex].waves = waves;
            }
            else
            {
                bd.pools[wavePoolIndex].waves = bd.pools[wavePoolIndex].waves.AddRangeToArray(waves);
            }
            return this;
        }

        /// <summary>
        /// [Important] Loads the battle into the game if it is a new battle.
        /// Does nothing otherwise.
        /// </summary>
        /// <returns></returns>
        public BattleDataEditor AddBattleToLoader()
        {
            if (newBattle)
            {
                bd.generationScript = bd.generationScript ?? mod.Get<BattleData>("Pengoons").generationScript;
                AddressableLoader.AddToGroup<BattleData>("BattleData", bd);
                newBattle = false;
                Debug.Log("[BattleEditor] The " + bd.name + " battle is loaded.");
            }
            return this;
        }

        [Obsolete("Use the version of LoadBattle with an exclusivity parameter")]
        public BattleDataEditor RegisterBattle(int tier = 0, string gameMode = "GameModeNormal", bool mandatory = false)
        {
            return LoadBattle(tier, true, gameMode, mandatory ? BattleStack.Exclusivity.removeUnmodded : BattleStack.Exclusivity.removeNone);
        }

        [Obsolete("Use the version of LoadBattle with an exclusivity parameter")]
        public BattleDataEditor RegisterBattle(int tier, bool resetAllOnClear, string gameMode = "GameModeNormal", bool mandatory = false)
        {
            return LoadBattle(tier, resetAllOnClear, gameMode, mandatory ? BattleStack.Exclusivity.removeUnmodded : BattleStack.Exclusivity.removeNone);
        }

        /// <summary>
        /// [Important] Places the battle in the pool of other battles of the same tiers:
        /// 0 = Pengoons/Snowbo, 1=Bears/Shroom/Berries/Ringer, 2=Infernoko/Bamboozle, etc.
        /// Setting mandatory to true removes all other battles in the tier.
        /// </summary>
        /// <param name="tier"></param>
        /// <param name="gameMode"></param>
        /// <param name="mandatory"></param>
        /// <returns></returns>
        public BattleDataEditor LoadBattle(int tier, bool resetAllOnClear = true, string gameMode = "GameModeNormal", BattleStack.Exclusivity exclusivity = BattleStack.Exclusivity.removeNone, bool startActive = true)
        {
            GameMode game = mod.Get<GameMode>(gameMode);
            if (!game)
            {
                throw new Exception($"Gamemode [{gameMode}] or [{mod.GUID + "." + gameMode} does not exist");
            }

            if (!changelog.ContainsKey(game.name))
            {
                changelog[game.name] = new List<BattleStack>();
            }

            BattleStack stack = changelog[game.name].FirstOrDefault(s => s.tier == tier);
            if (stack == null)
            {
                stack = new BattleStack(mod, game.name, tier);
                changelog[game.name].Add(stack);
            }
            stack.Add(mod, bd, exclusivity, startActive);
            if (resetAllOnClear)
            {
                if (!Main.ListeningReset)
                {
                    Events.OnModUnloaded += Main.instance.CheckReset;
                    Main.ListeningReset = true;
                }
                Main.resetModList.Add(mod);
            }
            return this;
        }

        public BattleDataEditor ToggleBattle(bool active, string gameMode = "GameModeNormal")
        {
            ToggleBattle(mod, bd, active, gameMode);
            return this;
        }

        public static void ToggleBattle(WildfrostMod mod, BattleData battle, bool active, string gameMode = "GameModeNormal")
        {
            GameMode game = mod.Get<GameMode>(gameMode);
            if (!game)
            {
                throw new Exception($"Gamemode [{gameMode}] or [{mod.GUID + "." + gameMode} does not exist");
            }
            if (!battle)
            {
                throw new Exception($"BattleData does not exist");
            }
            foreach (var stack in changelog[game.name])
            {
                stack.ChangeActive(battle, active);
            }
        }

        public static void DisplayOutput()
        {
            foreach(var list in changelog.Values)
            {
                foreach(var item in list)
                {
                    item.Output();
                }
            }
        }
    }
}
