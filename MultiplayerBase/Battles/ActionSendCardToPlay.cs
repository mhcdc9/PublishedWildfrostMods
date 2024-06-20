﻿using JetBrains.Annotations;
using MultiplayerBase.Handlers;
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
        private Friend friend;
        private TargetType type;
        public enum TargetType
        {
            None,
            Entity,
            Container
        }

        public ActionSendCardToPlay(Entity entity, Friend friend, ulong id, TargetType type)
        {
            this.entity = entity;
            this.friend = friend;
            this.id = id;
            this.type = type;
        }

        public override IEnumerator Run()
        {
            string s = CardEncoder.Encode(entity, id);
            HandlerBattle.instance.ToggleViewer(friend);
            yield return new WaitForSeconds(0.2f);
            HandlerSystem.SendMessage("CHT", HandlerSystem.self, $"Playing {entity.data.title} on {friend.Name}'s Board!");
            entity.curveAnimator.Ping();
            yield return new WaitForSeconds(0.4f);
            yield return Sequences.CardDiscard(entity);
            References.Player.handContainer.TweenChildPositions();
            yield return new WaitForSeconds(0.5f);
            switch (type)
            {
                case TargetType.None:
                    s = "PLAY!NON !" + s;
                    break;
                case TargetType.Entity:
                    s = $"PLAY!ENT {id}!" + s;
                    break;
                case TargetType.Container:
                    s = $"PLAY!ROW {id}!" + s;
                    break;

            }
            HandlerSystem.SendMessage("BAT", friend, s);
        }
    }
}
