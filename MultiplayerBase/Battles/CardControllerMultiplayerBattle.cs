using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MultiplayerBase.Handlers;
using MultiplayerBase.UI;
using Steamworks;

namespace MultiplayerBase.Battles
{
    internal class CardControllerMultiplayerBattle : CardControllerBattle
    {
        public override void Press()
        {
            if (pressEntity == null) { return; }

            if (pressEntity.containers != null && pressEntity.containers.Length > 0 && pressEntity.containers[0] is OtherCardViewer)
            {
                HandlerInspect.SelectPing(pressEntity);
                return;
            }

            if (pressEntity.owner == owner)
            {
                
                Debug.Log("Pressing [" + pressEntity.name + "]");
                if (TryDrag(pressEntity))
                {
                    UnHover(pressEntity);
                    NavigationState.Start(new NavigationStateMultiplayerCard(pressEntity));
                }
            }
        }

        public override void Release()
        {
            if (!dragging)
            {
                return;
            }

            bool retainPosition = false;
            bool retainRotation = false;
            bool retainScale = false;
            bool retainDrawOrder = false;
            if (base.enabled)
            {

                Debug.Log("[Multiplayer] Base is enabled.");
                if (InputSwitcher.justSwitched)
                {
                    dragging.TweenToContainer();
                }
                else if ((bool)hoverContainer && hoverContainer.canBePlacedOn && hoverContainer == owner.discardContainer && dragging.owner == owner)
                {
                    Debug.Log("[Multiplayer] Recalling?");
                    if (dragging.CanRecall())
                    {
                        ActionMove action = new ActionMove(dragging, hoverContainer);
                        if (Events.CheckAction(action))
                        {
                            Events.InvokeDiscard(dragging);
                            if (Battle.IsOnBoard(dragging))
                            {
                                owner.freeAction = true;
                            }

                            ActionQueue.Add(action);
                            ActionQueue.Add(new ActionEndTurn(owner));
                            base.enabled = false;
                            retainDrawOrder = true;
                        }
                    }

                    hoverContainer.UnHover();
                }
                else
                {
                    switch (dragging.data.playType)
                    {
                        case Card.PlayType.Play:
                            Debug.Log("[Multiplayer] Play!");
                            if (!dragging.NeedsTarget)
                            {
                                Debug.Log("[Multiplayer] Targetless?");
                                if (!hoverContainer || !dragging.InContainer(hoverContainer))
                                {
                                    ActionQueue.Add(new ActionSendCardToPlay(dragging, (Friend)HandlerBattle.friend, 0, ActionSendCardToPlay.TargetType.None));
                                }
                            }
                            else if (dragging.data.playOnSlot)
                            {
                                Debug.Log("[Multiplayer] Slots?");
                                CardContainer cardContainer = (dragging.targetMode.TargetRow ? hoverContainer : hoverSlot);
                                if (!dragging.CanPlayOn(cardContainer))
                                {
                                    break;
                                }

                                ActionTriggerAgainst action3 = new ActionTriggerAgainst(dragging, owner.entity, null, cardContainer);
                                if (Events.CheckAction(action3))
                                {
                                    if (ShoveSystem.Active)
                                    {
                                        ShoveSystem.Fix = true;
                                    }

                                    ActionQueue.Add(action3);
                                    ActionQueue.Add(new ActionReduceUses(dragging));
                                    ActionQueue.Add(new ActionResetOffset(dragging));
                                    ActionQueue.Add(new ActionEndTurn(owner));
                                    base.enabled = false;
                                    retainPosition = true;
                                    retainRotation = true;
                                    retainDrawOrder = true;
                                }
                            }
                            else if (dragging.targetMode.TargetRow)
                            {
                                Debug.Log("[Multiplayer] Barrage?");
                                if (dragging.CanPlayOn(hoverContainer))
                                {
                                    ActionQueue.Add(new ActionSendCardToPlay(dragging, (Friend)HandlerBattle.friend, HandlerBattle.instance.ConvertToID(hoverContainer), ActionSendCardToPlay.TargetType.Container));
                                }
                            }
                            else if ((bool)hoverEntity && hoverEntity != dragging)
                            {
                                Debug.Log("[Multiplayer] Standard!");
                                ActionQueue.Add(new ActionSendCardToPlay(dragging, (Friend)HandlerBattle.friend, HandlerInspect.FindTrueID(hoverEntity), ActionSendCardToPlay.TargetType.Entity));
                            }

                            break;
                    }
                }

                if (ActionQueue.Empty)
                {
                    Debug.Log("[Multiplayer] Tweening!");
                    dragging.TweenToContainer();
                }
            }
            Debug.Log("[Multiplayer] Ending!");

            TweenUnHover(dragging, retainScale, retainPosition, retainRotation, retainDrawOrder);
            DragEnd();
            UnHover();
            //base.Release();
        }
    }
}
