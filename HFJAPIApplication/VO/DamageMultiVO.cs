using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.VO
{
    public class DamageMultiVO
    {
        public DamageMultiVO(string damageType, List<MultiVO> damageResults)
        {
            this.damageType = damageType;
            this.damageResults = damageResults;
        }

        public string damageType { get; set; }
        public List<MultiVO> damageResults { get; set; }

    }
}
