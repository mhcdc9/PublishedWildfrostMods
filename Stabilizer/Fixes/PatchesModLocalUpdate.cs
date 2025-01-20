using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.UI.Button;
using UnityEngine;
using UnityEngine.UI;

namespace Stabilizer.Fixes
{
    //Patches here are accredited to Hopeful
    [HarmonyPatch]
    public class PatchModLocalUpdate
    {
        //Updates during ctor of Stabilizer only
        public static bool enabled = false;

        // Keys: Mod.GUID
        public static Dictionary<string, DateTime> modLastLoaded = new Dictionary<string, DateTime>();
        public static Dictionary<string, (Assembly assembly, string location)> modAssemblyInfo = new Dictionary<string, (Assembly assembly, string location)>();

        /// <summary>
        /// Key: Assembly.FullName, 
        /// Value: Assembly.Location
        /// </summary>
        public static Dictionary<string, string> AssemblyLocations
            => modAssemblyInfo.ToLookup(x => x.Value.assembly.FullName,
                                        x => x.Value.location)
                              .ToDictionary(x => x.Key,
                                            x => x.First());
        // Explanation:
        // Mods can rarely have the same Assembly but different GUID
        // i.e. the GUID is dynamic, e.g. GUID => DateTime.Now
        // ToLookup combines them into 1 group before doing ToDictionary with their first representative
        // Very possible to not rely on modAssemblyInfo, but we keep synced just in case

        /// <summary>
        /// Fix wrong assembly.location if loaded through bytes
        /// </summary>
        /// <returns>True if assembly was loaded by this code</returns>
        [HarmonyPatch(
            assemblyQualifiedDeclaringType: "System.Reflection.RuntimeAssembly, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            nameof(Assembly.Location), MethodType.Getter)]
        public static bool Prefix(Assembly __instance, ref string __result)
            => !(enabled && AssemblyLocations.TryGetValue(__instance.FullName, out __result));



        /// <summary>
        /// Load assemblies from bytes. 
        /// Wrap everything in a try-catch.
        /// </summary>
        /// <returns>Skips vanilla method</returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Bootstrap), nameof(Bootstrap.LoadModAtPath))]
        public static bool LoadModAtPath(string path)
        {
            if (!enabled) { return true; }

            string shortPath = path.Split('/').Last().Replace("StreamingAssets", "");
            Debug.LogError("Trying to load mod at " + shortPath);

            try
            {
                Assembly modAssembly = null;
                string assemblyLocation = "";

                // Get possible dlls in this directory
                IEnumerable<string> dllPaths = from file in new DirectoryInfo(path).GetFiles("*.dll", SearchOption.TopDirectoryOnly)
                                                   // Overcomplicated way to load UniverseLib first before Unity Explorer mod
                                               orderby file.Name == "UniverseLib.Mono.dll" ? DateTime.MinValue : file.LastWriteTime
                                               select file.FullName;

                if (!dllPaths.Any())
                    return false;

                Assembly modDependency = null;
                foreach (string dllPath in dllPaths)
                {
                    try
                    {
                        modDependency = Assembly.Load(File.ReadAllBytes(dllPath));
                    }
                    catch
                    {
                        modDependency = Assembly.LoadFrom(dllPath);
                    }

                    try
                    {
                        foreach (System.Type type in modDependency.GetTypes())
                        {
                            if (type.BaseType == typeof(WildfrostMod))
                            {
                                modAssembly = modDependency;
                                assemblyLocation = dllPath;
                                break;
                            }
                        }
                    }
                    catch (TypeLoadException ex) { }
                }

                if (modAssembly == null)
                {
                    Debug.LogWarning($"Empty mod at {shortPath}");
                }
                else
                {
                    WildfrostMod mod = null;
                    foreach (System.Type type in modAssembly.GetTypes())
                    {
                        if (type.BaseType == typeof(WildfrostMod) && type != typeof(InternalMod))
                        {
                            mod = (WildfrostMod)Activator.CreateInstance(type, path);
                            Bootstrap.Mods.Add(mod);

                            modLastLoaded[path] = File.GetLastWriteTime(path);
                            modAssemblyInfo[mod.GUID] = (modAssembly, assemblyLocation);
                            break;
                        }
                    }
                    Debug.LogWarning($"Added instance [{mod?.Title}] from dir [{shortPath}]");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"FAILED to instantiate mod from dir [{shortPath}]\n{ex}");
            };
            return false;
        }


        

        /// <summary>
        /// If the mod DLL updated, skip loading this mod. Load the updated one instead.
        /// Otherwise load normally.
        /// </summary>
        /// <param name="__state">Stopwatch</param>
        [HarmonyPatch(typeof(WildfrostMod), nameof(WildfrostMod.ModLoad))]
        public static bool Prefix(WildfrostMod __instance)//, out System.Diagnostics.Stopwatch __state)
        {
            //__state = System.Diagnostics.Stopwatch.StartNew();

            if (!enabled) { return true; }

            //Debug.LogWarning($"TRYING Mod load [{__instance.Title}] from [{modAssemblyInfo[__instance.GUID].location}]");
            var path = __instance.ModDirectory;

            if (modLastLoaded.TryGetValue(path, out DateTime time) && time < File.GetLastWriteTime(modAssemblyInfo[__instance.GUID].location))
            {
                Debug.LogError($"ASSEMBLY OF [{__instance.Title}] WAS CHANGED");
                Bootstrap.Mods.Remove(__instance);

                Bootstrap.LoadModAtPath(path);
                var newMod = Bootstrap.Mods.FirstOrDefault(mod => mod.ModDirectory == path);
                if (newMod != null)
                {
                    newMod.ModLoad();
                    var holder = Resources.FindObjectsOfTypeAll<ModHolder>().FirstOrDefault(h => h.Mod.ModDirectory == path);
                    if (holder)
                    {
                        holder.Mod = newMod;
                        holder.UpdateInfo();
                    }
                }
                return false;
            }
            return true;
        }
        
        /*
        [HarmonyPatch(typeof(WildfrostMod), nameof(WildfrostMod.ModLoad))]
        public static void Postfix(WildfrostMod __instance, System.Diagnostics.Stopwatch __state)
        {
            __state.Stop();
            if (__instance != null)
                Debug.LogError($"Mod load [{__instance.Title}] took {__state.ElapsedMilliseconds} ms");
        }

        
        [HarmonyPatch(typeof(WildfrostMod), nameof(WildfrostMod.Load))]
        public static void Prefix(WildfrostMod __instance, out (System.Diagnostics.Stopwatch, string) __state)
            => __state = (System.Diagnostics.Stopwatch.StartNew(), __instance.Title);


        [HarmonyPatch(typeof(WildfrostMod), nameof(WildfrostMod.Load))]
        public static void Postfix(WildfrostMod __instance, (System.Diagnostics.Stopwatch, string) __state)
        {
            __state.Item1.Stop();
            if (__state.Item1.ElapsedMilliseconds == 0) return; // For mods that force unloading when loaded
            Debug.LogError($">> Load [{__state.Item2}] took {__state.Item1.ElapsedMilliseconds} ms");
        }








        #region Stop myself from uploading a mod that can possibly break mod loading :p
        [HarmonyPatch(typeof(ModsSceneManager), nameof(ModsSceneManager.Start))]
        public static IEnumerator Postfix(IEnumerator __result, ModsSceneManager __instance)
        {
            yield return __result;
            //CoroutineManager.Start(DontRelease(__instance));
        }
        static IEnumerator DontRelease(ModsSceneManager __instance)
        {
            yield return new WaitForFixedUpdate(); // Wait for Mod Uploader to make changes first
                                                   // After Mod Uploader changed buttons, change it back
            foreach (var modTransform in __instance.Content.transform.GetAllChildren())
            {
                var modHolder = modTransform.GetComponent<ModHolder>();
                Button button = modHolder.PublishButton.GetComponentInChildren<Button>();

                button.onClick = new ButtonClickedEvent();
                button.onClick.AddListener(() => throw new NotSupportedException(
                    "This code includes patches to the Bootstrap and ModLoad operations which are not allowed to be released."
                    // unless used by someone that knows what they're doing
                    ));
            }
        }
        #endregion

        */
    }

}
