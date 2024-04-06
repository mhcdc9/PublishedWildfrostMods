using Deadpan.Enums.Engine.Components.Modding;
using FMODUnity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace GamemodeAndClasses
{
    public class ClassDataBuilderExt : ClassDataBuilder
    {

        public ClassDataBuilderExt() { }

        public ClassDataBuilderExt(WildfrostMod mod) : base(mod)
        {
        }

        public ClassDataBuilderExt CreateExt(string name)
        {
            return (ClassDataBuilderExt)Create(name);
        }

        public ClassDataBuilderExt CreateExt<X>(string name) where X : ClassData
        {
            return (ClassDataBuilderExt)Create<X>(name);
        }

        public ClassDataBuilderExt NewStartingInventory(params CardData[] cards)
        {
            _data.startingInventory = new Inventory();
            _data.startingInventory.deck.list = _data.startingInventory.deck.Concat(cards).ToList();
            return this;
        }

        public ClassDataBuilderExt AddCardsInDeck(params CardData[] cards)
        {
            if (_data?.startingInventory == null)
            {
                _data.startingInventory = new Inventory();
                _data.startingInventory.deck.list = cards.ToList();
            }
            else
            {
                _data.startingInventory.deck.list = _data.startingInventory.deck.Concat(cards).ToList();
            }
            return this;
        }

        public ClassDataBuilderExt AddCardsToReserve(params CardData[] cards)
        {
            if (_data?.startingInventory == null)
            {
                _data.startingInventory = new Inventory();
                _data.startingInventory.reserve.list = cards.ToList();
            }
            else
            {
                _data.startingInventory.reserve.list = _data.startingInventory.reserve.Concat(cards).ToList();
            }
            return this;
        }

        public ClassDataBuilderExt AddUpgrades(params CardUpgradeData[] upgrades)
        {
            if (_data?.startingInventory == null)
            {
                _data.startingInventory = new Inventory();
                _data.startingInventory.upgrades = upgrades.ToList();
            }
            else
            {
                _data.startingInventory.upgrades = _data.startingInventory.upgrades.Concat(upgrades).ToList();
            }
            return this;
        }

        public ClassDataBuilderExt AddGold(int gold, int goldOwed)
        {
            _data.startingInventory.gold = new Dead.SafeInt(gold);
            _data.startingInventory.goldOwed = goldOwed;
            return this;
        }

        [Obsolete]
        public ClassDataBuilderExt SetLeaders(params CardData[] cards)
        {
            _data.leaders = cards;
            return this;
        }

        [Obsolete]
        public ClassDataBuilderExt SetCharacterPrefabs(Character character)
        {
            _data.characterPrefab = character;
            return this;
        }

        [Obsolete]
        public ClassDataBuilderExt SetRewardPools(params RewardPool[] rewardPools)
        {
            _data.rewardPools = rewardPools;
            return this;
        }

        [Obsolete]
        public ClassDataBuilderExt AddRewardPools(params RewardPool[] rewardPools)
        {
            if (_data.rewardPools == null) 
            {
                return SetRewardPools(rewardPools);
            }
            _data.rewardPools  = _data.rewardPools.Concat(rewardPools).ToArray();
            return this;
        }

        [Obsolete]
        public ClassDataBuilderExt SetSelectSFX(EventReference selectSfxEvent)
        {
            _data.selectSfxEvent = selectSfxEvent;
            return this;
        }

        [Obsolete]
        public ClassDataBuilderExt SetFlag(Sprite sprite)
        {
            _data.flag = sprite;
            return this;
        }
    }
}
