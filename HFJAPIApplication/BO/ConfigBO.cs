using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.BO
{
    [Serializable]
    public class ConfigBO
    {
        public ObjectId _id { get; set; }
        public string platform { get; set; }
        public double shock_wave_01 { get; set; }
        public double shock_wave_02 { get; set; }
        public double shock_wave_03 { get; set; }
        public double nuclear_radiation_01 { get; set; }
        public double nuclear_radiation_02 { get; set; }
        public double nuclear_radiation_03 { get; set; }
        public double thermal_radiation_01 { get; set; }
        public double thermal_radiation_02 { get; set; }
        public double thermal_radiation_03 { get; set; }
        public double nuclear_pulse_01 { get; set; }
        public double nuclear_pulse_02 { get; set; }
        public double nuclear_pulse_03 { get; set; }
    }
}
