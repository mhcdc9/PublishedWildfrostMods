using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks;
using MultMain = MultiplayerBase.MultiplayerMain;
using Net = MultiplayerBase.Handlers.HandlerSystem;
using MultiplayerBase.Handlers;

namespace Sync
{
    public class SyncMain : WildfrostMod
    {
        public static Dictionary<Friend, bool> synced;
        public static bool sync = false;

        private List<StatusEffectDataBuilder> effects = new List<StatusEffectDataBuilder>();
        private List<KeywordDataBuilder> keywords = new List<KeywordDataBuilder>();
        public SyncMain(string modDirectory) : base(modDirectory)
        {
        }

        public override string GUID => "mhcdc9.wildfrost.sync";

        public override string[] Depends => new string[0];

        public override string Title => "Sync Test";

        public override string Description => "Can you deal with a problem that spans two boards?";

        public CardData.StatusEffectStacks SStack(string name, int count) => new CardData.StatusEffectStacks(Get<StatusEffectData>(name), count);
        protected override void Load()
        {
            CreateModAssets();
            Events.OnBattleTurnStart += CheckSync;
            synced = new Dictionary<Friend, bool>();
            Net.HandlerRoutines.Add("SYNC", SYNC_Handler);
            MultMain.Finalized += TrackSynced;
            base.Load();
        }

        protected override void Unload()
        {
            Events.OnBattlePreTurnStart -= CheckSync;
            Net.HandlerRoutines.Remove("SYNC");
            MultMain.Finalized -= TrackSynced;
            base.Unload();
        }

        public void CreateModAssets()
        {
            keywords.Add(Extensions.CreateBasicKeyword(this, "sync", "Sync", "Immediately gain an effect when certain conditions are met elsewhere..."));

            effects.Add(
                new StatusEffectDataBuilder(this)
                .CreateSyncEffect<StatusEffectSync>("Sync Attack", "<keyword=mhcdc9.wildfrost.sync.sync>: Gain <+{a}><keyword=attack>", "", Get<StatusEffectData>("Ongoing Increase Attack"))
                );
        }

        public override List<T> AddAssets<T, Y>()
        {
            var typeName = typeof(Y).Name;
            switch (typeName)
            {
                case nameof(StatusEffectData):
                    return effects.Cast<T>().ToList();
                case nameof(KeywordData):
                    return keywords.Cast<T>().ToList();
                default:
                    return null;
            }
        }

        internal void TrackSynced()
        {
            foreach (Friend friend in Net.friends)
            {
                synced.Add(friend, false);
            }
        }

        internal static void CheckSync(int turnCount)
        {
            if (StatusEffectSync.SyncOnScreen > 0)
            {
                synced[HandlerSystem.self] = true;
                Net.SendMessageToAllOthers("SYNC", "T");
            }
            else if (synced[Net.self] == true)
            {
                synced[HandlerSystem.self] = false;
                Net.SendMessageToAllOthers("SYNC", "F");
            }
        }

        internal static void SYNC_Handler(Friend friend, string message)
        {
            switch(message)
            {
                case "T":
                    synced[friend] = true;
                    if (!sync)
                    {
                        sync = true;
                        ActionSync action = new ActionSync(false);
                        HandlerBattle.TryAction(action);
                    }
                    break;
                case "F":
                    synced[friend] = false;
                    if (CheckChangedToFalse())
                    {
                        sync = false;
                        ActionSync action = new ActionSync(true);
                        HandlerBattle.TryAction(action);
                    }
                    break;
            }
        }

        internal static void SYNC_Status(Friend friend)
        {
            string s = synced[Net.self] ? "T" : "F";
            Net.SendMessage("SYNC", friend, s);
        }

        internal static bool CheckChangedToFalse()
        {
            if (sync)
            {
                foreach(Friend friend in Net.friends)
                {
                    if (friend.Id != Net.self.Id && synced[friend])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
