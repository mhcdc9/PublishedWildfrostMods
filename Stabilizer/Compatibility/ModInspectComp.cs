using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Stabilizer.Compatibility
{
    public static class ModInspectComp
    {
        public static IEnumerator RunCosmetic(WildfrostMod mod, RectTransform parent)
        {
            MethodInfo method = CompFinder.FindCompMethod(mod, "InspectCosmetic");
            if (method == null) 
            {
                IconSwivel(mod, parent);
                yield break;
            }

            if (method.ReturnType == typeof(IEnumerator))
            {
                yield return (method.Invoke(null, new object[1] { parent }) as IEnumerator);
            }
            if (method.ReturnType == typeof(void))
            {
                method.Invoke(null, new object[] { parent });
            }
            yield break;
        }

        public static void RunContent(WildfrostMod mod, RectTransform parent)
        {
            MethodInfo method = CompFinder.FindCompMethod(mod, "InspectContent");
            if (method == null) { return; }

            if (method.ReturnType == typeof(void))
            {
                method.Invoke(null, new object[] { parent });
            }
        }

        public static void IconSwivel(WildfrostMod mod, RectTransform parent)
        {
            GameObject icon = new GameObject("Wobbling Icon", new Type[] { typeof(Image) });
            icon.transform.SetParent(parent, false);
            icon.GetComponent<RectTransform>().sizeDelta = new Vector2(5, 5);
            Image image = icon.GetComponent<Image>();
            image.sprite = mod.IconSprite;
            image.color = new Color(1, 1, 1, 0.8f);
        }
    }
}
