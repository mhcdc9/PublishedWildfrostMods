using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BattleEditor
{
    internal class Edit
    {
        internal WildfrostMod mod;
        internal BattleData data;
        int waveIndex;
        CardData[] enemiesAdded;

        public Edit(WildfrostMod mod, BattleData data, int waveIndex, CardData[] enemiesAdded)
        {
            this.mod = mod;
            this.data = data;
            this.waveIndex = waveIndex;
            this.enemiesAdded = enemiesAdded;
            Run();
        }

        public void Run()
        {
            BattleWavePoolData.Wave[] waves = data.pools[waveIndex].waves;
            for (int i = 0; i < waves.Length; i++)
            {
                waves[i].units.AddRange(enemiesAdded);
                waves[i].maxSize += enemiesAdded.Length;
            }
        }

        public void Undo()
        {
            if (!data) { return; }

            if (data.pools.Length <= waveIndex) { return; };

            BattleWavePoolData.Wave[] waves = data.pools[waveIndex].waves;
            enemiesAdded = enemiesAdded.Where(c => c != null).ToArray();
            for (int i = 0; i < waves.Length; i++)
            {
                RemoveAddition(waves[i]);
            }
        }

        private void RemoveAddition(BattleWavePoolData.Wave wave)
        {
            List<CardData> cards = enemiesAdded.ToList();
            cards.Reverse();
            for (int i = wave.units.Count - 1; i >= 0 && cards.Count > 0; i--)
            {
                if (wave.units[i] == null)
                {
                    continue;
                }
                if (wave.units[i].name == cards[0].name)
                {
                    cards.RemoveAt(0);
                    wave.units.RemoveAt(i);
                    continue;
                }
            }
            if (cards.Count > 0)
            {
                Debug.LogWarning($"[BattleEditor] Unclean unload occurred in [{data}], wave [{waveIndex}]");
            }
        }
    }
}
