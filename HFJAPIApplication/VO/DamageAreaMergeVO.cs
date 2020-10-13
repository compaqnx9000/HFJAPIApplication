using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.VO
{
    /// <summary>
    /// 给"damagearea/merge"返回使用
    /// </summary>
    public class DamageAreaMergeVO
    {
        public DamageAreaMergeVO(string damageType, string damageGeometry, double value, string unit)
        {
            this.damageGeometry = damageGeometry;
            this.damageType = damageType;
            this.value = value;
            this.unit = unit;
        }

        public string damageGeometry { get; set; }
        public string damageType { get; set; }
        public double value { get; set; }
        public string unit { get; set; }
    }
}
