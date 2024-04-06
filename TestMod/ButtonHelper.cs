using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TestMod
{
    internal class ButtonHelper : Button
    {
        private StatusIcon icon => GetComponent<StatusIcon>();

        private Entity entity => icon.target;
        private void Awake()
        {
            onClick.AddListener(OnClick);
            base.Awake();
        }

        private void OnClick()
        {
            if (entity != null)
            {
                StatusEffectData trigger = AddressableLoader.Get<StatusEffectData>("StatusEffectData", "Trigger (High Prio)");
                StartCoroutine(StatusEffectSystem.Apply(entity, entity, trigger, 1));
            }
            else
            {
                Debug.Log($"[Test] No entity.");
            }
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            Events.OnCheckEntityDrag += disableDrag;
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            Events.OnCheckEntityDrag -= disableDrag;
        }

        private void disableDrag(ref Entity arg0, ref bool arg1)
        {
            if (arg0 != entity)
            {
                Events.OnCheckEntityDrag -= disableDrag;
                return;
            }
            arg1 = false;
        }
    }
}
