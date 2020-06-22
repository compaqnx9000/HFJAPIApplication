using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace HFJAPIApplication.VO
{
    [Serializable]
    public class DamageVO : ICloneable
    {
        public DamageVO()
        {
            statusTimeRanges = new List<StatusTimeRangesVO>();
            missileList = new List<MissileListVO>();
        }
        public object Clone()
        {
            var result = new DamageVO();
            result.launchUnitInfo = launchUnitInfo;
            result.statusTimeRanges = statusTimeRanges;
            result.missileList = missileList;
            result.nonce = nonce;
            result.warBase = warBase;
            result.platform = platform;
            result.warZone = warZone;
            result.combatZone = combatZone;
            result.platoon = platoon;
            result.missileNum = missileNum;
            result.lon = lon;
            result.lat = lat;
            result.alt = alt;
            return result;

        }
        public LaunchUnitInfoVO launchUnitInfo { get; set; }
        public List<StatusTimeRangesVO> statusTimeRanges { get; set; }
        public List<MissileListVO> missileList { get; set; }
        public string nonce { get; set; }

        // 下面3个属性用于筛选
        [JsonIgnore]
        public string warBase { get; set; }//基地
        [JsonIgnore]
        public string platform { get; set; }//发射平台


        // 下面是用于给selectVO的
        [JsonIgnore] 
        public string warZone { get; set; }
        [JsonIgnore]
        public string combatZone { get; set; }
        [JsonIgnore]
        public string platoon { get; set; }
        [JsonIgnore]
        public double missileNum { get; set; }
        [JsonIgnore]
        public double lon { get; set; }
        [JsonIgnore]
        public double lat { get; set; }
        [JsonIgnore]
        public double alt { get; set; }

    }
}
