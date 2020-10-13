using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.BO
{
    public class MissileBO
    {
        [Required]
        public string MissileID { get; set; }
        [Required]
        public double Lon { get; set; }
        [Required]
        public double Lat { get; set; }
        [Required]
        public double Alt { get; set; }
        [Required]
        public double Yield { get; set; }
    }
}
