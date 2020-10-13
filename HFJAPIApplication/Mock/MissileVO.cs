using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.Mock
{
    [Serializable]
    public class MissileVO
    {
        public MissileVO(string missileID, string warHeadNo, double yield, double lon, double lat, double alt, double impactTimeUTc, double measurement, double attackAccuracy,string nonce)
        {
            this.missileID = missileID;
            this.warHeadNo = warHeadNo;
            this.yield = yield;
            this.lon = lon;
            this.lat = lat;
            this.alt = alt;
            this.impactTimeUtc = impactTimeUTc;
            this.measurement = measurement;
            this.attackAccuracy = attackAccuracy;
            this.nonce = nonce;
        }

        public string missileID { get; set; }
        public string warHeadNo { get; set; }
        public double yield { get; set; }
        public double lon { get; set; }
        public double lat { get; set; }
        public double alt { get; set; }
        public double impactTimeUtc { get; set; }
        public double measurement { get; set; }
        public double attackAccuracy { get; set; }
        public string nonce { get; set; }
    }
}
