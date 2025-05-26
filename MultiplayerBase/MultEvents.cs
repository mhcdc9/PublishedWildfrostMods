using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Events;

namespace MultiplayerBase
{
    //Ideally, all events created in this mod should be stored. Although there may be some events from Steamworks not referenced here.
    public static class MultEvents
    {
        //Handler System
        public static event UnityAction OnHandlerSystemEnabled;
        public static event UnityAction OnHandlerSystemDisabled;

        //Handler Battle
        public static event UnityAction<Friend> OnBattleViewerOpen;
        public static event UnityAction<Friend> OnBattleViewerClose;
        public static event UnityAction<Friend, Entity> OnOtherCardPlayed;
        public static event UnityAction<Friend, Entity> OnSentCardToPlay;

        //Handler Event
        public static event UnityAction<BossRewardData.Data> OnBlessingSelected;

        public static void InvokeHandlerSystemEnabled()
        {
            OnHandlerSystemEnabled?.Invoke();
        }

        public static void InvokeHandlerSystemDisabled()
        {
            OnHandlerSystemDisabled?.Invoke();
        }

        public static void InvokeBattleViewerOpen(Friend f)
        {
            OnBattleViewerOpen?.Invoke(f);
        }

        public static void InvokeBattleViewerClose(Friend f)
        {
            OnBattleViewerClose?.Invoke(f);
        }

        public static void InvokeOtherCardPlayed(Friend f, Entity entity)
        {
            OnOtherCardPlayed?.Invoke(f, entity);
        }

        public static void InvokeSentCardToPlay(Friend f, Entity entity)
        {
            OnSentCardToPlay?.Invoke(f, entity);
        }

        public static void InvokeBlessingSelected(BossRewardData.Data data)
        {
            OnBlessingSelected?.Invoke(data);
        }
    }
}
