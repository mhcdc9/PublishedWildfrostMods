using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TestMod
{
    internal class StatusEffectEvolveOnUpgrade : StatusEffectEvolve
    {
        public override void Autofill(string n, string descrip, WildfrostMod mod)
        {
            base.Autofill(n,descrip,mod);
            type = "evolve2";
        }

        public override bool ReadyToEvolve(CardData cardData)
        {
            foreach(CardUpgradeData upgrade in cardData.upgrades) 
            {
                if (upgrade.type == CardUpgradeData.Type.Charm)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
