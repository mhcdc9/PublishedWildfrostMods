using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stabilizer.Compatibility
{
    public class ModComp : Attribute
    {
        public string[] alias;
        public ModComp(params string[] alias)
        {
            this.alias = alias;
        }
    }
}
