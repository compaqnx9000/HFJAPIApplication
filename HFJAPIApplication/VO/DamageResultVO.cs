using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.VO
{
    public class DamageResultVO
    {
        public string DamageType { get; set; }
        public double DamageRadius { get; set; }
        public double Lon { get; set; }
        public double Lat { get; set; }
        public double Alt { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }

        public DamageResultVO(string damageType, double damageRadius, double lon,
                                double lat, double alt, double value, string unit)
        {
            this.DamageType = damageType ?? throw new ArgumentNullException(nameof(damageType));
            this.DamageRadius = damageRadius;
            this.Lon = lon;
            this.Lat = lat;
            this.Alt = alt;
            this.Value = value;
            this.Unit = unit;
        }
    }
}
