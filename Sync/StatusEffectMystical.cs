using MultiplayerBase;
using MultiplayerBase.Handlers;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sync
{
    public class StatusEffectMystical : StatusEffectApplyX
    {
        public bool holdPlayOnHand = false;
        public override void Init()
        {
            MultEvents.OnBattleViewerOpen += ViewerOpen;
            MultEvents.OnBattleViewerClose += ViewerClose;
            //HandlerBattle.OnSendCardToPlay += Play;
            MultEvents.OnSentCardToPlay += GainZoomlin;
        }

        public void OnDestroy()
        {
            MultEvents.OnBattleViewerOpen -= ViewerOpen;
            MultEvents.OnBattleViewerClose -= ViewerClose;
            //HandlerBattle.OnSendCardToPlay -= Play;
            MultEvents.OnSentCardToPlay -= GainZoomlin;
        }

        public void ViewerOpen(Friend friend)
        {
            if (target != null && References.Battle != null && target.IsAliveAndExists())
            {
                holdPlayOnHand = target.data.canPlayOnHand;
                target.data.canPlayOnHand = false;
                target.display.hover.controller = HandlerBattle.instance.CB;
            }
        }

        public void ViewerClose(Friend friend)
        {
            if (target != null && References.Battle != null && target.IsAliveAndExists())
            {
                target.data.canPlayOnHand = holdPlayOnHand;
                target.display.hover.controller = References.Battle.playerCardController;
            }
        }

        public void GainZoomlin(Friend friend, Entity entity)
        {
            if (entity == target)
            {
                target.StartCoroutine(GainZoomlinRoutine());
            }
        }

        public IEnumerator GainZoomlinRoutine()
        {
            yield return Run(GetTargets(), 1);
            //yield return Remove();
        }

        public void Play(Friend friend, Entity entity)
        {
            if (entity == target)
            {
                target.StartCoroutine(Run(GetTargets(), 1));
            }
        }
    }
}
