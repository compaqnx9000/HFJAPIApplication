using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.VO
{
    public class TestResultVO
    {
        public TestResultVO(string platform, double lng, double lat, double dis)
        {
            this.platform = platform;
            this.lng = lng;
            this.lat = lat;
            this.dis = dis;
        }

        public string platform { get; set; }
        public double lng { get; set; }
        public double lat { get; set; }
        public double dis { get; set; }
    }
}
