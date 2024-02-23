using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.UI.CanvasScaler;
using UnityEngine.UI;
using System.IO;
using System.Configuration;

namespace TestMod
{
    public class RecurringItemNames : WildfrostMod
    {
        public override string GUID => "mhcdc9.wildfrost.recurnames";
        public override string[] Depends => new string[] { };
        public override string Title => "Reappearing Item Names";
        public override string Description => "Though previous runs were lost to history, their legacy lives on through dorky nicknames.";

        private Dictionary<String, List<String>> renames = new Dictionary<String, List<String>>();
        private float threshhold = 1f;
        private float satThreshhold = 1f;
        private static string fileName = "itemRenames.txt";
        private List<string> renamedCards = new List<string>(5);
        private List<string> originalNames = new List<string>(5);
        public RecurringItemNames(string modDirectory) : base(modDirectory)
        {

        }

        protected override void Load()
        {
            base.Load();
            fileName = Path.Combine(ModDirectory, fileName);
            if (!System.IO.File.Exists(fileName))
            {
                FirstWriteRenames();
            }
            LoadRenames();
            float saturation = Math.Min(50, renames.Count) / 50f;
            satThreshhold = threshhold * (4 - 3*saturation);
            Events.OnEntityOffered += ModifyName;
            //Events.OnEntityChosen += ModifyNameAgain;
            Events.OnCampaignEnd += AddRenames;
            Events.OnBattleStart += ClearCurrentNames;
            UnityEngine.Debug.Log("[Recurnames] Mod Loaded.");
            //titleFallback
        }

        protected override void Unload()
        {
            base.Unload();
            renames.Clear();
            Events.OnEntityOffered -= ModifyName;
            //Events.OnEntityChosen -= ModifyNameAgain;
            Events.OnCampaignEnd -= AddRenames;
            Events.OnBattleStart -= ClearCurrentNames;
            UnityEngine.Debug.Log("[Recurnames] Mod Unloaded.");
        }

        private void ClearCurrentNames()
        {
            for(int i=0; i<renamedCards.Count; i++)
            {
                string name = renamedCards[i];
                CardData card = this.Get<CardData>(name);
                if(renames.ContainsKey(name))
                {
                    //renames[name].Remove(card.forceTitle);
                }
                card.forceTitle = originalNames[i];
            }
            renamedCards.Clear();
            originalNames.Clear();
        }

        private void AddRenames(Campaign.Result result, CampaignStats stats, PlayerData playerData)
        {
            CardDataList cards = playerData.inventory.deck;
            foreach(CardData card in cards)
            {
                if (card.cardType.name != "Item" && card.cardType.name != "Clunker")
                    continue;
                CardData originalCard = Get<CardData>(card.name);
                if (!originalCard || card.title != Get<CardData>(card.name).title)
                {
                    if (!renames.ContainsKey(card.name))
                    {
                        renames.Add(card.name, new List<string>());
                    }
                    if (!renames[card.name].Contains(card.title))
                    {
                        renames[card.name].Add(card.title);
                    }
                }
            }
            StoreRenames();
        }

        private void StoreRenames()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(threshhold);
            stringBuilder.AppendLine();
            foreach(string key in renames.Keys)
            {
                stringBuilder.AppendLine(key);
                foreach(string value in renames[key])
                {
                    stringBuilder.AppendLine(">" + value);
                }
            }
            System.IO.File.WriteAllText(fileName, stringBuilder.ToString());
        }

        private void FirstWriteRenames()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(0.2f);
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Sword");
            stringBuilder.AppendLine(">The Sunbringer");
            System.IO.File.WriteAllText(fileName, stringBuilder.ToString());
        }

        private void LoadRenames()
        {
            List<string> data = ReadFromFile(fileName);
            if (data.Count == 0)
            {
                UnityEngine.Debug.Log("[Recurnames] File " + fileName + " is empty.");
                return;
            }
            int startIndex = 1;
            if(float.TryParse(data[0], out var result))
            {
                threshhold = result;
            }
            else
            {
                UnityEngine.Debug.Log("[Recurnames] No threshhold value at beginning. Assuming threshhold is 1 then.");
                threshhold = 1;
                startIndex = 0;
            }
            if (data[startIndex].Length ==0 || data[startIndex][0] == '>')
            {
                UnityEngine.Debug.Log("[Recurnames] Incorrect Formatting. First name should not start with a >.");
                return;
            }
            string nameOfCard ="";
            List<String> nicknames = new List<String>();
            for(int i=startIndex; i<data.Count; i++)
            {
                string s = data[i].Trim();
                //UnityEngine.Debug.Log("[Recurnames] " + s);
                if (s.Length == 0)
                    continue;
                if (s.Substring(0,1) != ">")
                {
                    if (nameOfCard != "")
                    {
                        renames.Add(nameOfCard,nicknames);
                    }
                    nameOfCard = s;
                    nicknames = new List<String>();
                    //UnityEngine.Debug.Log("[Recurnames] Added to names.");
                }
                else
                {
                    nicknames.Add(s.Substring(1));
                    //UnityEngine.Debug.Log("[Recurnames] Added to nicknames.");
                }
            }
            if (nicknames.Count > 0)
            {
                renames.Add(nameOfCard, nicknames);
            }
        }

        private void ModifyName(Entity entity)
        {
            //UnityEngine.Debug.Log("[Recurnames] Rename time.");
            if (entity.data.cardType.name != "Item" && entity.data.cardType.name != "Clunker")
            {
                return;
            }
            if(UnityEngine.Random.Range(0f,1f) > satThreshhold)
            {
                return;
            }
            UnityEngine.Debug.Log("[Recurnames] The item/clunker " + entity.data.name + " is a long lost artifact.");
            string cardName = entity.data.name;
            string newName;
            if (renames.ContainsKey(cardName) && renames[cardName].Count > 0)
            {
                newName = renames[cardName].RandomItem();
            }
            else
            {
                return;
            }
            UnityEngine.Debug.Log("[Recurnames] The item/clunker " + cardName + " has a nickname: " + newName);
            if (Events.CheckRename(ref entity, ref newName))
            {
                entity.data.forceTitle = newName;
                Card card = entity.gameObject.GetComponent<Card>();
                if (card != null)
                {
                    card.SetName(newName);
                    //UnityEngine.Debug.Log("[Recurnames] Name of offered card set.");
                }
                //Events.InvokeRename(entity, newName);

                renamedCards.Add(entity.data.name);
                originalNames.Add(entity.data.title);

                //renameOfCards.Add(newName);
                CardData cardData = this.Get<CardData>(entity.data.name);
                cardData.forceTitle = newName;
                //UnityEngine.Debug.Log("[Recurnames] Name of offered card logged.");
            }
        }

        public List<string> ReadFromFile(string name)
        {
            IEnumerable<string> enumerable = System.IO.File.ReadLines(name);
            foreach (string line in enumerable)
            {
                UnityEngine.Debug.Log(line);
            }
            return enumerable.ToList();
        }
    }
}
