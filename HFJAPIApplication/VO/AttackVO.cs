using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.VO
{
    public class AttackVO
    {
        public AttackVO(string launchUnit, string platform, string warZone, double distance,
            string warBase, string brigade,string name,string useState)
        {
            LaunchUnit = launchUnit;
            Platform = platform;
            WarZone = warZone;
            Distance = distance;
            WarBase = warBase;
            Brigade = brigade;
            Name = name;
            UseState = useState;

        }

        public string LaunchUnit { get; set; }
        public string Platform { get; set; }
        public string WarZone { get; set; }
        public double Distance { get; set; }
        public string WarBase { get; set; }
        public string Brigade { get; set; }

        public string Name { get; set; } // 2020-10-13 add
        public string UseState { get; set; }// 2020-10-13 add
    }
}
