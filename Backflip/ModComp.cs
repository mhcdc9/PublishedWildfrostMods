using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Backflip
{
    public static class ModComp
    {
        internal static float dur = 1;
        internal static Vector2 size = new Vector2(6, 10);
        internal static Vector3 pos = new Vector3(0, -1.5f, 0);

        internal static Vector2 waitRange = new Vector2(1f, 3f);
        public static IEnumerator InspectCosmetic(Transform parent)
        {
            Backflip.MakeBackflipCurves();
            GameObject booshuHolder = new GameObject("Booshu Holder");
            booshuHolder.transform.SetParent(parent, false);
            booshuHolder.transform.localPosition = pos;
            GameObject booshu = new GameObject("Booshu", new Type[] { typeof(Image) });
            RectTransform t = booshu.GetComponent<RectTransform>();
            t.sizeDelta = size;
            t.SetParent(booshuHolder.transform, false);
            t.localPosition = pos;
            Image image = booshu.GetComponent<Image>();
            image.sprite = AddressableLoader.Get<CardData>("CardData", "BerryPet").mainSprite;
            image.color = new Color(1, 1, 1, 0.7f);
            t.localScale = new Vector3(1, 1, 0);

            yield return Sequences.Wait(0.25f);

            LeanTween.scale(booshu, new Vector3(1, 1, 1), dur).setEaseInOutElastic();

            yield return Sequences.Wait(1 + dur);

            while(true)
            {
                booshuHolder.transform.eulerAngles = new Vector3(0, 0, 0);
                LeanTween.moveLocal(booshuHolder, new Vector3(0,2,0), 0.6667f).setEase(Backflip.jumpCurve);
                LeanTween.rotate(booshuHolder, new Vector3(0,0,1), 0.6667f).setEase(Backflip.rotateCurve);
                yield return Sequences.Wait(0.6667f);
                yield return Sequences.Wait(waitRange.Random());
            }
        }
    }
}
