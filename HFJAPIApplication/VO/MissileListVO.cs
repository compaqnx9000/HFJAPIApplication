using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.VO
{
    [Serializable]
    public class MissileListVO : IEquatable<MissileListVO>
    {

        public MissileListVO(string missileID, double impactTimeUtc, int damageLevel)
        {
            MissileID = missileID;
            ImpactTimeUtc = impactTimeUtc;
            DamageLevel = damageLevel;
        }

        public string MissileID { get; set; }
        public double ImpactTimeUtc { get; set; }
        [JsonIgnore]
        public int DamageLevel { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as MissileListVO);
        }

        public bool Equals(MissileListVO other)
        {
            return other != null &&
                   MissileID == other.MissileID &&
                   ImpactTimeUtc == other.ImpactTimeUtc &&
                   DamageLevel == other.DamageLevel;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MissileID, ImpactTimeUtc, DamageLevel);
        }

        public static bool operator ==(MissileListVO left, MissileListVO right)
        {
            return EqualityComparer<MissileListVO>.Default.Equals(left, right);
        }

        public static bool operator !=(MissileListVO left, MissileListVO right)
        {
            return !(left == right);
        }
    }
}
