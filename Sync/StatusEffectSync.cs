using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Net = MultiplayerBase.Handlers.HandlerSystem;

namespace Sync
{
    public class StatusEffectSync : StatusEffectApplyX
    {


        public bool ongoing = true;

        protected bool effectActive = false;
        protected List<Entity> entities = new List<Entity>();
        protected override void Init()
        {
            base.Init();
            base.OnEntityDestroyed += Destroyed;
        }

        public override bool RunCardPlayedEvent(Entity entity, Entity[] targets)
        {
            if (entity == target && !SyncMain.sentSyncMessage)
            {
                Debug.Log($"[Sync]{target.data.title}");
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
            if (effectToApply != null)
            {
                entities = GetTargets();
                yield return Run(GetTargets(), amount);
            }
            effectActive = true;
        }

        public override bool RunEntityDestroyedEvent(Entity entity, DeathType deathType)
        {
            return entity == target;
        }

        public IEnumerator Destroyed(Entity entity, DeathType deathType)
        {
            return Deactivate();
        }

        public IEnumerator Deactivate()
        {
            if (effectActive && ongoing)
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
                        yield return targetStatus.RemoveStacks(count, true);
                        targetStatus = null;
                    }
                    entity.display.promptUpdateDescription = true;
                    entity.PromptUpdate();
                }
                entities.Clear();
            }
            effectActive = false;
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
            yield return new WaitUntil(() => (Battle.instance == null || Battle.instance.phase == Battle.Phase.Play) );
            if (Battle.instance == null)
            {
                yield break;
            }
            SyncMain.syncCombo = combo;
            if (combo == 0)
            {
                Net.CHT_Handler(Net.self, "Not Synched...");
            }
            else
            {
                Net.CHT_Handler(Net.self, $"<size=0.55><color=#FC5>Synched!</color></size>");
            }
            SyncMain.sync = !endSync;
            StatusEffectSystem.activeEffects.Freeze();
            foreach (StatusEffectData status in StatusEffectSystem.activeEffects)
            {
                if (status is StatusEffectSync sync)
                {
                    yield return endSync ? sync.Deactivate() : sync.Activate(combo);
                }
            }
            StatusEffectSystem.activeEffects.Thaw();
        }
    }
}
