using HarmonyLib;
using MultiplayerBase.Battles;
using MultiplayerBase.Handlers;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Net = MultiplayerBase.Handlers.HandlerSystem;

namespace Sync
{
    [HarmonyPatch]
    public static class GaidenSystem
    {
        static bool canAcceptHelp;

        public static event UnityAction<string> OnRecallWanderer;
        public static void Enable()
        {
            Events.OnBattlePreTurnStart += EnlistHelp;
        }

        public static void Disable()
        {
            Events.OnBattlePreTurnStart -= EnlistHelp;
        }

        public static void EnlistHelp(int turn)
        {
            if (turn == 0)
            {
                canAcceptHelp = true;
                string s = Net.ConcatMessage(false, "GAIDEN", "ASK", Campaign.FindCharacterNode(References.Player).tier.ToString());
                Net.SendMessageToAllOthers("SYNC", s);
            }

            //And Offer Help Too
            IEnumerable<CardData> list = References.PlayerData.inventory.reserve.InRandomOrder();
            foreach (CardData card in list)
            {
                if (card.injuries.Count == 0 && card.traits.FirstOrDefault(s => s.data.name == "mhcdc9.wildfrost.sync.Gaiden") != null)
                {
                    if (card.customData == null)
                    {
                        card.customData = new Dictionary<string, object>();
                    }
                    int lvl = Campaign.FindCharacterNode(References.Player).id;
                    if (card.customData.ContainsKey("GaidenLvl") && (int)card.customData["GaidenLvl"] == lvl)
                    {
                        continue;
                    }
                    string s = Net.ConcatMessage(true, "GAIDEN", "OFFER", card.id.ToString(), CardEncoder.Encode(card));
                    Net.SendMessageToAllOthers("SYNC", s);
                }
            }
        }

        public static void GAIDEN_Handler(Friend f, string[] m)
        {
            //m[0] = "GAIDEN"
            Debug.Log($"[Sync] GAIDEN {m[1]}");
            switch(m[1])
            {
                case "ASK":
                    SendHelp(f, m[2]);
                    break;
                case "OFFER":
                    ConsiderOffer(f, m[2], m[3]);
                    break;
                case "ACCEPT":
                    UpdateCustomData(m[2], m[3]);
                    break;
                case "LEAVE":
                    RecallWanderer(f, m[2]);
                    break;
                case "INJURE":
                    WoundWanderer(f, m[2]);
                    break;
            }
        }

        /*[HarmonyPostfix]
        [HarmonyPatch(typeof(CardControllerDeck), nameof(CardControllerDeck.MoveToReserve))]
        public static void ForceHelp(Entity entity)
        {
            if (entity.statusEffects.FirstOrDefault(s => s.type == "mhcdc9.gaiden") != null)
            {
                string s = Net.ConcatMessage(true, "GAIDEN", "OFFER", entity.data.id.ToString(), CardEncoder.Encode(entity.data));
                Net.SendMessageToAllOthers("SYNC", s);
            }
        }*/

        public static void SendHelp(Friend f, string data)
        {
            if (References.Battle == null) { return; }

            if (References.PlayerData?.inventory?.reserve != null)
            {
                IEnumerable<CardData> list = References.PlayerData.inventory.reserve.InRandomOrder();
                foreach (CardData card in list)
                {
                    if (card.injuries.Count == 0 && card.traits.FirstOrDefault(s => s.data.name == "mhcdc9.wildfrost.sync.Gaiden") != null)
                    {
                        if (card.customData == null)
                        {
                            card.customData = new Dictionary<string, object>();
                        }
                        int tier = int.Parse(data);
                        if (card.customData.ContainsKey("GaidenLvl") && (int)card.customData["GaidenLvl"] >= tier)
                        {
                            continue;
                        }
                        string s = Net.ConcatMessage(true, "GAIDEN", "OFFER", card.id.ToString(), CardEncoder.Encode(card));
                        Net.SendMessage("SYNC", f, s);
                    }
                }
            }
        }

        public static void ConsiderOffer(Friend f, string id, string entityString)
        {
            if (Battle.instance != null && Battle.instance.phase != Battle.Phase.End && canAcceptHelp)
            {
                canAcceptHelp = false;
                CardData card = CardEncoder.DecodeData(Net.DecodeMessages(entityString));
                for(int i=0; i<card.traits.Count; i++)
                {
                    if (card.traits[i].data.name == "mhcdc9.wildfrost.sync.Gaiden")
                    {
                        card.traits[i].data = SyncMain.Instance.Get<TraitData>("SideQuest");
                    }
                }

                if (card.customData == null)
                {
                    card.customData= new Dictionary<string, object>();
                    card.customData["ActualId"] = f.Name + id;
                }
                HandlerBattle.instance.Queue(new ActionGainCardToHand(card, ActionGainCardToHand.Location.PlayerBoard));
                string s = Net.ConcatMessage(true, "GAIDEN", "ACCEPT", id, Campaign.FindCharacterNode(References.Player).tier.ToString());
                Net.SendMessage("SYNC", f, s);
            }
        }

        public static void UpdateCustomData(string idString, string tierString)
        {
            ulong id = ulong.Parse(idString);
            //int tier = int.Parse(tierString);
            int lvl = Campaign.FindCharacterNode(References.Player).id;
            if (References.PlayerData?.inventory?.reserve != null)
            {
                References.PlayerData.inventory.reserve.Do(c =>
                {
                    if (c.id == id)
                    {
                        if (c.customData == null)
                        {
                            c.customData = new Dictionary<string, object>();
                        }
                        c.customData["GaidenLvl"] = lvl;
                    }
                });
                References.PlayerData.inventory.deck.Do(c =>
                {
                    if (c.id == id)
                    {
                        if (c.customData == null)
                        {
                            c.customData = new Dictionary<string, object>();
                        }
                        c.customData["GaidenLvl"] = lvl;
                    }
                });
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CardControllerDeck), nameof(CardControllerDeck.MoveToDeck))]
        public static void ForceLeave(Entity entity)
        {
            if (entity.statusEffects.FirstOrDefault(s => s.type == "mhcdc9.gaiden") != null)
            {
                string s = Net.ConcatMessage(false, "GAIDEN", "LEAVE", HandlerSystem.self.Name + entity.data.id);
                Net.SendMessageToAllOthers("SYNC", s);
            }
        }

        public static void RecallWanderer(Friend f, string nameId)
        {
            ActionQueue.Add(new ActionSequence(RecallWanderer2(nameId))
            {
                note = $"Recalling wanderer {nameId}"
            });
        }

        public static IEnumerator RecallWanderer2(string nameId)
        {
            OnRecallWanderer?.Invoke(nameId);
            yield break;
        }

        public static void WoundWanderer(Friend f, string nameId)
        {
            InjurySystem system = Campaign.instance?.systems?.GetComponent<InjurySystem>();
            if (system == null || !system.enabled) { return; }

            if (References.PlayerData?.inventory?.reserve != null)
            {
                References.PlayerData.inventory.reserve.Do(c =>
                {
                    if (Net.self.Name + c.id.ToString() == nameId)
                    {
                        system.Injure(c);
                    }
                });
                References.PlayerData.inventory.deck.Do(c =>
                {
                    if (Net.self.Name + c.id.ToString() == nameId)
                    {
                        system.Injure(c);
                    }
                });
            }
        }
    }
}
