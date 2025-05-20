using MultiplayerBase.Handlers;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Image = UnityEngine.UI.Image;
using Color = UnityEngine.Color;
using Net = MultiplayerBase.Handlers.HandlerSystem;
using MultiplayerBase;

namespace Sync
{
    public class StatusEffectSync : StatusEffectApplyX
    {


        public bool ongoing = true;

        protected bool effectActive = false;
        protected List<Entity> entities = new List<Entity>();
        protected int amountApplied = 0;
        public override void Init()
        {
            base.Init();
            base.OnEntityDestroyed += Destroyed;
            base.OnEffectBonusChanged += EffectChanged;
        }

        public override bool RunCardPlayedEvent(Entity entity, Entity[] targets)
        {
            if (entity == target && !target.silenced && !SyncMain.sentSyncMessage && !Battle.IsOnBoard(target))
            {
                Debug.Log($"[Sync] {target.data.title}");
                SyncMain.SyncOthers();
            }
            return false;
        }

        public IEnumerator Activate(int amount)
        {
            if (effectActive && ongoing)
            {
                yield break;
            }
            if (target.silenced)
            {
                yield break;
            }
            effectActive = true;
            StatusIcon icon = target?.display?.FindStatusIcon(type);
            Transform cycle = icon?.transform?.GetChild(0);
            if (icon != null)
            {
                icon.GetComponent<Image>().color = Color.white;
            }
            if (cycle != null)
            {
                cycle.GetComponent<Image>().color = Color.white;
                cycle.GetComponent<SyncArrows>().enabled = true;
            }

            if (effectToApply != null)
            {
                entities = GetTargets();
                amountApplied = GetAmount();
                Routine.Clump clumpy = new Routine.Clump();
                for (int i = 0; i < entities.Count; i++)
                {
                    yield return StatusEffectSystem.Apply(entities[i], target, effectToApply, amountApplied, temporary: true);
                }                
            }
        }

        public IEnumerator EffectChanged()
        {
            if (effectActive && canBeBoosted && ongoing)
            {
                yield return Deactivate();
                yield return Activate(0);
            }
        }

        public override bool RunEntityDestroyedEvent(Entity entity, DeathType deathType)
        {
            return entity == target;
        }

        public IEnumerator Destroyed(Entity entity, DeathType deathType)
        {
            return Deactivate();
        }

        public IEnumerator FindAndRemoveStacks(int amountToRemove)
        {
            StatusEffectData targetStatus = null;
            foreach (Entity entity in entities)
            {
                if (!entity.IsAliveAndExists())
                {
                    continue;
                }
                foreach (StatusEffectData status in entity.statusEffects)
                {
                    if (status.name == effectToApply.name)
                    {
                        targetStatus = status;
                        break;
                    }
                }
                if (targetStatus != null)
                {
                    yield return targetStatus.RemoveStacks(amountToRemove, true);
                    targetStatus = null;
                }
                entity.display.promptUpdateDescription = true;
                entity.PromptUpdate();
            }
            entities.Clear();
        }

        public IEnumerator Deactivate()
        {
            if (effectActive && ongoing)
            {
                yield return FindAndRemoveStacks(amountApplied);

            }
            effectActive = false;
            if (target.IsAliveAndExists())
            {
                StatusIcon icon = target?.display?.FindStatusIcon(type);
                Transform cycle = icon?.transform?.GetChild(0);
                if (icon != null)
                {
                    icon.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
                }
                if (cycle != null)
                {
                    cycle.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f, 1f);
                    cycle.GetComponent<SyncArrows>().enabled = false;
                }
            }
        }
    }

    public class ActionSync : PlayAction
    {
        public override bool IsRoutine => true;
        private bool endSync = false;
        private int combo;

        public ActionSync(int combo)
        {
            this.combo = combo;
            endSync = (combo == 0);
        }

        public override IEnumerator Run()
        {
            yield return new WaitUntil(() => (Battle.instance == null || Battle.instance.phase != Battle.Phase.Battle || !HandlerBattle.instance.Blocking) );
            if (Battle.instance == null)
            {
                yield break;
            }
            SyncMain.syncCombo = combo;
            if (combo == 0)
            {
                //Net.CHT_Handler(Net.self, "Not Synched...");
                //MultTextManager.AddEntry("Not Synched...", 0.4f, Color.white, 2f);
            }
            else
            {
                //string s = (combo > 1) ? "Synched!" : $"Synched (x{combo})!";
                //s = string.Concat("<size=0.55><color=#FC5>", s, "</color></size>");
                //Net.CHT_Handler(Net.self, s);
                MultTextManager.AddEntry("Synched!", 0.55f, new Color(1f, 0.75f, 0.38f), 2f);
            }
            SyncMain.sync = !endSync;
            StatusEffectSystem.activeEffects.Freeze();
            Routine.Clump clump = new Routine.Clump();
            foreach (StatusEffectData status in StatusEffectSystem.activeEffects)
            {
                if (status is StatusEffectSync sync)
                {
                    clump.Add(endSync ? sync.Deactivate() : sync.Activate(combo));
                }
            }
            yield return clump.WaitForEnd();
            StatusEffectSystem.activeEffects.Thaw();
        }
    }

    public class SyncArrows : MonoBehaviour
    {
        public Vector3 rotation = new Vector3(0, 0, -180f);

        public void Start()
        {
            float z = Dead.PettyRandom.Range(-10f, 10f);
            rotation.z += z;
        }

        public void Update()
        {
            Vector3 localRotation = transform.localEulerAngles;
            localRotation += rotation * Time.deltaTime;
            transform.localEulerAngles = localRotation;
        }
    }
}
