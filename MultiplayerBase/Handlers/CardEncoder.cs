using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using EffectStack = CardData.StatusEffectStacks;
using TraitStack = CardData.TraitStacks;
using Debug = UnityEngine.Debug;
using System.Collections;

namespace MultiplayerBase.Handlers
{
    public static class CardEncoder
    {

        //[X]!height!damageCurrent!damageMax!hpcurrent!hpMax!counterMax!counterCurrent [!usesCurrent!usesMax!]
        public static string Encode(Entity entity, ulong id)
        {
            string s = Encode(entity.data, id) + "!";
            s += $"{entity.height}!";
            s += $"{entity.damage.max}!" + $"{entity.damage.current}!";
            s += $"{entity.hp.max}!" + $"{entity.hp.current}!";
            s += $"{entity.counter.max}!" + $"{entity.counter.current}";
            return s;
        }

        //CardData!customData!attackEffects!startWithEffects!traits!injuries!hp!damage!counter!upgrades!forceTitle
        public static string Encode(CardData cardData, ulong id)
        {
            string s = $"{id}!{cardData.name}!"; //0.CardData (id doesn't count)
            if (cardData.customData != null) //1. CustomData
            {
                Dictionary<string, object> customData = cardData.customData;
                foreach (string key in customData.Keys)
                {
                    s += $"{key},";
                }
            }
            s += "!";
            foreach (EffectStack stack in cardData.attackEffects) //2. attackEffects
            {
                s += $"{stack.count} {stack.data.name.Replace(",", ",:")}, ";
            }
            s += "!";
            foreach (EffectStack stack in cardData.startWithEffects) //3. startWithEffects
            {
                s += $"{stack.count} {stack.data.name.Replace(",", ",:")}, ";
            }
            s += "!";
            foreach (TraitStack stack in cardData.traits) //4. traits
            {
                s += $"{stack.count} {stack.data.name.Replace(",", ",:")}, ";
            }
            s += "!";
            foreach (EffectStack stack in cardData.injuries) //5. injuries
            {
                s += $"{stack.count} {stack.data.name.Replace(",", ",:")}, ";
            }
            s += "!";
            s += $"{IntOrNull(cardData.hp)}!" + $"{IntOrNull(cardData.damage)}!" + $"{IntOrNull(cardData.counter)}!"; //6-8. hp, damage, counter
            foreach(CardUpgradeData upgrade in cardData.upgrades) //9. upgrades
            {
                s += $"{upgrade.name.Replace(",", ",:")}, ";
            }
            s += "!";
            s += cardData.forceTitle.Replace('!', 'l').Replace('|','l'); //10. nickname
            return s;
        }

        public static IEnumerator CreateAndPlaceEntity(CardController cc, CardContainer container, string[] messages)
        {

            Entity entity = DecodeEntity1(cc, container.owner, messages);
            yield return DecodeEntity2(entity, messages);
            container.Add(entity);
            container.SetChildPosition(entity);
            
            entity.flipper.FlipUp(force: true);
        }

        public static Entity DecodeEntity1(CardController cc, Character owner, string[] messages)
        {
            CardData cardData = DecodeData(messages);
            Card card = CardManager.Get(cardData, cc, owner, inPlay: false, isPlayerCard: true);
            if (References.Battle?.cards != null)
            {
                References.Battle.cards.Remove(card.entity);
            }
            card.entity.flipper.FlipDownInstant();
            Entity entity = card.entity;
            return entity;
        }

        public static IEnumerator DecodeEntity2(Entity entity, string[] messages)
        {
            yield return entity.display.UpdateData();
            if (messages.Length <= 11)
            {
                yield break;
            }
            int i;
            if (int.TryParse(messages[11], out i)) { entity.height = i; } //11. Height
            if (int.TryParse(messages[12], out i)) { entity.damage.max = i; } //12. Damage Max
            if (int.TryParse(messages[13], out i)) { entity.damage.current = i; } //13. Damage Current
            if (int.TryParse(messages[14], out i)) { entity.hp.max = i; } //14. HP Max
            if (int.TryParse(messages[15], out i)) { entity.hp.current = i; } //15. HP Current
            if (int.TryParse(messages[16], out i)) { entity.counter.max = i; }//16. Counter Max
            if (int.TryParse(messages[17], out i)) { entity.counter.current = i; }//17. Counter Current
            entity.enabled = true;
        }

        //CardData!customData!attackEffects!startWithEffects!traits!injuries!hp!damage!counter!upgrades!forceTitle!
        public static CardData DecodeData(string[] messages)
        {
            foreach(string message in messages)
            {
                Debug.Log(message);
            }
            CardData data = AddressableLoader.Get<CardData>("CardData", messages[0]); //0. CardData
            if (data == null)
            {
                return data;
            }
            Debug.Log("[Multiplayer] 0");
            data = data.Clone(false);
            if (!messages[1].IsNullOrEmpty()) //1. CustomData: Fix Later
            {
                if (data.cardType.name == "Leader")
                {
                    Debug.Log("[Multiplayer] Leader Detected.");
                    data.customData = References.PlayerData.inventory.deck.FirstOrDefault((deckcard) => deckcard.cardType.name == "Leader").customData;
                }
            }
            Debug.Log("[Multiplayer] 1");
            data.attackEffects = DecodeToEffectStacks(messages[2]).ToArray(); //2. attackEffects
            Debug.Log("[Multiplayer] 2");
            data.startWithEffects = DecodeToEffectStacks(messages[3]).ToArray(); //3. startWithEffects
            Debug.Log("[Multiplayer] 3");
            data.traits = DecodeToTraitStacks(messages[4]); //4. traits
            Debug.Log("[Multiplayer] 4");
            data.injuries = DecodeToEffectStacks(messages[5]); //5. injuries
            Debug.Log("[Multiplayer] 5");
            int.TryParse(messages[6], out data.hp); //6. hp
            Debug.Log("[Multiplayer] 6");
            int.TryParse(messages[7], out data.damage); //7. damage
            Debug.Log("[Multiplayer] 7");
            int.TryParse(messages[8], out data.counter); //8. counter
            Debug.Log("[Multiplayer] 8");
            data.upgrades = DecodeUpgrades(messages[9]); //9. upgrades
            Debug.Log("[Multiplayer] 9");
            if (data.upgrades.Any((CardUpgradeData a) => a.becomesTargetedCard))
            {
                data.hasAttack = true;
                if (data.playType == Card.PlayType.None)
                {
                    data.playType = Card.PlayType.Play;
                }

                data.needsTarget = true;
            }
            data.forceTitle = messages[10]; // 10. nickname
            Debug.Log("[Multiplayer] 10");
            return data;
        }

        public static List<EffectStack> DecodeToEffectStacks(string s)
        {
            List<EffectStack> stacks = new List<EffectStack>();
            string[] messages = s.Split(new string[]{ ", "}, StringSplitOptions.RemoveEmptyEntries);
            foreach(string message in messages)
            {
                int splitIndex = message.IndexOf(' ');
                int count = int.Parse(message.Substring(0, splitIndex));
                string name = message.Substring(splitIndex + 1).Replace(",:", ","); ;
                StatusEffectData effect = AddressableLoader.Get<StatusEffectData>("StatusEffectData", name);
                if (effect != null)
                {
                    stacks.Add(new EffectStack(effect, count));
                }
            }
            return stacks;
        }

        public static List<TraitStack> DecodeToTraitStacks(string s)
        {
            List<TraitStack> stacks = new List<TraitStack>();
            string[] messages = s.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string message in messages)
            {
                int splitIndex = message.IndexOf(' ');
                int count = int.Parse(message.Substring(0, splitIndex));
                string name = message.Substring(splitIndex + 1).Replace(",:",",");
                TraitData effect = AddressableLoader.Get<TraitData>("TraitData", name);
                if (effect != null)
                {
                    stacks.Add(new TraitStack(effect, count));
                }
            }
            return stacks;
        }

        public static List<CardUpgradeData> DecodeUpgrades(string s)
        {
            List<CardUpgradeData> upgrades = new List<CardUpgradeData>();
            string[] messages = s.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string message in messages)
            {
                CardUpgradeData upgrade = AddressableLoader.Get<CardUpgradeData>("CardUpgradeData", message.Replace(",:", ","));
                if (upgrade != null)
                {
                    upgrades.Add(upgrade);
                }
            }
            return upgrades;
        }

        public static string IntOrNull(int? value)
        {
            return (value == null) ? "" : value.ToString();
        }
    }
}
