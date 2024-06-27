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

        public void Add(Entity entity, Friend friend, ulong id)
        {
            if (!decorations.ContainsKey(entity))
            {
                decorations[entity] = (friend, id);
                base.Add(entity);
            }
        }

        public override void Add(Entity entity)
        {
            if(!decorations.ContainsKey(entity))
            {
                decorations[entity] = (HandlerSystem.self, entity.data.id);
                base.Add(entity);
            }
        }

        public override void Remove(Entity entity)
        {
            if (decorations.ContainsKey(entity))
            {
                decorations.Remove(entity);
            }
            entity.owner = null;
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
                if (decorations.ContainsKey(entity) && decorations[entity].Item1.Id.Value == friend.Id.Value && decorations[entity].Item2 == id)
                {
                    return entity;
                }
            }
            return null;
        }

        public (Friend,ulong) Find(Entity entity)
        {
            if (decorations.ContainsKey(entity))
            {
                return decorations[entity];
            }
            return (HandlerSystem.self, entity.data.id);
        }
    }
}
