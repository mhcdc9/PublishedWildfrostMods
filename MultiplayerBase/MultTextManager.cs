using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace MultiplayerBase
{
    public class MultTextManager : MonoBehaviour
    {
        private class Entry
        {
            public string s;
            public float size;
            public Color Color;
            public float duration;
            public bool fading = false;

            internal Entry(string s, float size, Color color, float duration)
            {
                this.s = s;
                this.size = size;
                Color = color;
                this.duration = duration;
            }
        }

        private static MultTextManager instance;
        protected TextMeshProUGUI text => GetComponent<TextMeshProUGUI>();

        private static Entry current = null;
        
        public void Awake()
        {
            instance = this;
            text.horizontalAlignment = HorizontalAlignmentOptions.Center;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            text.fontSize = 0.4f;
            text.outlineWidth = 0.1f;
            text.color = UnityEngine.Color.white;
            text.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 1);
            text.gameObject.transform.SetParent(GameObject.Find("Canvas/SafeArea").transform);
            text.gameObject.transform.localPosition = new Vector3(0, 5, 0);
        }

        public void Update()
        {
            if (current == null)
            {
                this.enabled = false;
                return;
            }
            current.duration -= Time.unscaledDeltaTime;
            if (current.duration < -5f)
            {
                current = null;
            }
            else if (current.duration < 0 && !current.fading)
            {
                current.fading = true;
                text.CrossFadeAlpha(0, 4.5f, true);
            }
        }

        public static void AddEntry(string s, float size, Color color, float duration)
        {
            current = new Entry(s, size, color, duration);
            instance.text.text = s;
            instance.text.fontSize = size;
            instance.text.color = color;
            instance.enabled = true;
            instance.text.CrossFadeAlpha(1, 0.1f, true);
        }
    }
}
