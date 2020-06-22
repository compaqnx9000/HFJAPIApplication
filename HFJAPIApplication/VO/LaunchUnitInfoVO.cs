using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.VO
{
    public class LaunchUnitInfoVO
    {
        public LaunchUnitInfoVO(string launchUnit, string warBase, string brigade, string missileNo)
        {
            LaunchUnit = launchUnit;
            WarBase = warBase;
            Brigade = brigade;
            MissileNo = missileNo;
        }

        public string LaunchUnit { get; set; }
        public string WarBase { get; set; }
        public string Brigade { get; set; }
        public string MissileNo { get; set; }
    }
}
