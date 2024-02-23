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
    internal class StatusEffectEvolveFromKill : StatusEffectEvolve
    {

        public static Dictionary<string, string> upgradeMap = new Dictionary<string, string>();
        //public string[] typeConditions = null;
        public Action<Entity, DeathType> constraint = ReturnTrue;
        public static bool result = false;
        public bool anyKill = false;

        public override void Init()
        {
            base.Init();
            foreach(CardData.StatusEffectStacks statuses in target.data.startWithEffects)
            {
                if (statuses.data.name == this.name)
                {
                    //typeConditions = ((StatusEffectEvolveFromKill)statuses.data).typeConditions;
                    constraint = ((StatusEffectEvolveFromKill)statuses.data).constraint;
                    return;
                }
            }
        }

        public static void ReturnTrue(Entity entity, DeathType deathType)
        {
            StatusEffectEvolveFromKill.result = true;
            return;
        }

        public static void ReturnTrueIfCardTypeIsBossOrMiniboss(Entity entity, DeathType deathType)
        {
            switch (entity.data.cardType.name)
            {
                case "Boss":
                case "Miniboss":
                case "BossSmall":
                    StatusEffectEvolveFromKill.result = true;
                    return;
            }
        }
        
        public static void ReturnTrueIfCardWasConsumed(Entity entity, DeathType deathType)
        {
            if (deathType == DeathType.Consume)
            {
                StatusEffectEvolveFromKill.result = true;
                return;
            }
        }

        public virtual void SetConstraints(Action<Entity, DeathType> c)
        {
            constraint = c;
        }

        public override void Autofill(string n, string descrip, WildfrostMod mod)
        {
            base.Autofill(n, descrip, mod);

            type = "evolve2";
        }

        public virtual void SetCondition(params string[] types)
        {
            //typeConditions = types;
        }

        

        public override bool RunEntityDestroyedEvent(Entity entity, DeathType deathType)
        {
            UnityEngine.Debug.Log(entity.data.title + ", " + deathType.ToString());
            constraint(entity, deathType);
            bool deserving = anyKill || (entity.lastHit != null && entity.lastHit.attacker == target);
            if (deserving && result)
            {
                UnityEngine.Debug.Log("[Debug] Confrimed Kill!");
                foreach (StatusEffectData statuses in target.statusEffects)
                {
                    if (statuses.name == this.name && this.count > 0)
                    {
                        this.count--;
                        target.display.promptUpdateDescription = true;
                        target.PromptUpdate();
                        UnityEngine.Debug.Log("[Debug] Updated card on board!");
                    }
                }
                foreach(CardData card in References.Player.data.inventory.deck)
                {
                    if (card.id == target.data.id)
                    {
                        foreach (CardData.StatusEffectStacks statuses in card.startWithEffects)
                        {
                            if (statuses.data.name == this.name && statuses.count > 0)
                            {
                                statuses.count--;
                                UnityEngine.Debug.Log("[Debug] Updated deck copy!");
                            }
                        }
                    }
                }
                
            }
            result = false;
            return false;
        }

        public override bool ReadyToEvolve(CardData cardData)
        {
            foreach (CardData.StatusEffectStacks statuses in cardData.startWithEffects)
            {
                if (statuses.data.name == this.name)
                {
                    return (statuses.count == 0);
                }
            }
            return false;
        }
    }
}
