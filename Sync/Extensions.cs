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
        public static StatusEffectDataBuilder CreateTempTrait(this StatusEffectDataBuilder b, string name, TraitData trait)
        {
            return b.Create<StatusEffectTemporaryTrait>(name)
                .WithType("")
                .FreeModify<StatusEffectTemporaryTrait>(
                (data) =>
                {
                    data.trait = trait;
                });
        }

        public static StatusEffectDataBuilder CreateSyncEffect<T>(this StatusEffectDataBuilder b, string name, string desc, string textInsert, string effectToApply, string type = "", bool boostable = false, bool ongoing = true) where T : StatusEffectSync
        {
            return b.Create<T>(name)
                .WithCanBeBoosted(boostable)
                .WithText(desc)
                .WithTextInsert(textInsert)
                .WithType(type)
                .SubscribeToAfterAllBuildEvent(
                (data) =>
                {
                    T syncData = data as T;
                    syncData.effectToApply = AddressableLoader.Get<StatusEffectData>("StatusEffectData", effectToApply);
                    syncData.applyToFlags = StatusEffectApplyX.ApplyToFlags.Self;
                    syncData.ongoing = ongoing;
                }
                );
        }

        public static StatusEffectDataBuilder WithConstraints(this StatusEffectDataBuilder b, params TargetConstraint[] constraints)
        {
            return b.FreeModify(
                (data) =>
                {
                    data.targetConstraints = constraints;
                });
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

        public static bool TryAddSync(Entity e, (string, int)[] options)
        {
            if (TryAddSync(e.data, options, e))
            {
                References.instance.StartCoroutine(e.display.UpdateData(true));
                e.display.promptUpdateDescription = true;
                e.PromptUpdate();
                return true;
            }
            return false;
        }

        public static bool TryAddSync(CardData data, (string, int)[] options, Entity entity = null)
        {
            if (data == null)
                return false;
            foreach ((string, int) stack in options.InRandomOrder())
            {
                StatusEffectData effect = SyncMain.Instance.Get<StatusEffectData>(stack.Item1);
                if (CheckConstraints(effect, data))
                {
                    data.startWithEffects = CardData.StatusEffectStacks.Stack(data.startWithEffects, new CardData.StatusEffectStacks[]
                    {
                        new CardData.StatusEffectStacks(effect, stack.Item2)
                    });
                    if (entity != null)
                    {
                        effect.InstantiateKeepName().Apply(stack.Item2, entity, null);
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool TryAddMyst(Entity e)
        {
            if (TryAddMyst(e.data, e))
            {
                References.instance.StartCoroutine(e.display.UpdateData(true));
                e.display.promptUpdateDescription = true;
                e.PromptUpdate();
                return true;
            }
            return false;
        }

        public static bool TryAddMyst(CardData data, Entity entity = null)
        {
            if (data == null) { return false; }
            StatusEffectData effect = SyncMain.Instance.Get<StatusEffectData>("Mystic");
            if (CheckConstraints(effect, data))
            {
                data.startWithEffects = CardData.StatusEffectStacks.Stack(data.startWithEffects, new CardData.StatusEffectStacks[]
                {
                    new CardData.StatusEffectStacks(effect, 1)
                });
                if (entity != null)
                {
                    effect.InstantiateKeepName().Apply(1, entity, null);
                }
                return true;
                }
            return false;
        }

        public static bool CheckConstraints(StatusEffectData effect, CardData data)
        {
            foreach (TargetConstraint constraint in effect.targetConstraints)
            {
                if (!constraint.Check(data))
                {
                    return false;
                }
            }
            return true;
        }

        public static TargetConstraint DoesAttack() => ScriptableObject.CreateInstance<TargetConstraintDoesAttack>();
        public static TargetConstraint DoesDamage() => ScriptableObject.CreateInstance<TargetConstraintDoesDamage>();
        public static TargetConstraint HasHealth() => ScriptableObject.CreateInstance<TargetConstraintHasHealth>();
        public static TargetConstraint HasCounter() => ScriptableObject.CreateInstance<TargetConstraintMaxCounterMoreThan>();
        public static TargetConstraint CanBeBoosted() => ScriptableObject.CreateInstance<TargetConstraintCanBeBoosted>();

        public static TargetConstraint TargetsBoard() => ScriptableObject.CreateInstance<TargetConstraintPlayableOnBoard>();

        public static TargetConstraint IsItem() => ScriptableObject.CreateInstance<TargetConstraintIsItem>();

        public static TargetConstraint IsPlay()
        {
            TargetConstraintPlayType play =  ScriptableObject.CreateInstance<TargetConstraintPlayType>();
            play.targetPlayType = Card.PlayType.Play;
            return play;
        }

        public static TargetConstraint NotOnSlot()
        {
            TargetConstraintPlayOnSlot play = ScriptableObject.CreateInstance<TargetConstraintPlayOnSlot>();
            play.not = true;
            play.slot = true;
            return play;
        }

        public static TargetConstraint NotTrait(string traitName)
        {
            TargetConstraintHasTrait constraint = ScriptableObject.CreateInstance<TargetConstraintHasTrait>();
            constraint.trait = SyncMain.Instance.Get<TraitData>(traitName);
            constraint.not = true;
            return constraint;
        }

    }

    
}
