using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestMod
{
    internal class StatusEffectChangeData : StatusEffectData
    {
        public CardData cardBase;
        public int keepIndex = 0;

        public override void Init()
        {

            base.OnTurn += TurnStart;
        }

        private IEnumerator TurnStart(Entity entity)
        {
            if (entity == target)
            {
                ChangeCard();
                target.PromptUpdate();
                target.display.UpdateData(true);
            }
            yield break;
        }

        public void ChangeCard()
        {
            IEnumerable<CardData> cards = AddressableLoader.GetGroup<CardData>("CardData").InRandomOrder();
            foreach(CardData card in cards)
            {
                if (card.cardType.name == "Item")
                {
                    cardBase = card;
                    break;
                }
            }
        }

        public void UpdateData()
        {
            CardData trueData = target.data;
            trueData.mainSprite = cardBase.mainSprite;
            trueData.canPlayOnBoard = cardBase.canPlayOnBoard;
            trueData.canPlayOnEnemy = cardBase.canPlayOnEnemy;
            trueData.canPlayOnHand = cardBase.canPlayOnHand;
            trueData.canPlayOnFriendly = cardBase.canPlayOnFriendly;
            trueData.damage = cardBase.damage;
            trueData.needsTarget = cardBase.needsTarget;
            trueData.playOnSlot = cardBase.playOnSlot;
            trueData.textInsert = cardBase.title;

            trueData.startWithEffects = CardData.StatusEffectStacks.Stack(new CardData.StatusEffectStacks[1] { trueData.startWithEffects[keepIndex] }, cardBase.startWithEffects);
            trueData.attackEffects = cardBase.attackEffects;
        }
    }
}
