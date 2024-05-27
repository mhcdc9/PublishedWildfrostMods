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

namespace BattleEditor
{
    public class Main : WildfrostMod
    {
        public Main(string modDirectory) : base(modDirectory)
        {
        }

        public void ExampleCode()
        {
            new BattleDataEditor(this, "Spare Shells")
            .SetSprite(this.ImagePath("Spare Shells.png").ToSprite())
            .SetNameRef("The Other Shelled Husks")
            .EnemyDictionary(('C', "Conker"), ('W', "ShellWitch"), ('P', "Pecan"), ('K', "Prickle"), ('B', "Bolgo"))
            .StartWavePoolData(0, "Wave 1: The first of the husks")
            .ConstructWaves(3, 0, "CWW", "CWP")
            .StartWavePoolData(1, "Wave 2: Some more husks")
            .ConstructWaves(3, 1, "PCW", "CPW", "CKW", "KCW")
            .StartWavePoolData(2, "Wave 3: Bolgo is here!")
            .ConstructWaves(3, 9, "BPW", "BCW")
            .AddBattleToLoader().RegisterBattle(0, mandatory: true);
        }

        public override string GUID => "mhcdc9.wildfrost.battle";

        public override string[] Depends => new string[0];

        public override string Title => "Battle Data Editor";

        public override string Description => "[Backend] Provides useful functions to the creation/editing of battles.";
    }

    public class BattleDataEditor
    {
        private readonly WildfrostMod mod;
        public readonly BattleData bd;
        private bool newBattle = false;
        private List<CardData> enemies = new List<CardData>();
        private Dictionary<char, CardData> dictionary = new Dictionary<char, CardData>();
        private Dictionary<char, >
        private int wavePoolIndex = 0;

        public readonly string[] VanillaBattles =
        {
            "Pengoons", "Snowbos", 
            "Berries", "Frosters", "Shroomers", "Yeti",
            "Frenzy Boss", "Split Boss",
            "Goats", "Husks", "Spice Monkeys",
            "Drek", "Spikers",
            "Clunker Boss", "Toadstool Boss",
            "Blockers", "Wildlings",
            "Final Boss",
            "Final Final Boss"
        };

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
            bd = mod.Get<BattleData>(name);
            if (bd == null)
            {
                Debug.LogWarning("[BattleEditor] Cound not find BattleData for " + name + ". Creating new BattleData instead.");
                bd = ScriptableObject.CreateInstance<BattleData>();
                bd.name = name;
                bd.bonusUnitPool = new CardData[0];
                bd.bonusUnitRange = new Vector2Int(0, 0);
                bd.generationScript = null; //Unsure of what this is, so I'm going to set it to be the same as the other battles :P 
                bd.goldGivers = goldGivers;
                bd.goldGiverPool = new CardData[0];
                if (goldGivers != 0)
                {
                    bd.goldGiverPool = new CardData[1] { mod.Get<CardData>("Gobling").Clone() };
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

        /// <summary>
        /// Sets the name that shows up on the map page (e.g. "The Teethy Shades" or "The Spike Mokos").
        /// </summary>
        /// <param name="nameRef"></param>
        /// <returns></returns>
        public BattleDataEditor SetNameRef(string nameRef)
        {
            UnityEngine.Localization.Tables.StringTable collection = LocalizationHelper.GetCollection("Cards", SystemLanguage.English);
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
                cards[i] = mod.Get<CardData>(cardNames[i]).Clone();
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
            BattleWavePoolData.Wave[] waves = GetWavesPools(index).waves;
            for (int i = 0; i < waves.Length; i++)
            {
                waves[i].units.AddRange(cards);
                waves[i].maxSize += cards.Length;
            }
            return this;
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
                CardData card = mod.Get<CardData>(keyValuePairs[i].Item2) ?? throw new ArgumentException("CardData name is not valid.", keyValuePairs[i].Item2);
                enemies.Add(card.Clone());
                dictionary.Add(keyValuePairs[i].Item1, enemies[i]);
            }
            return this;
        }

        /// <summary>
        /// Loads enemies into memory to be used later. Use this before the first ConstructWaves(). Order matter later. EnemyKeys are also cleared to default.
        /// </summary>
        /// <param name="cardNames"></param>
        /// <returns></returns>
        public BattleDataEditor PossibleEnemies(params string[] cardNames)
        {
            enemies = new List<CardData>(cardNames.Length);
            dictionary.Clear();
            for (int i = 0; i < cardNames.Length; i++)
            {
                CardData card = mod.Get<CardData>(cardNames[i]) ?? throw new ArgumentException("CardData name is not valid.", cardNames[i]);
                enemies.Add(card.Clone());
            }
            return this;
        }

        /// <summary>
        /// Add keys for enemies added by Possible Enemies(). If not specified, numbers can be used instead.
        /// </summary>
        /// <param name="enemyKeys"></param>
        /// <returns></returns>
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
                Debug.LogWarning("[Warning] Index does not exist.");
                return null;
            }
            return bd.pools[index];
        }

        public BattleWavePoolData.Wave GetWave(int wavePoolIndex, int waveIndex)
        {
            if(bd.pools.Length <= wavePoolIndex)
            {
                Debug.LogWarning("[Warning] Wave Pool Index does not exist.");
                return bd.pools[0].waves[0];
            }
            if(bd.pools[wavePoolIndex].waves.Length <= waveIndex)
            {
                Debug.LogWarning("[Warning] Wave Index does not exist.");
                return bd.pools[0].waves[0];
            }
            return bd.pools[wavePoolIndex].waves[waveIndex];
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
                Debug.Log("[Warning] WavePool index does not exist. Ading a new WavePool.");
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
                        wave.units.Add(dictionary[c[0]].Clone());
                    }
                    else
                    {
                        wave.units.Add(enemies[int.Parse(c)].Clone());
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
                bd.generationScript = mod.Get<BattleData>("Pengoons").generationScript;
                AddressableLoader.AddToGroup<BattleData>("BattleData", bd);
                newBattle = false;
                Debug.Log("[BattleEditor] The " + bd.name + " battle is loaded.");
            }
            return this;
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
        public BattleDataEditor RegisterBattle(int tier, string gameMode = "GameModeNormal", bool mandatory = false)
        {
            GameMode game = mod.Get<GameMode>(gameMode);
            if (!game)
            {
                Debug.LogWarning("Gamemode does not exist.");
            }
            if (mandatory)
            {
                game.populator.tiers[tier].battlePool = new BattleData[] { bd };
                Debug.Log("[BattleEditor] The " + bd.name + "is the only battle in tier " + tier.ToString());
            }
            else
            {
                game.populator.tiers[tier].battlePool = game.populator.tiers[tier].battlePool.AddToArray(bd);
                Debug.Log("[BattleEditor] The " + bd.name + "is in tier " + tier.ToString()); 
            }
            
            
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tier"></param>
        public static void ResetTier(int tier, params string[] battleNames)
        {
            for(int i=0; i<battles.Length; i++)
        }
    }
}
