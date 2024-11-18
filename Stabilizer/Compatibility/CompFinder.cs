using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Stabilizer.Compatibility
{
    internal static class CompFinder
    {
        internal static Type FindCompClass(WildfrostMod mod)
        {
            Assembly assembly = mod.GetType().Assembly;
            return assembly.GetTypes().FirstOrDefault(t => t.Name == "ModComp" || t.GetCustomAttribute(typeof(ModComp)) != null);
        }

        internal static MethodInfo FindCompMethod(WildfrostMod mod, string methodName)
        {
            Type type = FindCompClass(mod);
            if (type == null) { return null; }
            return type.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(m => m.Name == methodName || m.GetCustomAttributes<ModComp>().Any(a => a.alias.Contains(methodName)));
        }
    }
}
