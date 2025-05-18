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
        public static float delay = 0.5f;
        public ActionBattleViewer() : base()
        {
            note = "Blocking the ActionQueue :)";
        }

        public override IEnumerator Run()
        {
            yield return new WaitUntil(() => !HandlerBattle.instance.Blocking);

            //MultTextManager.AddEntry($"Tasks Unfinished: {HandlerBattle.instance.updateTasks}", 0.4f, Color.white, 0f);

            float timer = 0f;
            while (timer < delay && HandlerBattle.instance.updateTasks > 0)
            {
                MultiplayerMain.textElement.text = $"Tasks Unfinished: {HandlerBattle.instance.updateTasks}";
                yield return null;
                timer += Time.deltaTime;
            }
            if (timer >= delay)
            {
                MultTextManager.AddEntry($"Tasks Still Unfinished: {HandlerBattle.instance.updateTasks} ({timer}s)", 0.6f, Color.yellow, 100f);
            }

            HandlerBattle.instance.Clear();
            for (int i = 0; i < HandlerBattle.actions.Count; i++)
            {
                ActionQueue.Add(HandlerBattle.actions[i]);
            }
            HandlerBattle.actions.Clear();
        }
    }
}
