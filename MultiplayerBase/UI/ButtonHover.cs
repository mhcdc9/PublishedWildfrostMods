using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MultiplayerBase.UI
{
    internal class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Button _button;

        public Button Button => _button ?? GetComponent<Button>();

        public Color hoverColor = Color.white;
        public Color unhoverColor = HelperUI.restingColor;

        public void Set(Button button, Color hover, Color unhover)
        {
            _button = button;
            hoverColor = hover;
            unhoverColor = unhover;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Button.interactable)
            {
                GetComponent<Image>().color = hoverColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Button.interactable)
            {
                GetComponent<Image>().color = unhoverColor;
            }
        }

        public void Enable()
        {
            //lol
        }

        public void Disable()
        {
            GetComponent<Image>().color = unhoverColor;
        }
    }
}
