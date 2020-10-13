using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.VO
{
    public class MissileAreaVO
    {
        public MissileAreaVO(double damageRadius, double lon, double lat, double alt)
        {
            this.damageRadius = damageRadius;
            this.lon = lon;
            this.lat = lat;
            this.alt = alt;
        }

        public double damageRadius { get; set; }
        public double lon { get; set; }
        public double lat { get; set; }
        public double alt { get; set; }


    }
}
