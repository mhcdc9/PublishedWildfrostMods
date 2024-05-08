using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Tokens
{
    public static class Extensions
    {
        public static CardUpgradeDataBuilder CreateToken(this CardUpgradeDataBuilder b, string name, string title)
        {
            return b.Create(name)
                .WithTitle(title)
                .SetCanBeRemoved(true)
                .WithType(CardUpgradeData.Type.Token);
        }

        public static StatusEffectDataBuilder CreateStatusToken<T>(this StatusEffectDataBuilder b, string name, string type) where T : StatusEffectData
        {
            return b.Create<T>(name)
                .WithCanBeBoosted(false)
                .WithIconGroupName("counter")
                .WithIsStatus(true)
                .WithStackable(false)
                .WithType(type)
                .WithVisible(true);
        }
    }

    
}
