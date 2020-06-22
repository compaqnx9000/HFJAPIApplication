using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.VO
{
    public class StatusTimeRangesVO
    {
        public StatusTimeRangesVO(double startTimeUtc, double endTimeUtc, int status)
        {
            StartTimeUtc = startTimeUtc;
            EndTimeUtc = endTimeUtc;
            Status = status;
        }

        public double StartTimeUtc { get; set; }
        public double EndTimeUtc { get; set; }
        public int Status { get; set; }
    }
}
