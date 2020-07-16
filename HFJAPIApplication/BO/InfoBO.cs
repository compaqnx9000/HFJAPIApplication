using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.BO
{
    public class InfoBO
    {
        public InfoBO()
        {
            nuclear_warheads = new List<string>();
        }
        
        public ObjectId _id { get; set; }
        public string brigade { get; set; }
        public string warBase { get; set; }
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
        public double fallout_01 { get; set; }
        public double fallout_02 { get; set; }
        public double fallout_03 { get; set; }
        public double lon { get; set; }
        public double lat { get; set; }
        public double alt { get; set; }
        public string launchUnit { get; set; }
        public string platform { get; set; }
        public string warZone { get; set; }
        public string combatZone { get; set; }
        public string platoon { get; set; }
        public string missileNo { get; set; }
        public double missileNum { get; set; }
        [JsonIgnore]
        public List<string> nuclear_warheads { get; set; }
        //07-01新加的
        public double prepareTime { get; set; }
        public double targetBindingTime { get; set; }
        public double defenseBindingTime { get; set; }
        //07-11新加的
        public double fireRange { get; set; }

    }
}
