using System;
using MultiplayerBase.Handlers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using MultiplayerBase.UI;
using static StatusEffectBombard;

namespace MultiplayerBase.Battles
{
    public class NavigationStateMultiplayerCard : INavigationState
    {
        public static bool warpCanPlayOnMethods = false;

        public readonly List<UINavigationItem> disabled = new List<UINavigationItem>();

        public readonly Entity entity;

        public NavigationStateMultiplayerCard(Entity entity)
        {
            this.entity = entity;
        }

        public void Begin()
        {
            warpCanPlayOnMethods = true;
            List<CardContainer> list = new List<CardContainer>();
            List<CardContainer> secondBoard = HandlerBattle.GetContainers();
            foreach (CardContainer item in from c in UnityEngine.Object.FindObjectsOfType<CardContainer>()
                                           where (bool)c.nav && c.nav.enabled
                                           select c)
            {
                if (secondBoard.Contains(item) && entity.CanPlayOn(item))
                {
                    list.Add(item);
                }
                else
                {
                    Disable(item.nav);
                }
            }

            foreach (Entity card in References.Battle.cards)
            {
                if ((bool)card.uINavigationItem && card.uINavigationItem.enabled && (entity.data.playType != Card.PlayType.Play || !entity.CanPlayOn(card)))
                {
                    Disable(card.uINavigationItem);
                }
                else if(entity.data.canPlayOnHand && card.InHand())
                {
                    Disable(card.uINavigationItem);
                }
            }

            Disable(RedrawBellSystem.nav);
            Disable(WaveDeploySystem.nav);
            if (References.Battle.playerCardController is CardControllerBattle cardControllerBattle)
            {
                UINavigationItem useOnHandAnchor = cardControllerBattle.useOnHandAnchor;
                if ((object)useOnHandAnchor != null && entity.NeedsTarget)
                {
                    Disable(useOnHandAnchor);
                }
            }

            UINavigationDefaultSystem.SetDefaultTarget(entity);
        }

        public void End()
        {
            warpCanPlayOnMethods = false;
            foreach (UINavigationItem item in disabled.Where((UINavigationItem a) => a))
            {
                item.enabled = true;
            }

            disabled.Clear();
        }

        public void Disable(UINavigationItem item)
        {
            if ((bool)item)
            {
                item.enabled = false;
                disabled.Add(item);
            }
        }
    }

#pragma warning disable IDE0051 // Remove unused private members

    [HarmonyPatch(typeof(Entity), "CanPlayOn", new Type[]
    {
        typeof(Entity),
        typeof(bool)
    })]
    internal static class PatchCanPlayOnTarget
    {
        static bool Prefix(ref bool __result, Entity __instance, Entity target)
        {
            if (target == null)
            {
                __result = false;
                return false;
            }
            if (NavigationStateMultiplayerCard.warpCanPlayOnMethods && __instance.data.playOnSlot)
            {
                //UnityEngine.Debug.Log("Slot??");
                if (target.owner == __instance.owner && Battle.IsOnBoard(target))
                {
                    //UnityEngine.Debug.Log("Success?");
                    __result = __instance.data.canPlayOnFriendly;
                    return false;
                }
            }
            return true;
        }

    }

    [HarmonyPatch(typeof(Entity), "CanPlayOn", new Type[]
    {
        typeof(CardContainer),
        typeof(bool)
    })]
    internal static class PatchCanPlayOnRow
    {
        static bool Postfix(bool __result, Entity __instance, CardContainer container, bool ignoreRowCheck)
        {
            //UnityEngine.Debug.Log("Starting...");
            if (!NavigationStateMultiplayerCard.warpCanPlayOnMethods || container == null)
            {
                //UnityEngine.Debug.Log("Unwarped.");
                return __result;
            }
            
            //UnityEngine.Debug.Log("Warped.");
            //UnityEngine.Debug.Log($"{container.name}, {container is OtherCardViewer}, {ignoreRowCheck}");
            bool flag = container is OtherCardViewer;
            if (!flag)
            {
                //UnityEngine.Debug.Log("Flagged?");
                return false;
            }
            if (__instance.data.playType == Card.PlayType.Play)
            {
                UnityEngine.Debug.Log("Play.");
                if (__instance.targetMode.TargetRow && !ignoreRowCheck)
                {
                    //UnityEngine.Debug.Log("Row.");
                    if (__instance.data.canPlayOnBoard && !__instance.data.playOnSlot)
                    {
                        //UnityEngine.Debug.Log("Board.");
                        Entity[] targets = __instance.targetMode.GetTargets(__instance, null, container);
                        if (targets == null || targets.Length <= 0)
                        {
                            //UnityEngine.Debug.Log("Empty?");
                            return false;
                        }

                        if (!(container.owner == __instance.owner))
                        {
                            //UnityEngine.Debug.Log("Success?");
                            return __instance.data.canPlayOnEnemy;
                        }
                        //UnityEngine.Debug.Log("Success!");
                        return __instance.data.canPlayOnFriendly;
                    }
                }

                if (__instance.data.playOnSlot)
                {
                    UnityEngine.Debug.Log("Slot?");
                    if (!(container.owner == __instance.owner))
                    {
                        //UnityEngine.Debug.Log("Success?");
                        return __instance.data.canPlayOnEnemy;
                    }
                    //UnityEngine.Debug.Log("Success!");
                    return __instance.data.canPlayOnFriendly;
                }
            }
            return __result;
        }
    }

    [HarmonyPatch(typeof(TargetingArrowHeadRow), "LateUpdate")]
    internal static class PatchArrows
    {
        static bool Prefix(TargetingArrowHeadRow __instance)
        {
            if (__instance.targetArrowSystem.snapToContainer is OtherCardViewer)
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(TargetingArrowHeadRow), "OnEnable")]
    internal static class PatchArrows2
    {
        static void Postfix(TargetingArrowHeadRow __instance)
        {
            if (__instance.targetArrowSystem.snapToContainer is OtherCardViewer container)
            {
                for (int i = 0; i < __instance.targets.Length;  i++)
                {
                    SpriteRenderer obj = __instance.targets[i];
                    obj.sprite = (__instance.targetArrowSystem.target.CanPlayOn(container, ignoreRowCheck: true) ? __instance.canTarget : __instance.cannotTarget);
                }
            }
        }
    }

    [HarmonyPatch(typeof(TargetingArrow), nameof(TargetingArrow.ContainerHover))]
    internal static class PatchArrows3
    {
        static void Postfix(TargetingArrow __instance, CardContainer cardContainer, TargetingArrowSystem system)
        {
            if (NavigationStateMultiplayerCard.warpCanPlayOnMethods && system.target.data.playOnSlot
                && system.target.CanPlayOn(cardContainer))
            {
                system.snapToContainer = cardContainer;
                __instance.SetStyle("TargetRow");
            }
        }
    }

    [HarmonyPatch(typeof(TargetingArrowSystem), nameof(TargetingArrowSystem.EntityHover))]
    internal static class PatchSlotArrows
    {
        static void Postfix(TargetingArrowSystem __instance, Entity entity)
        {
            if (NavigationStateMultiplayerCard.warpCanPlayOnMethods && __instance.active
                && !__instance.target.targetMode.TargetRow && __instance.target.data.playOnSlot)
            {
                __instance.hover = entity;
                __instance.currentArrow.EntityHover(entity);
            }
        }
    }
}
