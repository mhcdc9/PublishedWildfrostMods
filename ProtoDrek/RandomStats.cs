using Dead;
using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomStat
{
    public class RandomStats : WildfrostMod
    {
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
                var baseHealth = cardData.hp%2 + cardData.hp/2 + Geom(cardData.hp/2);
                cardData.hp = baseHealth;
            }
            if (cardData.hasAttack)
            {
                var baseAttack = Geom(cardData.damage + 0.5f);
                cardData.damage = baseAttack;
            }
            if (cardData.counter != 0)
            {
                cardData.counter = Stoch(cardData.counter);
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

        private int Stoch(int counter)
        {
            float reroll = Dead.Random.Range(0f, 1f);
            while(reroll < 0.6f)
            {
                var r = Dead.Random.Range(0f, 1f);
                if(r < (counter+1f)/(2f*counter))
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
