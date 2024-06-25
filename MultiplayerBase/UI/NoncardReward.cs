using System;
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

        private bool popped = false;
        private Vector2 offset = new Vector2(0f, 1f);
        private Vector2 defaultUpgradeDim = new Vector2(1.5f, 1.5f);
        private Vector2 defaultBellDim = new Vector2(1f, 1.5f);

        private static NoncardReward Create(Transform transform, Vector2 dim, string name, string title, string body, Sprite sprite)
        {
            GameObject obj = new GameObject("Reward: " + name);
            obj.transform.SetParent(transform, false);
            obj.AddComponent<Image>().sprite = sprite;
            obj.GetComponent<RectTransform>().sizeDelta = dim;
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
            pointerExit.callback.AddListener(b => { ncr.Unpop(); });
            trigger.triggers.Add(pointerExit);

            return ncr;
        }

        public static NoncardReward CreateUpgrade(Transform transform, Vector2 dim, string upgradeName)
        {
            CardUpgradeData cardUpgradeData = AddressableLoader.Get<CardUpgradeData>("CardUpgradeData", upgradeName);
            string title = cardUpgradeData?.title ?? "";
            string body = cardUpgradeData?.text ?? "";
            Sprite sprite = cardUpgradeData.image;
            return Create(transform, dim, upgradeName, title, body, sprite);
        }

        public static NoncardReward CreateModifier(Transform transform, Vector2 dim, string modifierName)
        {
            GameModifierData modifierData = AddressableLoader.Get<GameModifierData>("GameModifierData", modifierName);
            string title = (modifierData?.titleKey != null) ? modifierData.titleKey.GetLocalizedString() : ""; 
            string body = (modifierData?.descriptionKey != null) ? modifierData.descriptionKey.GetLocalizedString() : "";
            Sprite sprite = modifierData?.bellSprite;
            return Create(transform, dim, modifierName, title, body, sprite);
        }

        public void Pop()
        {
            if (popped) { return; }

            CardPopUp.AssignTo(GetComponent<RectTransform>(), offset.x, offset.y);
            CardPopUp.AddPanel(name, title, body);
            popped = true;
        }

        public void Unpop()
        {
            if (popped)
            {
                CardPopUp.RemovePanel(name);
                popped = false;
            }
        }
    }
}
