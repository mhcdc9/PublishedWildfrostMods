using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Stabilizer
{
    public static class StabilizerEvents
    {
        public static event UnityAction<WildfrostMod, RectTransform> OnModInspect;

        public static void InvokeModInspect(WildfrostMod mod, RectTransform contentParent)
        {
            OnModInspect?.Invoke(mod, contentParent);
        }
    }
}
