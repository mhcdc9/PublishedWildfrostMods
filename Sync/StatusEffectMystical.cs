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
        public override void Init()
        {
            HandlerBattle.OnBattleViewerOpen += ViewerOpen;
            HandlerBattle.OnBattleViewerClose += ViewerClose;
            //HandlerBattle.OnSendCardToPlay += Play;
            HandlerBattle.OnPostSendCardToPlay += GainZoomlin;
        }

        public void OnDestroy()
        {
            HandlerBattle.OnBattleViewerOpen -= ViewerOpen;
            HandlerBattle.OnBattleViewerClose -= ViewerClose;
            //HandlerBattle.OnSendCardToPlay -= Play;
            HandlerBattle.OnPostSendCardToPlay += GainZoomlin;
        }

        public void ViewerOpen(Friend friend)
        {
            if (target != null && References.Battle != null && target.IsAliveAndExists())
            {
                target.display.hover.controller = HandlerBattle.instance.CB;
            }
        }

        public void ViewerClose(Friend friend)
        {
            if (target != null && References.Battle != null && target.IsAliveAndExists())
            {
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
            yield return Remove();
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
