using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerBase.UI
{
    internal class MarkerManager : MonoBehaviour
    {
        List<GameObject> enemyMarks = new List<GameObject>();
        List<GameObject> playerMarks = new List<GameObject>();

        static GameObject prefab;

        private bool visible = true;

        public void Start()
        {
            Events.OnInspect += OnInspect;
            Events.OnInspectEnd += OnInspectEnd;
        }

        public void OnDestroy()
        {
            Events.OnInspect -= OnInspect;
            Events.OnInspectEnd -= OnInspectEnd;
        }
        
        private bool FindPrefab()
        {
            VfxDeathSystem system = GameObject.FindObjectOfType<VfxDeathSystem>();
            prefab = system?.sacrificeFX?.transform?.GetChild(4).gameObject;
            return prefab == null;
        }

        public void CreateDeathMarker(string side, Vector3 position)
        {
            if (!markList(side, out List<GameObject> markers))
            {
                return;
            }

            if (prefab == null && FindPrefab())
            {
                return;
            }

            GameObject obj = GameObject.Instantiate(prefab, transform);
            markers.Add(obj);
            obj.SetActive(true);
            obj.GetComponent<ParticleSystemRenderer>().enabled = visible;
            obj.transform.position = position;
            StartCoroutine(GrowAndStop(obj));
        }

        public static float scalingFactor = 1f;

        public void CreateMarker(string side, Vector3 position, string type = "death")
        {
            if (type == "death")
            {
                CreateDeathMarker(side, position);
            }

            if (!visible || !markList(side, out List<GameObject> markers))
            { return; }

            VfxStatusSystem system = GameObject.FindObjectOfType<VfxStatusSystem>();

            if (system == null || !system.profileLookup.ContainsKey(type))
            { return; }

            system.CreateEffect(system.profileLookup[type].applyEffectPrefab, position, scalingFactor*Vector3.one);
        }

        private IEnumerator GrowAndStop(GameObject obj)
        {
            ParticleSystem system = obj?.GetComponent<ParticleSystem>(); //I sure hope that the object hasn't been destroyed in a fraction of a frame :/
            yield return new WaitForSeconds(0.5f);
            system?.Pause();
        }

        public void OnInspect(Entity _)
        {
            ChangeVisibility(false);
        }

        public void OnInspectEnd(Entity _)
        {
            ChangeVisibility(true);
        }

        public void ChangeVisibility(bool visible)
        {
            this.visible = visible;
            foreach(GameObject obj in enemyMarks)
            {
                ParticleSystemRenderer renderer = obj?.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }

        public void ClearMarkers(string side)
        {
            if (!markList(side, out List<GameObject> markers))
            {
                return;
            }

            for (int i = markers.Count - 1; i>=0; i--)
            {
                markers[i]?.Destroy();
            }

            markers.Clear();
        }

        private bool markList(string side, out List<GameObject> markers)
        {
            markers = null;
            switch(side)
            {
                case "PLAYER":
                    markers = playerMarks;
                    return true;
                case "ENEMY":
                    markers = enemyMarks;
                    return true;
            }
            return false;
        }
    }
}
