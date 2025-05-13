using Deadpan.Enums.Engine.Components.Modding;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer;
using UnityExplorer.CacheObject;
using UnityExplorer.UI;
using WildfrostHopeMod.CommandsConsole;
using static Console;

namespace EyeEyeEye
{
    internal class Commands
    {
        public static IEnumerator AddCustomCommands(WildfrostMod _)
        {
            yield return new WaitUntil(() => ConsoleMod.instantiated);
            commands.Add(new CommandEYE());
            commands.Add(new CommandEYERESET());
            commands.Add(new CommandEYERECORD());
            commands.Add(new CommandEYEOUTPUT());
        }

        public class CommandEYE : Command
        {

            public static List<Transform> transforms = new List<Transform>();
            public override string id => "eye";

            public override string format => "eye <posX> <posY> <scaleX> <scaleY> <rotation> or eye here";

            public override string desc => "Create frosty eyes for frosty cards.";

            public override bool IsRoutine => false;
            public override void Run(string args)
            {
                Entity entity = hover ?? GameObject.FindObjectOfType<InspectSystem>()?.inspect;
                if (entity == null) { Fail("A card needs to be inspected or hovered over"); return; }

                if (!(entity.display is Card card))
                {
                    return;
                }

                Transform parent = card.mainImage.transform.parent;

                string[] s = args.Split(' ');

                Transform transform = parent.Cast<Transform>().FirstOrDefault((Transform a) => a.gameObject.activeSelf);
                if ((bool)transform)
                {
                    EyeData.Eye eye;

                    if (s[0] == "here")
                    {
                        eye = new EyeData.Eye()
                        {
                            position = transform.InverseTransformPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition + new Vector3(0, 0, 9.5f))),
                            scale = Vector2.one,
                            rotation = 0
                        };
                    }
                    else
                    {
                        eye = new EyeData.Eye()
                        {
                            position = new Vector2(float.Parse(s[0]), float.Parse(s[1])),
                            scale = new Vector2(float.Parse(s[2]), float.Parse(s[3])),
                            rotation = float.Parse(s[4])
                        };
                    }

                    Transform transform2 = FrostEyeSystem.frostEyePrefabRef.InstantiateAsync(transform).WaitForCompletion().transform;
                    transform2.SetLocalPositionAndRotation(eye.position, Quaternion.Euler(0f, 0f, eye.rotation));
                    transform2.localScale = eye.scale.WithZ(1f);
                    transforms.Add(transform2);

                    if (ExplorerStandalone.Instance == null)
                    {
                        Fail("Unity Explorer by Miya/Kopie must be loaded to finish this command");
                    }
                    else
                    {
                        InspectorManager.Inspect(transform2.gameObject, (CacheObjectBase)null);
                        UIManager.ShowMenu = true;
                    }
                }
            }

            public Vector3 FindLocalPosition()
            {
                Vector3 v = Vector3.zero;
                Entity entity = hover ?? GameObject.FindObjectOfType<InspectSystem>()?.inspect;
                if (entity == null) { Fail("A card needs to be inspected or hovered over"); return v; }

                if (!(entity.display is Card card))
                {
                    return v;
                }

                Transform parent = card.mainImage.transform.parent;

                Transform transform = parent.Cast<Transform>().FirstOrDefault((Transform a) => a.gameObject.activeSelf);
                return transform?.InverseTransformPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition + new Vector3(0, 0, 10))) ?? v;
            }

            public override IEnumerator GetArgOptions(string currentArgs)
            {
                string[] args = currentArgs.Split(new Char[] { ' ' }, options: StringSplitOptions.None);
                int length = args.Length;

                Vector3 v = FindLocalPosition();

                if (length == 1)
                {
                    predictedArgs = new string[] { "<posX>", "here", v.x.ToString() };
                    yield break;
                }

                else if (length == 2 && args[0] != "here")
                {
                    predictedArgs = new string[] { "<posY>", v.y.ToString() };
                    yield break;
                }

                else if (length == 3)
                {
                    predictedArgs = new string[] { "<scaleX>" };
                    yield break;
                }

                else if (length == 4)
                {
                    predictedArgs = new string[] { "<scaleY>" };
                    yield break;
                }

                else if (length == 5)
                {
                    predictedArgs = new string[] { "<rotation>" };
                    yield break;
                }
            }
        }

        public class CommandEYERESET : Command
        {
            public override string id => "eyereset";

            public override string format => "eyereset";

            public override string desc => "Create a frosty eye";

            public override bool IsRoutine => false;
            public override void Run(string args)
            {
                for (int i = CommandEYE.transforms.Count-1; i>=0; i-- )
                {
                    CommandEYE.transforms[i].gameObject.Destroy();
                    CommandEYE.transforms.RemoveAt(i);
                }
                CommandEYE.transforms.Clear();
            }

            public override IEnumerator GetArgOptions(string currentArgs)
            {
                yield break;
            }
        }

        public class CommandEYERECORD : Command
        {
            public override string id => "recordeyes";

            public override string format => "recordeyes";

            public override string desc => "Records ALL eye data onto the currently hovered card, then resets";

            public override bool IsRoutine => false;
            public override void Run(string args)
            {
                Entity entity = hover ?? GameObject.FindObjectOfType<InspectSystem>()?.inspect;
                if (entity == null) { Fail("A card needs to be inspected or hovered over"); return; }

                if (!(entity.display is Card card))
                {
                    return;
                }

                List<(float, float, float, float, float)> list = new List<(float, float, float, float, float)>();
                if (EyeEyeEye.eyeData.ContainsKey(entity.data.name))
                {
                    list = EyeEyeEye.eyeData[entity.data.name];              
                }

                for (int i = CommandEYE.transforms.Count - 1; i >= 0; i--)
                {
                    Transform tr = CommandEYE.transforms[i];
                    if (tr == null)
                        continue;
                    (float, float, float, float, float) data = (tr.localPosition.x, tr.localPosition.y, tr.localScale.x, tr.localScale.y, tr.rotation.eulerAngles.z);
                    if (entity.data.cardType.name == "Leader")
                    {
                        data.Item1 += 0.05f;
                        data.Item2 += 0.45f;
                        data.Item3 += 0.08f;
                        data.Item4 += 0.08f;
                    }
                    list.Insert(0,data);
                    CommandEYE.transforms[i].gameObject.Destroy();
                    CommandEYE.transforms.RemoveAt(i);
                }
                CommandEYE.transforms.Clear();
                EyeEyeEye.eyeData[entity.data.name] = list;
            }

            public override IEnumerator GetArgOptions(string currentArgs)
            {
                yield break;
            }
        }

        public class CommandEYEOUTPUT : Command
        {
            public override string id => "outputeyes";

            public override string format => "outputeyes <fileName>";

            public override string desc => "Outputs all eye data into the file with designated name";

            public override bool IsRoutine => false;
            public override void Run(string args)
            {
                if (args.IsNullOrEmpty()) { Fail("Type a nonempty file name!"); return; }

                string fileName = Path.Combine(EyeEyeEye.instance.ModDirectory, args);
                if (!fileName.Contains(".txt"))
                {
                    fileName += ".txt";
                }

                List<string> list = new List<string>();
                if (System.IO.File.Exists(fileName))
                {
                    list.AddRange(File.ReadAllLines(fileName));
                }

                foreach(string key in EyeEyeEye.eyeData.Keys)
                {
                    string s = $"Eyes(\"{key}\",";
                    var data = EyeEyeEye.eyeData[key];
                    for(int i =0; i<data.Count; i++)
                    {
                        s += string.Format(" ({0:f2}f,{1:f2}f,{2:f2}f,{3:f2}f,{4:f0}f)", data[i].Item1, data[i].Item2, data[i].Item3, data[i].Item4, data[i].Item5);
                        if (i != data.Count-1)
                        {
                            s += ",";
                        }
                    }
                    s += "),";
                    list.Add(s);
                }
                File.WriteAllLines(fileName, list);
                EyeEyeEye.eyeData.Clear();
            }

            public override IEnumerator GetArgOptions(string currentArgs)
            {
                yield break;
            }
        }
    }
}
