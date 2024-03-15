using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections;

namespace Information_Collector
{
    public class InfoCollector : WildfrostMod
    {
        public InfoCollector(string modDirectory) : base(modDirectory)
        {
        }

        public override string GUID => "mhcdc9.wildfrost.collector";

        public override string[] Depends => new string[0];

        public override string Title => "DataFile Scraper";

        public override string Description => "Scrapes information for the purpose of the Wildfrost References page.";

        public void CollectWhoUsesStatuses()
        {
            Dictionary<string, string> statusUsed  = new Dictionary<string, string>();
            List<string> newStatuses = new List<string>();
            UnityEngine.Debug.Log("==========Phase 1: Cards===========");
            List<CardData> cards = AddressableLoader.GetGroup<CardData>("CardData");
            foreach (CardData card in cards)
            {
                foreach(CardData.StatusEffectStacks stack in card.attackEffects)
                {
                    if (!statusUsed.ContainsKey(stack.data.name))
                    {
                        statusUsed.Add(stack.data.name, "");
                        newStatuses.Add(stack.data.name);
                    }

                    if (stack.data.stackable)
                    {
                        statusUsed[stack.data.name] += $"{card.title}(a{stack.count}),";
                    }
                    else
                    {
                        statusUsed[stack.data.name] += $"{card.title}(a),";
                    }
                }
                foreach(CardData.StatusEffectStacks stack in card.startWithEffects)
                {
                    if (!statusUsed.ContainsKey(stack.data.name))
                    {
                        statusUsed.Add(stack.data.name, "");
                        newStatuses.Add(stack.data.name);
                    }

                    if (stack.data.stackable)
                    {
                        statusUsed[stack.data.name] += $"{card.title}({stack.count}),";
                    }
                    else
                    {
                        statusUsed[stack.data.name] += $"{card.title},";
                    }
                }
            }

            UnityEngine.Debug.Log("==========Phase 2: Status Effects===========");
            for(int i=0; i<5; i++) //I am not expecting more than 5 levels of nesting. At least until this is bug-free.
            {
                List<string> list = newStatuses;
                UnityEngine.Debug.Log($"There are {list.Count} entries to evaluate.");
                newStatuses = new List<string>();
                foreach(string name in list)
                {
                    StatusEffectData data = Get<StatusEffectData>(name);
                    if (data == null)
                    {
                        continue;
                    }
                    if (data is StatusEffectAffectAllXApplied effect1)
                    {
                        StatusEffectData newEffect = effect1.effectToAffect;
                        if (newEffect == null) { continue; }
                        if (!statusUsed.ContainsKey(newEffect.name))
                        {
                            statusUsed.Add(newEffect.name, "");
                            newStatuses.Add(newEffect.name);
                        }
                        statusUsed[newEffect.name] += "{";
                    }
                    if (data is StatusEffectApplyToSummon effect2)
                    {
                        StatusEffectData newEffect = effect2.effectToApply;
                        if (newEffect == null) { continue; }
                        if (!statusUsed.ContainsKey(newEffect.name))
                        {
                            statusUsed.Add(newEffect.name, "");
                            newStatuses.Add(newEffect.name);
                        }
                        statusUsed[newEffect.name] += "{";
                    }
                    if (data is StatusEffectApplyX effect3)
                    {
                        StatusEffectData newEffect = effect3.effectToApply;
                        if (newEffect == null) { continue; }
                        if (!statusUsed.ContainsKey(newEffect.name))
                        {
                            statusUsed.Add(newEffect.name, "");
                            newStatuses.Add(newEffect.name);
                        }
                        statusUsed[newEffect.name] += "{";
                    }
                    if (data is StatusEffectDoubleAllXWhenDestroyed effect4)
                    {
                        StatusEffectData newEffect = effect4.effectToDouble;
                        if (newEffect == null) { continue; }
                        if (!statusUsed.ContainsKey(newEffect.name))
                        {
                            statusUsed.Add(newEffect.name, "");
                            newStatuses.Add(newEffect.name);
                        }
                        statusUsed[newEffect.name] += "{";
                    }
                    if (data is StatusEffectHaltX effect5)
                    {
                        StatusEffectData newEffect = effect5.effectToHalt;
                        if (newEffect == null) { continue; }
                        if (!statusUsed.ContainsKey(newEffect.name))
                        {
                            statusUsed.Add(newEffect.name, "");
                            newStatuses.Add(newEffect.name);
                        }
                        statusUsed[newEffect.name] += "{";
                    }
                    if (data is StatusEffectIncreaseAttackWhileDamaged effect6)
                    {
                        StatusEffectData newEffect = effect6.effectToGain;
                        if (newEffect == null) { continue; }
                        if (!statusUsed.ContainsKey(newEffect.name))
                        {
                            statusUsed.Add(newEffect.name, "");
                            newStatuses.Add(newEffect.name);
                        }
                        statusUsed[newEffect.name] += "{";
                    }
                    if (data is StatusEffectInstantApplyEffect effect7)
                    {
                        StatusEffectData newEffect = effect7.effectToApply;
                        if (newEffect == null) { continue; }
                        if (!statusUsed.ContainsKey(newEffect.name))
                        {
                            statusUsed.Add(newEffect.name, "");
                            newStatuses.Add(newEffect.name);
                        }
                        statusUsed[newEffect.name] += "{";
                    }
                    if (data is StatusEffectInstantEatSomething effect8)
                    {
                        StatusEffectData newEffect = effect8.eatEffect;
                        if (newEffect == null) { continue; }
                        if (!statusUsed.ContainsKey(newEffect.name))
                        {
                            statusUsed.Add(newEffect.name, "");
                            newStatuses.Add(newEffect.name);
                        }
                        statusUsed[newEffect.name] += "{";
                    }
                    if (data is StatusEffectInstantHealFullGainEqualX effect9)
                    {
                        StatusEffectData newEffect = effect9.effectToGain;
                        if (newEffect == null) { continue; }
                        if (!statusUsed.ContainsKey(newEffect.name))
                        {
                            statusUsed.Add(newEffect.name, "");
                            newStatuses.Add(newEffect.name);
                        }
                        statusUsed[newEffect.name] += "{";
                    }
                    if (data is StatusEffectInstantLoseX effect10)
                    {
                        StatusEffectData newEffect = effect10.statusToLose;
                        if (newEffect == null) { continue; }
                        if (!statusUsed.ContainsKey(newEffect.name))
                        {
                            statusUsed.Add(newEffect.name, "");
                            newStatuses.Add(newEffect.name);
                        }
                        statusUsed[newEffect.name] += "{";
                    }
                    if (data is StatusEffectInstantMultiple effect11)
                    {
                        foreach(StatusEffectData newEffect in effect11.effects)
                        {
                            if (!statusUsed.ContainsKey(newEffect.name))
                            {
                                statusUsed.Add(newEffect.name, "");
                                newStatuses.Add(newEffect.name);
                            }
                            statusUsed[newEffect.name] += "{";
                        }                       
                    }
                    if (data is StatusEffectInstantSummon effect12)
                    {
                        StatusEffectData newEffect1 = effect12.targetSummon;
                        if (newEffect1 == null) { continue; }
                        if (!statusUsed.ContainsKey(newEffect1.name))
                        {
                            statusUsed.Add(newEffect1.name, "");
                            newStatuses.Add(newEffect1.name);
                        }
                        statusUsed[newEffect1.name] += "{";
                        foreach (StatusEffectData newEffect in effect12.withEffects)
                        {
                            if (!statusUsed.ContainsKey(newEffect.name))
                            {
                                statusUsed.Add(newEffect.name, "");
                                newStatuses.Add(newEffect.name);
                            }
                            statusUsed[newEffect.name] += "{";
                        }
                    }
                    if (data is StatusEffectSummon effect13)
                    {
                        StatusEffectData newEffect = effect13.gainTrait;
                        if (newEffect == null) { continue; }
                        if (!statusUsed.ContainsKey(newEffect.name))
                        {
                            statusUsed.Add(newEffect.name, "");
                            newStatuses.Add(newEffect.name);
                        }
                        statusUsed[newEffect.name] += "{";
                    }
                    if (data is StatusEffectWhileActiveAlliesImmuneToX effect14)
                    {
                        StatusEffectData newEffect = effect14.immunityEffect;
                        if (newEffect == null) { continue; }
                        if (!statusUsed.ContainsKey(newEffect.name))
                        {
                            statusUsed.Add(newEffect.name, "");
                            newStatuses.Add(newEffect.name);
                        }
                        statusUsed[newEffect.name] += "{";
                    }
                    if (data is StatusEffectWhileActiveApplyXToEachCardPlayed effect15)
                    {
                        StatusEffectData newEffect = effect15.effectToApply;
                        if (newEffect == null) { continue; }
                        if (!statusUsed.ContainsKey(newEffect.name))
                        {
                            statusUsed.Add(newEffect.name, "");
                            newStatuses.Add(newEffect.name);
                        }
                        statusUsed[newEffect.name] += "{";
                    }
                }
            }

            UnityEngine.Debug.Log("=====================Phase 3: Output=======================");
            List<StatusEffectData> statuses = AddressableLoader.GetGroup<StatusEffectData>("StatusEffectData");
            foreach(StatusEffectData status in statuses)
            {
                if (statusUsed.ContainsKey(status.name))
                {
                    UnityEngine.Debug.Log($"{status.name}|{statusUsed[status.name]}|");
                }
                else
                {
                    UnityEngine.Debug.Log($"{status.name}|[Unused]");
                }
            }
        }

        public static void CollectStatuses(bool displayName)
        {
            UnityEngine.Debug.Log("=======StatusEffectData=======");
            List<StatusEffectData> statusEffects = AddressableLoader.GetGroup<StatusEffectData>("StatusEffectData");
            foreach(StatusEffectData effect in statusEffects) 
            {
                string output = (displayName) ? effect.name + ": ": string.Empty;
                string type = effect.type;
                if (!type.IsNullOrEmpty())
                {
                    if (!types.ContainsKey(type))
                    {
                        types.Add(type, effect.name);
                    }
                    else
                    {
                        types[type] += ", " + effect.name;
                    } 
                }
                int eventPriority = effect.eventPriority;
                if (eventPriority != 0)
                {
                    if (!eventPriorities.ContainsKey(eventPriority))
                    {
                        eventPriorities.Add(eventPriority, effect.name);
                    }
                    else
                    {
                        eventPriorities[eventPriority] += ", " + effect.name;
                    }
                }
                output += $"{type}, {eventPriority}";
                UnityEngine.Debug.Log(output);
            }
            UnityEngine.Debug.Log("=======Types=======");
            foreach(string key in types.Keys)
            {
                UnityEngine.Debug.Log($"{key}:{types[key]}");
            }
            UnityEngine.Debug.Log("=======EventPriorities=======");
            foreach(int key in eventPriorities.Keys)
            {
                UnityEngine.Debug.Log($"{key}:{eventPriorities[key]}");
            }
        }

        public static Dictionary<string,string> types = new Dictionary<string,string>();
        public static Dictionary<int, string> eventPriorities = new Dictionary<int, string>();



        public override void Load()
        {
            CollectWhoUsesStatuses();
            base.Load();
        }

        public override void Unload()
        {
            base.Unload();
        }
    }
}
