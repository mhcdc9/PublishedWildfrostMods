using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WildfrostHopeMod;

namespace UnreliableCounters
{
    public class UnreliableCounters : WildfrostMod
    {
        [ConfigManagerTitle("Probabilistic Flavor")]
        [ConfigManagerDesc("Select how the counters should count down. (!) do not preserve averages.")]
        [ConfigOptions("All or Nothing", "Time is Relative", "Two steps forward (One step back)", "Uniform Chaos (!)", "Boring")]
        [ConfigItem("All or Nothing", "Mode")]
        public string mode = "All or Nothing";
        public UnreliableCounters(string modDirectory) : base(modDirectory)
        {
        }

        public override string GUID => "mhcdc9.wildfrost.randCounters";

        public override string[] Depends => new string[0];

        public override string Title => "Probabilistic Counters";

        public override string Description => "Counters no longer count down conventionally. In the options menu, switch from the different flavors between runs, battles, or even turns.";

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
                    r = Dead.Random.Range(0f, 1f);
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
                        r = Dead.Random.Range(0f, 1f);
                        if (r < ((float)amount) / rate)
                        {
                            newAmount++;
                        }
                    }
                    amount = newAmount;
                    break;
                case "Two steps forward (One step back)":
                    r = Dead.Random.Range(0f, 1f);
                    amount = (r < 2f / 3f) ? 2 : -1;
                    break;
                case "Uniform Chaos (!)":
                    amount = Dead.Random.Range(0, rate); 
                    break;
                case "Boring":
                    break;
            }
            
        }
    }
}
