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
    public class DamageVO : ICloneable, IEquatable<DamageVO>
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

        public override bool Equals(object obj)
        {
            return Equals(obj as DamageVO);
        }

        public bool Equals(DamageVO other)
        {
            if (other == null) return false;
            if (other.missileList.Count != missileList.Count) return false;
            if (other.statusTimeRanges.Count != statusTimeRanges.Count) return false;

            for (int i = 0; i < missileList.Count; i++)
            {
                if (!other.missileList[i].Equals(missileList[i]))
                    return false;
            }

            for (int i = 0; i < statusTimeRanges.Count; i++)
            {
                if (!other.statusTimeRanges[i].Equals(statusTimeRanges[i]))
                    return false;
            }
            return true;

            //return other != null &&
            //       EqualityComparer<List<StatusTimeRangesVO>>.Default.Equals(statusTimeRanges, other.statusTimeRanges) &&
            //       EqualityComparer<List<MissileListVO>>.Default.Equals(missileList, other.missileList);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(statusTimeRanges, missileList);
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

        public static bool operator ==(DamageVO left, DamageVO right)
        {
            return EqualityComparer<DamageVO>.Default.Equals(left, right);
        }

        public static bool operator !=(DamageVO left, DamageVO right)
        {
            return !(left == right);
        }
    }
}
