using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.VO
{
    public class StatusTimeRangesVO : IEquatable<StatusTimeRangesVO>
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

        public override bool Equals(object obj)
        {
            return Equals(obj as StatusTimeRangesVO);
        }

        public bool Equals(StatusTimeRangesVO other)
        {
            return other != null &&
                   StartTimeUtc == other.StartTimeUtc &&
                   EndTimeUtc == other.EndTimeUtc &&
                   Status == other.Status;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StartTimeUtc, EndTimeUtc, Status);
        }

        public static bool operator ==(StatusTimeRangesVO left, StatusTimeRangesVO right)
        {
            return EqualityComparer<StatusTimeRangesVO>.Default.Equals(left, right);
        }

        public static bool operator !=(StatusTimeRangesVO left, StatusTimeRangesVO right)
        {
            return !(left == right);
        }
    }
}
