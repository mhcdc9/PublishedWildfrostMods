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

        public static StatusEffectDataBuilder CreateSyncEffect<T>(this StatusEffectDataBuilder b, string name, string desc, string textInsert, StatusEffectData effectToApply, string type = "", bool ongoing = true) where T : StatusEffectSync
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
                    data.ongoing = ongoing;
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
                StatusEffectData effect = SyncMain.Instance.Get<StatusEffectData>(stack.Item1).InstantiateKeepName();
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

    }

    
}
