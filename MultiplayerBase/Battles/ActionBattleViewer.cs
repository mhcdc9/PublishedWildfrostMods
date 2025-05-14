using MultiplayerBase.Handlers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerBase.Battles
{
    internal class ActionBattleViewer : PlayAction
    {
        public ActionBattleViewer() : base()
        {
            note = "Blocking the ActionQueue :)";
        }

        public override IEnumerator Run()
        {
            return new WaitUntil(() => !HandlerBattle.instance.Blocking);
        }
    }
}
