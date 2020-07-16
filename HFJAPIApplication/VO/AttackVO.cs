using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.VO
{
    public class AttackVO
    {
        public AttackVO(string launchUnit, string platform, string warZone, double distance, string warBase, string brigade)
        {
            LaunchUnit = launchUnit;
            Platform = platform;
            WarZone = warZone;
            Distance = distance;
            WarBase = warBase;
            Brigade = brigade;
        }

        public string LaunchUnit { get; set; }
        public string Platform { get; set; }
        public string WarZone { get; set; }
        public double Distance { get; set; }
        public string WarBase { get; set; }
        public string Brigade { get; set; }
    }
}
