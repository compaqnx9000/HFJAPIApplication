using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.VO
{
    public class MissileListVO
    {
        public MissileListVO(string missileID, double impactTimeUtc,int damageLevel)
        {
            MissileID = missileID;
            ImpactTimeUtc = impactTimeUtc;
            DamageLevel = damageLevel;
        }

        public string MissileID { get; set; }
        public double ImpactTimeUtc { get; set; }
        [JsonIgnore]
        public int DamageLevel { get; set; }
    }
}
