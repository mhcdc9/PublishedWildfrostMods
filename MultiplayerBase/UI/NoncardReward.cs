using MultiplayerBase.Handlers;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Building;

namespace MultiplayerBase.UI
{
    internal class NoncardReward : MonoBehaviour
    {
        protected internal string title = "";
        protected string body = "";
        protected string hoverSFX;
        protected string clickSFX;

        private bool popped = false;
        private Vector2 offset = new Vector2(0f, 1f);
        private Vector2 defaultUpgradeDim = new Vector2(1.5f, 1.5f);
        private Vector2 defaultBellDim = new Vector2(1f, 1.5f);

        private Vector3 baseScale = Vector3.one;
        private float focusFactor = 1.4f;
        private static float time = 0.1f;
        private static float excitingTime = 0.15f;
        private static float enableTime = 0.5f;
        private LeanTweenType excitingType = LeanTweenType.easeOutElastic;
        private LeanTweenType type = LeanTweenType.easeOutQuad;

        private static NoncardReward Create(Transform transform, Vector2 dim, string name, string title, string body, Sprite sprite)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            obj.AddComponent<Image>().sprite = sprite;
            obj.GetComponent<RectTransform>().sizeDelta = dim;
            obj.SetActive(false);
            NoncardReward ncr = obj.AddComponent<NoncardReward>();
            ncr.title = title;
            ncr.body = body;
            EventTrigger trigger = obj.AddComponent<EventTrigger>();
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener(b => { ncr.Pop(); });
            trigger.triggers.Add(pointerEnter);
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener(b => {  ncr.Unpop(); });
            trigger.triggers.Add(pointerExit);
            EventTrigger.Entry pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback.AddListener(b => { ncr.TweenOut(excitingTime); });
            trigger.triggers.Add(pointerDown);
            EventTrigger.Entry pointerClick = new EventTrigger.Entry();
            pointerClick.eventID = EventTriggerType.PointerClick;
            pointerClick.callback.AddListener(b => { ncr.Click(); });
            trigger.triggers.Add(pointerClick);

            TweenUI tween = obj.AddComponent<TweenUI>();
            tween.target = obj;
            tween.property = TweenUI.Property.Scale;
            tween.ease = LeanTweenType.easeOutBounce;
            tween.fireOnEnable = true;
            tween.duration = enableTime;
            tween.to = new Vector3(1, 1, 1);
            tween.hasFrom = true;
            tween.from = new Vector3(0, 0, 0);

            return ncr;
        }

        public static NoncardReward CreateUpgrade(Transform transform, Vector2 dim, string upgradeName)
        {
            CardUpgradeData cardUpgradeData = AddressableLoader.Get<CardUpgradeData>("CardUpgradeData", upgradeName);
            string title = cardUpgradeData?.title ?? "???";
            string body = cardUpgradeData?.text ?? "";
            Sprite sprite = cardUpgradeData.image;
            NoncardReward ncr =  Create(transform, dim, upgradeName, title, body, sprite);
            ncr.hoverSFX = "event:/sfx/inventory/charm_hover";
            ncr.clickSFX = "event:/sfx/inventory/charm_pickup";
            return ncr;
        }

        public static NoncardReward CreateModifier(Transform transform, Vector2 dim, string modifierName)
        {
            GameModifierData modifierData = AddressableLoader.Get<GameModifierData>("GameModifierData", modifierName);
            string title = (modifierData?.titleKey != null) ? modifierData.titleKey.GetLocalizedString() : ""; 
            string body = (modifierData?.descriptionKey != null) ? modifierData.descriptionKey.GetLocalizedString() : "";
            Sprite sprite = modifierData?.bellSprite;
            NoncardReward ncr =  Create(transform, dim, modifierName, title, body, sprite);
            ncr.hoverSFX = "event:/sfx/modifiers/bell_hovering";
            ncr.clickSFX = "event:/sfx/modifiers/mod_bell_ringing";
            return ncr;
        }

        public void Pop()
        {
            TweenIn();
            if (popped) { return; }

            SfxSystem.OneShot(hoverSFX);
            CardPopUp.AssignTo(GetComponent<RectTransform>(), offset.x, offset.y);
            CardPopUp.AddPanel(name, title, body);
            popped = true;
        }

        public void Unpop()
        {
            TweenOut(time);
            if (popped)
            {
                SfxSystem.OneShot(hoverSFX);
                CardPopUp.RemovePanel(name);
                popped = false;
            }
        }

        public void TweenIn()
        {
            LeanTween.cancel(gameObject);
            Tween(focusFactor * baseScale, time, type);
        }

        public void TweenOut(float time)
        {
            LeanTween.cancel(gameObject);
            Tween(baseScale, time, type);
        }

        public void Click()
        {
            SfxSystem.OneShot(clickSFX);
            TweenExciting();
            (Friend, int) decoration = NoncardViewer.FindDecoration(this);
            HandlerSystem.SendMessage("EVE",decoration.Item1, $"PING!{decoration.Item1.Id.Value}!{decoration.Item2}!Bell");
        }

        public void TweenExciting()
        {
            LeanTween.cancel(gameObject);
            Tween(focusFactor * baseScale, excitingTime, excitingType);
        }

        public void Tween(Vector3 to, float duration, LeanTweenType type)
        {
            LeanTween.scale(gameObject, to, duration).setEase(type);
        }
    }
}
