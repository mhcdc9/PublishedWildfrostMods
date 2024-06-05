using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Sync
{
    public class StatusEffectSync : StatusEffectApplyX
    {
        public static int SyncOnScreen = 0;

        protected bool effectActive = false;
        protected List<Entity> entities = new List<Entity>();
        protected override void Init()
        {
            base.Init();
            base.OnEntityDestroyed += Destroyed;
        }

        public override bool RunTurnEndEvent(Entity entity)
        {
            if (entity == target)
            {
                SyncOnScreen++;
            }
            return false;
        }

        public IEnumerator Activate(int amount)
        {
            if (!effectActive)
            {
                entities = GetTargets();
                yield return Run(GetTargets(),amount);
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

        public IEnumerator Deactivate()
        {
            if (effectActive)
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
        }
    }

    public class ActionSync : PlayAction
    {
        public override bool IsRoutine => true;
        private bool not;

        public ActionSync(bool descync)
        {
            not = descync;
        }

        public override IEnumerator Run()
        {
            if (Battle.instance == null)
            {
                yield break;
            }
            StatusEffectSystem.activeEffects.Freeze();
            foreach(StatusEffectData status in StatusEffectSystem.activeEffects)
            {
                if (status is StatusEffectSync sync)
                {
                    yield return not ? sync.Deactivate() : sync.Activate(1);
                }
            }
            StatusEffectSystem.activeEffects.Thaw();
        }
    }

}
