using JetBrains.Annotations;
using MultiplayerBase.Handlers;
using MultiplayerBase.StatusEffects;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerBase.Battles
{
    public class ActionSendCardToPlay : PlayAction
    {
        public override bool IsRoutine => true;
        private Entity entity;
        private ulong id;
        private int slotId;
        private Friend friend;
        private TargetType type;
        private List<IAlternatePlay> alternatePlays;

        private bool runAsIntended = true;
        public enum TargetType
        {
            None,
            Entity,
            Container,
            Slot //Needs work
        }



        public ActionSendCardToPlay(Entity entity, Friend friend, object context, TargetType type)
        {
            this.entity = entity;
            this.friend = friend;
            if (context is Entity e)
            {
                if (type == TargetType.Entity)
                {
                    this.id = HandlerInspect.FindTrueID(e);
                }
                if (type == TargetType.Slot)
                {
                    this.slotId = e.containers[0].IndexOf(e);
                }
            }
            if (context is CardContainer c)
            {
                if (type == TargetType.Container)
                {
                    this.id = HandlerBattle.instance.ConvertToID(c);
                }
                if (type == TargetType.Slot)
                {
                    this.id = (ulong)c.IndexOf(null);
                }
                
            }
            this.type = type;

            alternatePlays = entity.statusEffects.OrderByDescending(s => s.eventPriority).OfType<IAlternatePlay>().ToList();

            runAsIntended = (alternatePlays.Where(s => !s.PreProcess(context, type)).Count() == 0);

            note = runAsIntended ? $"{entity.data.title} to {friend.Name}" : $"{entity.data.title}'s alternate play";
        }

        public override IEnumerator Run()
        {
            for(int i=0; i<alternatePlays.Count; i++)
            {
                if (alternatePlays[i] != null)
                {
                    yield return alternatePlays[i];
                }
            }

            if (!runAsIntended)
            {
                entity.TweenToContainer();
                yield return new WaitForSeconds(0.2f);
                yield break;

            }

            string s = CardEncoder.Encode(entity);
            yield return new WaitForSeconds(0.2f);
            //HandlerSystem.SendMessage("CHT", HandlerSystem.self, $"Playing {entity.data.title} on {friend.Name}'s Board!");
            entity.curveAnimator.Ping();
            foreach(StatusEffectData statuses in entity.statusEffects)
            {
                if (statuses is StatusEffectFreeAction f && f.RunCardPlayedEvent(entity, new Entity[0]))
                {
                    entity.StartCoroutine(f.CardPlayed(entity, new Entity[0]));
                }
            }
            yield return new WaitForSeconds(0.4f);
            yield return Sequences.CardDiscard(entity);
            switch (type)
            {
                case TargetType.None:
                    s = HandlerSystem.ConcatMessage(false, "PLAY", "NON ", entity.data.id.ToString(), s);
                    break;
                case TargetType.Entity:
                    s = HandlerSystem.ConcatMessage(false, "PLAY", $"ENT {id}", entity.data.id.ToString(), s);
                    break;
                case TargetType.Container:
                    s = HandlerSystem.ConcatMessage(false, "PLAY", $"ROW {id}", entity.data.id.ToString(), s);
                    break;
                case TargetType.Slot:
                    s = HandlerSystem.ConcatMessage(false, "PLAY", $"SLT {id} {slotId}", entity.data.id.ToString(), s);
                    break;
            }

            HandlerSystem.SendMessage("BAT", friend, s);
            MultEvents.InvokeSentCardToPlay(friend, entity);
            References.Player.handContainer.TweenChildPositions();
            yield return new WaitForSeconds(0.5f);
            ActionQueue.Add(new ActionEndTurn(References.Player));
        }
    }
}
