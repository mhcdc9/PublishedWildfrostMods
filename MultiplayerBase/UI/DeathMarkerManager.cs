﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerBase.UI
{
    internal class DeathMarkerManager : MonoBehaviour
    {
        List<GameObject> enemyMarks = new List<GameObject>();
        List<GameObject> playerMarks = new List<GameObject>();

        static GameObject prefab;

        private bool FindPrefab()
        {
            VfxDeathSystem system = GameObject.FindObjectOfType<VfxDeathSystem>();
            prefab = system?.sacrificeFX?.transform?.GetChild(4).gameObject;
            return prefab == null;
        }

        public void CreateMarker(string side, Vector3 positon)
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
            obj.transform.position = positon;
            StartCoroutine(GrowAndStop(obj));
        }

        private IEnumerator GrowAndStop(GameObject obj)
        {
            yield return new WaitForSeconds(0.5f);
            obj?.GetComponent<ParticleSystem>()?.Pause();
        }

        public void ClearMarkers(string side)
        {
            if (!markList(side, out List<GameObject> markers))
            {
                return;
            }

            for (int i = markers.Count - 1; i>=0; i--)
            {
                markers[i].Destroy();
            }
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