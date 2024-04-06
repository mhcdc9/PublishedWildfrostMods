using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GamemodeAndClasses
{
    internal class GameModeBuilderExt : GameModeBuilder
    {
        public GameModeBuilderExt() { }

        public GameModeBuilderExt(WildfrostMod mod) : base(mod) { }

        public GameModeBuilderExt CreateGameMode(string name, string title, string saveFileSuffix = null, string startScene = "CharacterSelect", bool mainGameMode = true)
        {
            return CreateGameMode<GameMode>(name, title, saveFileSuffix, startScene, mainGameMode);
    }
        public GameModeBuilderExt CreateGameMode<X>(string name, string title, string saveFileSuffix = null, string startScene = "CharacterSelect", bool mainGameMode = true) where X : GameMode
        {
            Create<X>(name);
            if (saveFileSuffix != null)
            {
                _data.saveFileName = saveFileSuffix;
                _data.doSave = true;
            }
            else
            {
                _data.saveFileName = "";
                _data.doSave = false;
            }
            _data.startScene = startScene;
            _data.mainGameMode = mainGameMode;
            _data.campaignSystemNames = new string[0];
            _data.systemsToDisable = new string[] {"TutorialSystem"};
            return Register(title);
        }

        public GameModeBuilderExt SetClasses(params string[] classes)
        {
            return (GameModeBuilderExt)WithClasses(classes.Select(Mod.Get<ClassData>).ToArray());
        }

        [Obsolete]
        public GameModeBuilderExt SetClasses(params ClassData[] classes)
        {
            _data.classes = classes;
            return this;
        }

        public GameModeBuilderExt NewGenerator(params string[] mapPresets)
        {
            _data.generator = ScriptableObject.CreateInstance<CampaignGenerator>();
            _data.generator.presets = (from item in mapPresets select new TextAsset(item)).ToArray();
            return this;
        }

        [Obsolete]
        public GameModeBuilderExt NewGenerator(CampaignGenerator generator)
        {
            _data.generator = generator;
            return this;
        }

        public GameModeBuilderExt NewCampaignPopulator()
        {
            _data.populator = new CampaignPopulator();
            return this;
        }

        [Obsolete]
        public GameModeBuilderExt NewCampaignPopulator(CampaignPopulator populator)
        {
            _data.populator = populator;
            return this;
        }

        public GameModeBuilderExt NewTier(BattleData[] battles, CampaignNodeType[] rewards)
        {
            CampaignTier ct = ScriptableObject.CreateInstance<CampaignTier>();
            ct.battlePool = battles;
            ct.rewardPool = rewards;
            _data.populator.tiers = _data.populator.tiers.Append(ct).ToArray();
            return this;
        }

        public GameModeBuilderExt SetTier(int index, BattleData[] battles = null, CampaignNodeType[] rewards = null)
        {
            CampaignTier tier = _data.populator.tiers[index];
            if (battles != null)
            {
                tier.battlePool = battles;
            }
            if (rewards != null)
            {
                tier.rewardPool = rewards;
            }
            return this;
        }

        public GameModeBuilderExt ResetTiers(int numberOfNewTiers)
        {
            _data.populator.tiers = new CampaignTier[numberOfNewTiers];
            return this;
        }

        public GameModeBuilderExt Register(string title)
        {
            MainModClass.gameModes.Add(_data.name);
            MainModClass.displayedNames.Add(title);
            return this;
        }


    }
}
