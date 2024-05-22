using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tokens
{
    public class ScriptableAmountMissingHealth : ScriptableAmount
    {
        public int min = 0;
        public int max = 100;
        public static ScriptableAmountMissingHealth CreateInstance(int min, int max)
        {
            ScriptableAmountMissingHealth script = ScriptableObject.CreateInstance<ScriptableAmountMissingHealth>();
            script.min = min;
            script.max = max;
            return script;
        }

        public override int Get(Entity entity)
        {
            int amount = Math.Max(entity.hp.max - entity.hp.current, min);
            return Math.Min(amount, max);
        }
    }
}
