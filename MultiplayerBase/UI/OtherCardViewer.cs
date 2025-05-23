using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using MultiplayerBase.Handlers;

namespace MultiplayerBase.UI
{
    public class OtherCardViewer : CardLane
    {
        Dictionary<Entity, (Friend, ulong)> decorations = new Dictionary<Entity, (Friend, ulong)>();

        public Vector3 startTextPosition = new Vector3(0f, -3f, 0f);
        public Vector3 defaultTextPosition = new Vector3(0f, -4f, 0f);

        public bool BattleCardViewer = false;

        public void Add(Entity entity, Friend friend, ulong id)
        {
            if (entity == null)
            {
                entities.Add(null);
                Count++;
                return;
            }
            if (!decorations.ContainsKey(entity))
            {
                decorations[entity] = (friend, id);
                entity.owner = owner;
                base.Add(entity);
            }
        }

        public void Insert(int index, Entity entity, Friend friend, ulong id)
        {
            while (entities.Count < index)
            {
                Add(null);
            }
            if (!decorations.ContainsKey(entity))
            {
                if (index < entities.Count && entities[index] == null)
                {
                    decorations[entity] = (friend, id);
                    entity.transform.SetParent(holder);
                    entity.AddTo(this);
                    entities[index] = entity;
                    CardAdded(entity);
                    onAdd.Invoke(entity);
                }
                else
                {
                    Add(entity, friend, id);
                }
            }
        }

        public override void Add(Entity entity)
        {
            if (entity == null)
            {
                entities.Add(null);
                Count++;
                return;
            }
            if (!decorations.ContainsKey(entity))
            {
                decorations[entity] = (HandlerSystem.self, entity.data.id);
                entity.owner = owner;
                base.Add(entity);
            }
        }

        public override void Remove(Entity entity)
        {
            if (entity == null)
            {
                entities.RemoveWhere(x => x == null);
                Count--;
                return;
            }
            if (decorations.ContainsKey(entity))
            {
                decorations.Remove(entity);
            }
            entity.owner = null; //Why was this here? To stop the splatter surface from throwing a null error on destroy
            base.Remove(entity);
        }

        /*public override void RemoveAt(int index)
        {
            Entity entity = this[index];
            if (decorations.ContainsKey(entity))
            {
                decorations.Remove(entity);
            }
            entity.owner = null;
            base.RemoveAt(index);
        }*/

        public void Hover(Entity entity)
        {
            if (decorations.ContainsKey(entity))
            {
                GameObject obj = new GameObject("Owner Text");
                obj.transform.SetParent(entity.canvas.transform, false);
                obj.transform.localPosition = startTextPosition;
                LeanTween.moveLocal(obj, defaultTextPosition, 0.5f).setEaseInQuart();
                TextMeshProUGUI textElement = obj.AddComponent<TextMeshProUGUI>();
                textElement.fontSize = 0.4f;
                textElement.horizontalAlignment = HorizontalAlignmentOptions.Center;
                textElement.text = decorations[entity].Item1.Name;
                textElement.outlineColor = Color.black;
                textElement.outlineWidth = 0.06f;
                obj.GetComponent<RectTransform>().sizeDelta = new Vector2(4f, 1f);
            }
        }

        public void Unhover(Entity entity)
        {
            if (entity == null) { return; }

            foreach(TextMeshProUGUI text in entity.gameObject.GetComponentsInChildren<TextMeshProUGUI>())
            {
                if (text.gameObject.name == "Owner Text")
                {
                    text.gameObject.Destroy();
                }
            }
        }

        public Entity Find(Friend friend, ulong id)
        {
            foreach (Entity entity in this)
            {
                if (entity == null) { continue; }
                if (decorations.ContainsKey(entity) && decorations[entity].Item1.Id.Value == friend.Id.Value && decorations[entity].Item2 == id)
                {
                    return entity;
                }
            }
            return null;
        }

        public (Friend,ulong) Find(Entity entity)
        {
            if (entity == null) { return (HandlerSystem.self, 0); }
            if (decorations.ContainsKey(entity))
            {
                return decorations[entity];
            }
            return (HandlerSystem.self, entity.data.id);
        }

        public override void SetChildPosition(Entity child)
        {
            if (child == null) { return; }
            /*if (child.height > 1 && BattleCardViewer)
            {
                child.transform.position = HandlerBattle.FindPositionForBosses(this, child);
                child.transform.localScale = GetChildScale(child);
                child.transform.localEulerAngles = GetChildRotation(child);
                return;
            }*/
            base.SetChildPosition(child);
            if (child?.actualContainers != null && child.actualContainers.Count() > 1)
            {
                child.transform.position = HandlerBattle.FindPositionForBosses(this, child);
            }
            /*if (child?.actualContainers != null && child.actualContainers.Count() > 1)
            {
                child.transform.localPosition *= -1;
            }*/
        }

        public override Vector3 GetChildPosition(Entity child)
        {
            Vector3 v =  base.GetChildPosition(child);
            if (child?.actualContainers != null && child.actualContainers.Count() > 1)
            {
                v *= -1f;
            }
            return v;
        }

        new public void ClearAndDestroyAllImmediately()
        {
            Entity[] array = ToArray();
            Clear();
            Entity[] array2 = array;
            if (array2 == null) { return; }
            for (int i = 0; i < array2.Length; i++)
            {
                if (array2[i] != null)
                {
                    array2[i]?.gameObject.DestroyImmediate();
                }
            }
        }
    }
}
