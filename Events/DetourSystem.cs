using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detours
{
    internal class DetourSystem : GameSystem
    {
        public static DetourSystem instance;
        public static bool active;

        public static readonly Dictionary<string, Detour> allDetours = new Dictionary<string, Detour>();
        public static readonly Dictionary<string, Storyline> allStorylines = new Dictionary<string, Storyline>();
        public static List<Detour> activeEvents = new List<Detour>();
        public static List<Storyline> storylines = new List<Storyline>();
        public static readonly List<DetourPack> packs = new List<DetourPack>();

        public static string detourTitle = "mhcdc9_Detour_Title";
        public static string storylineTitle = "mhcdc9_Storyline_Titles";

        public static void Populate()
        {
            activeEvents.Clear();
            activeEvents.AddRange(packs.Where(p => p.Active).SelectMany(p => p.GetList()).OrderByDescending((d) => d.Priority));
            storylines = storylines.OrderBy((s) => Dead.Random.Range(s.Priority-1f, s.Priority)).ToList();
            Storyline._storyNode = Campaign.instance.nodes[0];
        }

        public static void SelectStorylines()
        {
            SaveCollection<string> collection = new SaveCollection<string>(storylines.Take(Math.Min(storylines.Count, DetourMain.instance.storylines)).Select(s => s.QualifiedName).ToArray());
            if (Storyline.StoryNode.data == null)
            {
                Storyline.StoryNode.data = new Dictionary<string, object>();
            }
            Storyline.StoryNode.data.Add(storylineTitle, collection);
            for(int i = 0; i < collection.Count; i++)
            {
                string item = collection[i];
                allStorylines[item].Setup();
            }
            
        }

        static Storyline currentStoryline;
        public static bool HasActiveStoryline(CampaignNode node)
        {
            if (Storyline.StoryNode.data.TryGetValue("mhcdc9_Storyline_Titles", out object data))
            {
                SaveCollection<string> storylines = (SaveCollection<string>)data;
                for(int i =0; i<storylines.Count; i++)
                {
                    string name = storylines[i];
                    if (allStorylines.ContainsKey(name))
                    {
                        currentStoryline = allStorylines[name];
                        if (currentStoryline.Active && currentStoryline.CanActivate(node))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static IEnumerator StartStoryline(CampaignNode node)
        {
            active = true;
            //detour.Setup(node);
            yield return currentStoryline.Run(node);
            active = false;
        }

        public static bool HasActiveDetour(CampaignNode node)
        {
            if (node.data.TryGetValue(detourTitle,out object data))
            {
                if (data is string title && allDetours.ContainsKey(title))
                {
                    if(!allDetours[title].MissingData(node))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static IEnumerator StartDetour(CampaignNode node)
        {
            active = true;
            node.data.TryGetValue(detourTitle, out object data);
            Detour detour = allDetours[data.ToString()];
            //detour.Setup(node);
            yield return DetourHolder.StartDetour(node, detour);
            node.data.Remove(detourTitle);
            active = false;
        }
    }
}
