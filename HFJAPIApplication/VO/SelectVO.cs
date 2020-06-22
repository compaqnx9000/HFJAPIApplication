using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.VO
{
    public class SelectVO
    {
        public SelectVO()
        {
            statusTimeRanges = new List<StatusTimeRangesVO>();
            missileList      = new List<MissileListVO>();
        }

        public string launchUnit { get; set; }
        public string platform { get; set; }
        public string warZone { get; set; }
        public string combatZone { get; set; }
        public string brigade { get; set; }
        public string platoon { get; set; }
        public string missileNo { get; set; }
        public double missileNum { get; set; }
        public double lon { get; set; }
        public double lat { get; set; }
        public double alt { get; set; }
        public List<StatusTimeRangesVO> statusTimeRanges { get; set; }
        public List<MissileListVO> missileList { get; set; }
    }
}
