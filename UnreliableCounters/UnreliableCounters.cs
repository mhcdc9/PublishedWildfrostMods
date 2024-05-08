using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WildfrostHopeMod;
using WildfrostHopeMod.Configs;
using static WildfrostHopeMod.ConfigManager;

namespace UnreliableCounters
{
    public class UnreliableCounters : WildfrostMod
    {
        [ConfigManagerTitle("Probabilistic Flavor")]
        [ConfigManagerDesc("Select how the counters should count down. (!) do not preserve averages.")]
        [ConfigOptions("All or Nothing", "Time is Relative", "Two steps forward (One step back)", "Uniform Chaos (!)", "Boring")]
        [ConfigItem("All or Nothing", "", "mode")]
        public string mode = "All or Nothing";

        [ConfigManagerTitle("Legacy RNG")]
        [ConfigManagerDesc("Legacy RNG is highly correlated. If you know how it works, it could be fun. Not recommended.")]
        [ConfigOptions("No", "Yes")]
        [ConfigItem("No", "", "mode2")]
        public string mode2 = "No";
        public UnreliableCounters(string modDirectory) : base(modDirectory)
        {
        }

        public override string GUID => "mhcdc9.wildfrost.randCounters";

        public override string[] Depends => new string[0];

        public override string Title => "Probabilistic Counters";

        public override string Description => "Counters no longer count down conventionally. In the options menu, switch from the different flavors between runs, battles, or even turns. \n\n\n\n" +
            "Flavours:\r\nAll or Nothing: The unit either triggers or it doesn't. No counting down.\r\nTime is Relative: The unit will count down (sometimes) but not necessarily by 1.\r\nTwo Steps Forward (One Step Back): The unit typically counts down by 2 but sometimes counts up by 1.\r\nUniform Chaos (!): The unit chooses a random number (uniformly from available options) to count down to.\r\nBoring: Cards count down by 1.\r\n\r\nEnjoy!\r\n-Michael";

        protected override void Load()
        {
            base.Load();
            Events.OnEntityCountDown += TriggerChance;
        }

        protected override void Unload()
        {
            base.Unload();
            Events.OnEntityCountDown -= TriggerChance;
        }

        private void TriggerChance(Entity entity, ref int amount)
        {
            int rate = entity.counter.current;
            if (rate == 0)
            {
                return;
            }
            float r;
            switch(mode)
            {
                case "All or Nothing":
                    r = RRange(0f, 1f);
                    if (r < ((float)amount) / rate)
                    {
                        amount = rate;
                    }
                    else
                    {
                        amount = 0;
                    }
                    break;
                case "Time is Relative":
                    int newAmount = 0;
                    for(int i=0; i<rate; i++)
                    {
                        r = RRange(0f, 1f);
                        if (r < ((float)amount) / rate)
                        {
                            newAmount++;
                        }
                    }
                    amount = newAmount;
                    break;
                case "Two steps forward (One step back)":
                    r = RRange(0f, 1f);
                    amount = (r < 2f / 3f) ? 2 : -1;
                    break;
                case "Uniform Chaos (!)":
                    amount = RRange(0, rate); 
                    break;
                case "Boring":
                    break;
            }
            
        }

        private float RRange(float min, float max)
        {
            float r = (mode2 == "No") ? Dead.PettyRandom.Range(min, max) : Dead.Random.Range(min, max);
            return r;
        }

        private int RRange(int min, int max)
        {
            int r = (mode2 == "No") ? Dead.PettyRandom.Range(min, max) : Dead.Random.Range(min, max);
            return r;
        }
    }
}
