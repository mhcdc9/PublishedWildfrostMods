using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PersistentDrek
{
    public class PersistentDrek : WildfrostMod
    {
        private DataStorage drekstats;
        private string datafile;
        private List<string> list;
        private readonly string targetName = "WoollyDrek";
        public PersistentDrek(string modDirectory) : base(modDirectory)
        {
        }

        protected override void Load()
        {
            base.Load();
            Events.OnCardDataCreated += FeedDrek;
            drekstats = new DataStorage();
            drekstats.Mod = this;
            datafile = Path.Combine(ModDirectory, "drekstats.cfg");
            drekstats.Store = new (string,string)[] { ("hp", "0"), ("damage","0") };
            if (!File.Exists(datafile))
            {
                drekstats.WriteToFile(datafile);
            }
            list =  drekstats.ReadFromFile(datafile);
            UnityEngine.Debug.Log("[[Michael]] Loaded");
            if (list.Count < 2)
            {
                drekstats.WriteToFile(datafile);
                list = drekstats.ReadFromFile(datafile);
                UnityEngine.Debug.Log("[[Michael]] Re-Loaded");
            }
        }

        protected override void Unload()
        {
            base.Unload();
            Events.OnCardDataCreated += FeedDrek;
            drekstats.Store = new (string, string)[] { ("hp", "50"), ("damage", "50") };
            drekstats.WriteToFile(datafile);
            UnityEngine.Debug.Log("[[Michael]] Unloaded");
        }

        private void FeedDrek(CardData cardData)
        {
            if (cardData.name == targetName)
            {
                int newhp = Int32.Parse(list[0].Split(':')[1]);
                int newDamage = Int32.Parse(list[1].Split(':')[1]);
                if (cardData.hp < newhp)
                {
                    cardData.hp = newhp;
                }
                if (cardData.damage < newDamage)
                {
                    cardData.damage = newDamage;
                }
                Events.OnEntityFlee += LiveDrek;
                Events.OnBattleEnd += WhereDrek;

                UnityEngine.Debug.Log("[[Michael]] Tracking Drek!");
            }
        }

        private void WhereDrek()
        {
            UnityEngine.Debug.Log("[[Michael]] Is Drek here?");
            foreach (Entity en in References.Battle.cards)
            {
                if (en.data.name == targetName)
                {
                    UnityEngine.Debug.Log("[[Michael]] Yes!");
                    LiveDrek(en);
                }
                else
                {
                    Events.OnBattleEnd -= WhereDrek;
                    Events.OnEntityFlee -= LiveDrek;
                    DeadDrek(en);
                }
            }
            UnityEngine.Debug.Log("[[Michael]] No.");
        }

        private void DeadDrek(Entity en)
        {
            if (en.name == targetName)
            {
                UnityEngine.Debug.Log("[[Michael]] The Drek is dead!");
                drekstats.Store = new (string, string)[] { ("hp", "0"),("damage","0")};
                drekstats.WriteToFile(datafile);
                list = drekstats.ReadFromFile(datafile);
            }
        }

        private void LiveDrek(Entity en)
        {
            Events.OnBattleEnd -= WhereDrek;
            Events.OnEntityFlee -= LiveDrek;
            UnityEngine.Debug.Log("[Michael] Long live the Drek!");
            drekstats.Store = new (string, string)[] { ("hp", en.hp.max.ToString()), ("damage", en.damage.max.ToString()) };
            drekstats.WriteToFile(datafile);
            list = drekstats.ReadFromFile(datafile);
        }

        public override string GUID => "mhcdc9.wildfrost.persistdrek";

        public override string[] Depends => new string[0];

        public override string Title => "Persistent Drek";

        public override string Description => "Wooly Drek's stats persists between runs.";

        private struct DataStorage
        {
            public (string key, string value)[] Store;

            public (string key, string value)[] Read;

            public WildfrostMod Mod;

            public void WriteToFile(string name)
            {
                StringBuilder stringBuilder = new StringBuilder();
                (string, string)[] store = Store;
                for (int i = 0; i < store.Length; i++)
                {
                    (string, string) tuple = store[i];
                    stringBuilder.AppendLine(tuple.Item1 + ":" + tuple.Item2);
                }

                File.WriteAllText(name, stringBuilder.ToString());
            }

            public List<string> ReadFromFile(string name)
            {
                IEnumerable<string> enumerable = File.ReadLines(name);
                foreach(string line in enumerable) 
                { 
                    UnityEngine.Debug.Log(line);
                }
                return enumerable.ToList();
                
            }
        }

    }
}
