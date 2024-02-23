using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TestMod
{
    internal class StatusEffectEvolveFromMoney : StatusEffectEvolve
    {

        public static Dictionary<string, string> upgradeMap = new Dictionary<string, string>();
        public int threshold;
        public Action<int> constraint = ReturnTrueIfMoneyIsAboveThreshold;
        public static bool result = false;

        public static void ReturnTrueIfMoneyIsAboveThreshold(int t)
        {
            result = (References.Player.data.inventory.gold >= t);
        }

        public static void ReturnTrueIfEmptyDeck(int t)
        {
            result = (References.Player.drawContainer.Count + References.Player.handContainer.Count + References.Player.discardContainer.Count == 0);
        }

        public void SetConstraint(Action<int> c)
        {
            constraint = c;
        }

        public override void Init()
        {
            base.Init();
            foreach(CardData.StatusEffectStacks statuses in target.data.startWithEffects)
            {
                if (statuses.data.name == this.name)
                {
                    threshold = ((StatusEffectEvolveFromMoney)statuses.data).threshold;
                    return;
                }
            }
        }

        public override void Autofill(string n, string descrip, WildfrostMod mod)
        {
            base.Autofill(n, descrip, mod);

            type = "evolve2";
            UnityEngine.Localization.Tables.StringTable collection = LocalizationHelper.GetCollection("Card Text", SystemLanguage.English);
            collection.SetString(name + "_text", descrip);
            textKey = collection.GetString(name + "_text");
        }

        public override bool ReadyToEvolve(CardData cardData)
        {
            foreach (CardData.StatusEffectStacks statuses in cardData.startWithEffects)
            {
                if (statuses.data.name == this.name)
                {
                    constraint(statuses.count);
                    return result;
                }
            }
            return false;
        }
    }
}
