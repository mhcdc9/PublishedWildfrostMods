using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotPotato
{
    public class HotPotato : WildfrostMod
    {
        public HotPotato(string modDirectory) : base(modDirectory)
        {
            
        }
        public override string GUID => "mhcdc9.wildfrost.HotPotato";

        public override string[] Depends => new string[] { };

        public override string Title => "Hot Potato";

        public override string Description => "Your leader starts with a countdown. I wonder what happens to the poor soul who keeps it at the end?";


    }
}
