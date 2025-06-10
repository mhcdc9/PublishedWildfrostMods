using MultiplayerBase.Handlers;
using MultiplayerBase.StatusEffects;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerBase.Ongoing
{
    public static class OngoingEffectSystem
    {
        public static Dictionary<Friend, List<Entry>> activeEffects = new Dictionary<Friend, List<Entry>>();
        public static Dictionary<string, Entry> baseEntries = new Dictionary<string, Entry>();

        public static List<Agent> agents = new List<Agent>();
        public static int idMax = 100;

        public static void Enable()
        {
            foreach(Friend f in HandlerSystem.friends)
            {
                activeEffects[f] = new List<Entry>();
            }
            Entry entry = new OngoingEntryStatusEffect(MultiplayerMain.instance.Get<StatusEffectData>("Temporary Barrage"), StatusEffectApplyX.ApplyToFlags.AlliesInRow);
            baseEntries["Quantum Gachapomper"] = entry;
            StatusEffectOngoingAgent agent = ScriptableObject.CreateInstance<StatusEffectOngoingAgent>();
            agent.name = "Quantum Gachapomper";
            agent.entryName = "Quantum Gachapomper";
            agent.targetConstraints = new TargetConstraint[0];
            agent.type = "";
            AddressableLoader.AddToGroup<StatusEffectData>("StatusEffectData", agent);
            StatusEffectWhileActiveX gacha = (MultiplayerMain.instance.Get<StatusEffectData>("While Active Barrage To AlliesInRow") as StatusEffectWhileActiveX);
            gacha.effectToApply = agent;
            gacha.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
        }

        //ONGOING! ACTIVATE! [EntryName! Id! Amount! ExtraInfo]
        public static void ActivateEffect(Friend f, string[] messages)
        {
            UnityEngine.Debug.Log($"[Multiplayer] Activating {messages[0]}");
            if (!baseEntries.ContainsKey(messages[0])) return;

            Entry entry = baseEntries[messages[0]].Create(messages);
            activeEffects[f].Add(entry);
            entry.Activate(int.Parse(messages[2]), messages[3]);
        }

        //ONGOING! UPDATE! [EntryName1! Id! Amount! ExtraInfo]! [EntryName2...]! ...
        public static void UpdateEffects(Friend f, string[] messages)
        {
            string[] entryData;
            for (int i = 2; i < messages.Length; i++)
            {
                entryData = HandlerSystem.DecodeMessages(messages[i]);
                UnityEngine.Debug.Log($"[Multiplayer] Updating {entryData[0]}");
                Entry entry = activeEffects[f].FirstOrDefault(e => e.Equals(entryData));
                if (entry == null && int.Parse(entryData[2]) > 0)
                {
                    ActivateEffect(f, entryData);
                    continue;
                }
                else if (entry.ChangeAmount(int.Parse(entryData[2]), entryData[3]))
                {
                    activeEffects[f].Remove(entry);
                }
            }
        }

        public static void Deactivate(Friend f)
        {
            for(int i = activeEffects.Count-1; i >= 0; i--)
            {
                activeEffects[f][i].Deactivate();
            }
            activeEffects.Clear();
        }

        public interface Agent
        {
            string UpdateOngoing();
        }

        public interface Entry
        {
            //EntryName! Id! Amount! ExtraInfo
            Entry Create(string[] info);
            bool Equals(string[] data);
            void Activate(int amount, string extraInfo);

            bool ChangeAmount(int newAmount, string extraInfo);

            void Deactivate();
        }
    }
}
