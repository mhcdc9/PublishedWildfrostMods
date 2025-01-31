using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Detours
{
    public class DetourPack : IList<Detour>
    {
        #region boilerplate
        protected readonly List<Detour> _list = new List<Detour>();
        public Detour this[int index]
        {
            get
            {
                if (_list.Count <= index)
                {
                    return null;
                }

                return _list[index];
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public int Count => _list.Count;

        public bool IsReadOnly => throw new NotImplementedException();

        public void Add(Detour item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(Detour item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(Detour[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<Detour> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(Detour item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, Detour item)
        {
            _list.Insert(index, item);
        }

        public bool Remove(Detour item)
        {
            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        #endregion boilerplate

        public DetourPack(WildfrostMod mod, string name, bool active = true, int copies = 1)
        {
            this.mod = mod;
            this.name = name;
            this._active = active;
            this.copies = copies;
        }

        public virtual void Register()
        {
            DetourSystem.packs.Add(this);
            foreach(Detour item in this)
            {
                DetourSystem.allDetours[item.QualifiedName] = item;
            }
        }

        public virtual void Unregister()
        {
            DetourSystem.packs.Remove(this);
        }

        public List<Detour> GetList()
        {
            return _list;
        }

        public string name = "Grass Is Greener";

        public bool _active = true;

        public int copies = 1;

        public WildfrostMod mod;

        public bool Active => _active;
    }

    public abstract class Storyline : DetourPack
    {
        internal static CampaignNode _storyNode;

        public string QualifiedName => mod.GUID + name;

        public static CampaignNode StoryNode { get { return _storyNode; } }
        protected Storyline(WildfrostMod mod, string name, bool active = true, int copies = 1) : base(mod, name, active, copies)
        {
        }

        public virtual int Priority => 1;


        public override void Register()
        {
            DetourSystem.storylines.Add(this);
            DetourSystem.allStorylines[QualifiedName] = this;
        }

        public override void Unregister()
        {
            DetourSystem.storylines.Remove(this);
        }

        public virtual void Setup()
        {

        }

        public virtual bool CanActivate(CampaignNode node)
        {
            return true;
        }

        public abstract IEnumerator Run(CampaignNode node, string startFrame = "START");

        public void SetData(string key, object value)
        {
            key = QualifiedName + ": " + key;
            if (StoryNode.data.TryGetValue(key, out var data))
            {
                StoryNode.data.Remove(key);
            }
            StoryNode.data.Add(key, value);
        }

        public bool TryGetData<T>(string key, out T value)
        {
            if (StoryNode.data.TryGetValue(QualifiedName + ": " + key, out object protoValue))
            {
                value = (T)protoValue;
                return true;
            }
            value = default(T);
            return false;
        }

    }
}
