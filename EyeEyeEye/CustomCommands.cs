using Deadpan.Enums.Engine.Components.Modding;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
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
            commands.Add(new CommandMultEYE());
            commands.Add(new CommandMultEYERESET());
        }

        public class CommandMultEYE : Command
        {
            public static List<Transform> transforms = new List<Transform>();
            public override string id => "eye";

            public override string format => "eye <posX> <posY> <scaleX> <scaleY> <rotation> or eye here";

            public override string desc => "Create a frosty eye";

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
                            position = transform.InverseTransformPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition + new Vector3(0, 0, 10))),
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

        public class CommandMultEYERESET : Command
        {
            public static List<Transform> transforms = new List<Transform>();
            public override string id => "eyereset";

            public override string format => "eyereset";

            public override string desc => "Create a frosty eye";

            public override bool IsRoutine => false;
            public override void Run(string args)
            {
                for (int i = CommandMultEYE.transforms.Count-1; i>=0; i-- )
                {
                    CommandMultEYE.transforms[i].gameObject.Destroy();
                    CommandMultEYE.transforms.RemoveAt(i);
                }
                CommandMultEYE.transforms.Clear();
            }

            public override IEnumerator GetArgOptions(string currentArgs)
            {
                yield break;
            }
        }
    }
}
