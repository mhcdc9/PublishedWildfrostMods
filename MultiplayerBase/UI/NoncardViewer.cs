using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MultiplayerBase.UI
{
    internal class NoncardViewer : MonoBehaviourRect, IList<NoncardReward>, ICollection<NoncardReward>, IEnumerable<NoncardReward>, IEnumerable
    {
        private List<NoncardReward> list = new List<NoncardReward>();
        private static Dictionary<NoncardReward, (Friend, int)> decorations = new Dictionary<NoncardReward, (Friend, int)>();

        public float Spacing => GetComponent<HorizontalLayoutGroup>().spacing;
        public int Count => list.Count;
        public bool Empty => Count <= 0;
        public bool IsReadOnly => false;
        public static NoncardViewer Create(Transform transform)
        {
            GameObject obj = HelperUI.HorizontalGroup("Noncard Viewer", transform, new Vector2(1.5f, 1.5f), 0.2f);
            obj.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            obj.transform.SetParent(transform, false);
            NoncardViewer ncv = obj.AddComponent<NoncardViewer>();
            return ncv;
        }

        public static (Friend,int) FindDecoration(NoncardReward ncr)
        {
            return decorations[ncr];
        }

        public NoncardReward this[int index]
        {
            get
            {
                if (list.Count < index)
                {
                    return null;
                }
                return list[index];
            }
            set
            {
                if (list.Count < index)
                {
                    list[index] = value;
                }
            }
        }

        public void Add(NoncardReward item, Friend friend, int id)
        {
            decorations[item] = (friend, id);
            Add(item);
        }

        public void Add(NoncardReward item)
        {
            item.transform.SetParent(transform);
            list.Add(item);
        }

        public void Insert(int index, NoncardReward item, Friend friend, int id)
        {
            decorations[item] = (friend, id);
            Insert(index, item);
        }

        public void Insert(int index, NoncardReward item)
        {
            list.Insert(index, item);
        }

        public bool Remove(NoncardReward item)
        {
            if (decorations.ContainsKey(item))
            {
                decorations.Remove(item);
            }
            return list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            Remove(this[index]);
        }

        public int IndexOf(NoncardReward item)
        {
            return list.IndexOf(item);
        }

        public bool Contains(NoncardReward item)
        {
            return list.Contains(item); 
        }

        public void CopyTo(NoncardReward[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public void Clear()
        {
            decorations.Clear();
            list.Clear();
        }

        public void SetSize()
        {
            float width = (Count - 1) * Spacing;
            float height = 0;
            foreach(var item in list)
            {
                Vector2 vec = item.GetComponent<RectTransform>().sizeDelta;
                width += vec.x;
                height = Math.Max(height, vec.y);
            }
            GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
        }

        public NoncardReward[] ToArray()
        {
            return list.ToArray();
        }

        public IEnumerator<NoncardReward> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
