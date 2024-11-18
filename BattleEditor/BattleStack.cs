using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BattleEditor
{
    public class BattleStack
    {
        public enum Exclusivity
        {
            removeNone,
            removeUnmodded,
            removeAll
        }
        internal class BattleChange
        {
            internal WildfrostMod mod;
            internal BattleData data;
            internal Exclusivity exclusivity = Exclusivity.removeNone;
            internal bool active = true;
            

            public BattleChange(WildfrostMod mod, BattleData data, Exclusivity exclusivity, bool active)
            {
                this.mod = mod;
                this.data = data;
                this.exclusivity = exclusivity;
                this.active = active;
            }

            public void Run(List<BattleData> list)
            {
                switch (exclusivity)
                {
                    case Exclusivity.removeUnmodded:
                        list.RemoveAllWhere(b => b.ModAdded == null);
                        break;
                    case Exclusivity.removeAll:
                        list.RemoveAllWhere(b => b.ModAdded != mod);
                        break;
                }

                list.Add(data);
            }

            public override string ToString()
            {
                string s = $"Add {data.title}";
                if (exclusivity != Exclusivity.removeNone)
                {
                    s += " (force)";
                }
                return s;
            }
        }

        public GameMode gamemode;
        public int tier = 0;
        List<BattleChange> changes = new List<BattleChange>();

        public BattleStack(WildfrostMod mod, string gamemode, int tier)
        {
            this.gamemode = mod.Get<GameMode>(gamemode);
            if (this.gamemode == null)
            {
                throw new Exception($"Gamemode [{gamemode}] or [{mod.GUID + "." + gamemode} does not exist");
            }
            this.tier = tier;
        }

        public void Add(WildfrostMod mod, BattleData data, Exclusivity exclusivity, bool active)
        {
            changes.Add(new BattleChange(mod, data, exclusivity, active));
            Run();
        }

        public void Run()
        {
            List<BattleData> list = new List<BattleData>();
            for (int i=0; i<Main.VanillaBattles.GetLength(1); i++)
            {
                if (Main.VanillaBattles[tier,i] != "")
                {
                    list.Add(AddressableLoader.Get<BattleData>("BattleData", Main.VanillaBattles[tier, i]));
                }
            }
            for(int i=0; i<changes.Count; i++)
            {
                if (changes[i].active) { changes[i].Run(list); }
            }
            gamemode.populator.tiers[tier].battlePool = list.ToArray();
        }

        public void Output()
        {
            Debug.LogWarning($"[BattleDataEditor] ======= Tier {tier} =======");
            foreach (BattleData data in gamemode.populator.tiers[tier].battlePool)
            {
                Debug.Log($"[BattleDataEditor] {data.title}");
            }
            Debug.Log($"[BattleDataEditor] ======= End =======");
        }

        public void Remove(WildfrostMod mod)
        {
            changes.RemoveAllWhere(c => c.mod == mod);
            Run();
        }

        public void Remove(BattleData data)
        {
            changes.RemoveAllWhere(c => c.data == data);
            Run();
        }

        public void ChangeActive(BattleData data, bool active)
        {
            BattleChange change = changes.FirstOrDefault(c => c.data == data);
            if (change != null)
            {
                change.active = active;
                Run();
            }
        }
    }
}
