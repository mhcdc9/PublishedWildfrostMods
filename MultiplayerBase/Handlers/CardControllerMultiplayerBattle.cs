using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerBase.Handlers
{
    internal class CardControllerMultiplayerBattle : CardControllerBattle
    {
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
                        case Card.PlayType.Place:
                            Debug.Log("[Multiplayer] Place Type?");
                            if (!hoverSlot || dragging.actualContainers.Contains(hoverSlot) || !hoverSlot.canBePlacedOn || !(hoverSlot.owner == dragging.owner))
                            {
                                break;
                            }

                            if (hoverSlot.Count < hoverSlot.max)
                            {
                                ActionMove action6 = new ActionMove(dragging, hoverSlot);
                                if (Events.CheckAction(action6))
                                {
                                    bool flag = Battle.IsOnBoard(dragging) && Battle.IsOnBoard(hoverSlot.Group);
                                    Events.InvokeEntityPlace(dragging, new CardContainer[1] { hoverSlot }, flag);
                                    ActionQueue.Add(action6);
                                    ActionQueue.Add(new ActionEndTurn(owner));
                                    if (flag)
                                    {
                                        owner.freeAction = true;
                                    }

                                    base.enabled = false;
                                }
                            }
                            else
                            {
                                if (!ShoveSystem.CanShove(hoverSlot.GetTop(), dragging, out var shoveData))
                                {
                                    break;
                                }

                                ActionMove action7 = new ActionMove(dragging, hoverSlot);
                                if (Events.CheckAction(action7))
                                {
                                    bool flag2 = Battle.IsOnBoard(dragging) && Battle.IsOnBoard(hoverSlot.Group);
                                    ShoveSystem.Fix = true;
                                    Events.InvokeEntityPlace(dragging, new CardContainer[1] { hoverSlot }, flag2);
                                    ActionQueue.Add(new ActionShove(shoveData));
                                    ActionQueue.Add(action7);
                                    ActionQueue.Add(new ActionEndTurn(owner));
                                    if (flag2)
                                    {
                                        owner.freeAction = true;
                                    }

                                    base.enabled = false;
                                }
                            }

                            break;
                        case Card.PlayType.Play:
                            Debug.Log("[Multiplayer] Play!");
                            if (!dragging.NeedsTarget)
                            {
                                Debug.Log("[Multiplayer] Targetless?");
                                if (!hoverContainer || !dragging.InContainer(hoverContainer))
                                {
                                    ActionTrigger action2 = new ActionTrigger(dragging, owner.entity);
                                    if (Events.CheckAction(action2))
                                    {
                                        ActionQueue.Add(action2);
                                        ActionQueue.Add(new ActionReduceUses(dragging));
                                        ActionQueue.Add(new ActionResetOffset(dragging));
                                        ActionQueue.Add(new ActionEndTurn(owner));
                                        base.enabled = false;
                                        retainRotation = true;
                                        retainDrawOrder = true;
                                        dragging.RemoveFromContainers();
                                    }
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
                                    ActionTriggerAgainst action4 = new ActionTriggerAgainst(dragging, owner.entity, null, hoverContainer);
                                    if (Events.CheckAction(action4))
                                    {
                                        ActionQueue.Add(action4);
                                        ActionQueue.Add(new ActionReduceUses(dragging));
                                        ActionQueue.Add(new ActionResetOffset(dragging));
                                        ActionQueue.Add(new ActionEndTurn(owner));
                                        base.enabled = false;
                                        retainPosition = true;
                                        retainRotation = true;
                                        retainDrawOrder = true;
                                    }
                                }
                            }
                            else if ((bool)hoverEntity && hoverEntity != dragging)
                            {
                                Debug.Log("[Multiplayer] Standard!");
                                ActionTriggerAgainst action5 = new ActionTriggerAgainst(dragging, owner.entity, hoverEntity, null);
                                if (Events.CheckAction(action5))
                                {
                                    Debug.Log("[Multiplayer] Running!");
                                    HandlerSystem.SendMessage("CHT", HandlerSystem.self, $"Playing {dragging.data.title} on {hoverEntity.data.title}");
                                    /*
                                    ActionQueue.Add(action5);
                                    ActionQueue.Add(new ActionReduceUses(dragging));
                                    ActionQueue.Add(new ActionResetOffset(dragging));
                                    ActionQueue.Add(new ActionEndTurn(owner));
                                    base.enabled = false;
                                    retainPosition = true;
                                    retainRotation = true;
                                    retainDrawOrder = true;
                                    */
                                }
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
