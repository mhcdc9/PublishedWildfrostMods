using Dead;
using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WildfrostHopeMod;

namespace RandomStat
{
    public class RandomStats : WildfrostMod
    {
        [ConfigManagerTitle("Base HP (%)")]
        [ConfigManagerDesc("A card's minimum HP is determined will be a percentage of its original hp (default=0.5)")]
        [ConfigSlider(0, 1f)]
        [ConfigItem(0.5f, "Base HP")]
        public float hpBase = 0.5f;

        [ConfigManagerTitle("Random HP Multiplier")]
        [ConfigManagerDesc("A card's average random HP is this multiplier times its original hp (default=0.5)")]
        [ConfigSlider(0, 2f)]
        [ConfigItem(0.5f, "HP Variance")]
        public float hpVariance = 0.5f;

        [ConfigManagerTitle("Base Attack (%)")]
        [ConfigManagerDesc("A card's minimum attack is determined will be a percentage of its original attack (default=0.2)")]
        [ConfigSlider(0, 1f)]
        [ConfigItem(0.2f, "Base Attack")]
        public float attackBase = 0.2f;

        [ConfigManagerTitle("Random Attack Multiplier")]
        [ConfigManagerDesc("A card's average random attack is almost this multiplier times its original attack (default=1.0)")]
        [ConfigSlider(0, 2f)]
        [ConfigItem(1f, "Attack Variance")]
        public float attackVariance = 1f;

        [ConfigManagerTitle("Counter Variance")]
        [ConfigManagerDesc("Higher variance leads to more volatile counters (default=0.5, Be careful)")]
        [ConfigSlider(0, 0.95f)]
        [ConfigItem(0.5f, "Counter Variance")]
        public float counterVariance = 0.5f;

        [ConfigManagerTitle("Counter Skew")]
        [ConfigManagerDesc("Counter skew modifies the average change in counters. Lower skew leads to smaller counters (default=-0.3)")]
        [ConfigSlider(-1f, 1f)]
        [ConfigItem(-0.3f, "Counter Skew")]
        public float counterSkew = -0.3f;


        public RandomStats(string modDirectory) : base(modDirectory)
        {
            
        }

        public override string GUID => "mhcdc9.wildfrost.randomizestats";
        public override string[] Depends => new string[] { };
        public override string Title => "Randomized Stats";
        public override string Description => "On average, the stats are the same. In reality...";

        protected override void Load()
        {
            base.Load();
            Events.OnCardDataCreated += RandomizeStats;
        }

        protected override void Unload()
        {
            base.Unload();
            Events.OnCardDataCreated -= RandomizeStats;
        }

        private void RandomizeStats(CardData cardData)
        {
            if (cardData.hasHealth)
            {
                float baseHealth = hpBase*cardData.hp + Geom(hpVariance*cardData.hp);
                cardData.hp = (int) Math.Ceiling(baseHealth);
            }
            if (cardData.hasAttack)
            {
                float baseAttack = attackBase*cardData.hp + Geom(attackVariance * (cardData.damage + 0.5f));
                cardData.damage = (int)Math.Ceiling(baseAttack);
            }
            if (cardData.counter != 0)
            {
                cardData.counter = Stoch(cardData.counter, counterVariance, counterSkew);
            }
        }

        private int Geom(float mean)
        {
            float p = mean / (1 + mean);

            float r = Dead.Random.Range(0f, 1f);
            int count = 0;
            while (r < p)
            {
                r /= p;
                count++;
            }
            return count;
        }

        private int Stoch(int counter, float variance, float skew)
        {
            float reroll = Dead.Random.Range(0f, 1f);
            while(reroll < variance)
            {
                var r = Dead.Random.Range(0f, 1f);
                if(r < ((1f+skew)*counter+1.01f-skew)/(2f*counter))
                {
                    counter++;
                }
                else
                {
                    counter--;
                }
                reroll = Dead.Random.Range(0f, 1f);
            }
            return counter;
        }
    }
}
