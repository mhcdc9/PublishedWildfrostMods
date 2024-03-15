using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.UI.CanvasScaler;
using UnityEngine.UI;

namespace TestMod
{
    public class Class1 : WildfrostMod
    {
        public Class1(string modDirectory) : base(modDirectory)
        {

        }

        protected override void Load()
        {
            base.Load();
            Events.OnEntityChosen += ModifyName;
            //Events.OnEntityChosen += ModifyName;
            UnityEngine.Debug.Log("Mod Loaded. -Michael.");
        }

        private void ModifyName(Entity entity)
        {
            string newName = "Guka Guka";
            UnityEngine.Debug.Log("[Michael] Trying to modify name in deckpack.");
            if (Events.CheckRename(ref entity, ref newName))
            {
                entity.data.forceTitle = newName;
                Card card = entity.gameObject.GetComponent<Card>();
                if (card != null)
                {
                    card.SetName(newName);
                    UnityEngine.Debug.Log("[Michael] Name set.");
                }
                Events.InvokeRename(entity, newName);
                UnityEngine.Debug.Log("[Michael] Card renamed in deck.");

                List<CardUpgradeData> g = AddressableLoader.GetGroup<CardUpgradeData>("CardUpgradeData");

            }
        }

        protected override void Unload()
        {
            base.Unload();
            Events.OnEntityChosen -= ModifyName;
            //Events.OnEntityChosen -= ModifyName;
            UnityEngine.Debug.Log("Mod Unloaded. -Michael.");
        }

        private void EnemyRename(Entity entity)
        {
            UnityEngine.Debug.Log(entity.name);
            UnityEngine.Debug.Log(entity.owner.name);
            //if (entity.owner.name == "Enemy")
            //{
                float r = Dead.Random.Range(1, 100);
                Card card = entity.gameObject.GetComponent<Card>();
                card.SetName(card.name + ": " + r.ToString());
            entity.data.name = "Boop.";
            //}      
            
        }

        public override List<T> AddAssets<T, Y>()
        {
            var typeName = typeof(Y).Name;
            switch (typeName)
            {
                case "CardData": return AddCards().Cast<T>().ToList();
            }
            return base.AddAssets<T, Y>();
        }

        public override string GUID => "mhcdc9.wildfrost.recurnames";
        public override string[] Depends => new string[] { };
        public override string Title => "Renaming Enemies";
        public override string Description => "Checking how feasible renaming enemies are.";

        private List<CardDataBuilder> AddCards()
        {
            var list = new List<CardDataBuilder>();
            //Add our cards here
            var booshu = this.Get<CardData>("BerryPet");
            booshu.hp = 99;
            booshu.damage = 99;
            //
            return list;
        }
    }
}
