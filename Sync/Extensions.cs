using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Sync
{
    public static class Extensions
    {

        public static StatusEffectDataBuilder CreateSyncEffect<T>(this StatusEffectDataBuilder b, string name, string desc, string textInsert, StatusEffectData effectToApply, string type = "") where T : StatusEffectSync
        {
            return b.Create<T>(name)
                .WithCanBeBoosted(true)
                .WithText(desc)
                .WithTextInsert(textInsert)
                .WithIsStatus(false)
                .WithStackable(true)
                .WithType(type)
                .FreeModify<T>(
                (data) =>
                {
                    data.effectToApply = effectToApply;
                    data.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                }
                );

        }

        public static KeywordDataBuilder CreateBasicKeyword(this WildfrostMod mod, string name, string title, string desc)
        {
            return new KeywordDataBuilder(mod)
                .Create(name)
                .WithTitle(title)
                .WithDescription(desc)
                .WithShowName(true);
        }

        public static KeywordDataBuilder ChooseColors(this KeywordDataBuilder builder, Color? titleColor = null, Color? bodyColor = null, Color? noteColor = null)
        {
            return builder.WithTitleColour(titleColor)
                .WithBodyColour(bodyColor)
                .WithNoteColour(noteColor);
        }
    }

    
}
