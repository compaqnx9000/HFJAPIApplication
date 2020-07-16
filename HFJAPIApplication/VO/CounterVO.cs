using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HFJAPIApplication.VO
{
    [Serializable]
    public class TimeRange : IEquatable<TimeRange>
    {

        public TimeRange(double startTimeUtc, double endTimeUtc, int timeTypeName)
        {
            StartTimeUtc = startTimeUtc;
            EndTimeUtc = endTimeUtc;
            TimeTypeName = timeTypeName;
        }

        public double StartTimeUtc { get; set; }
        public double EndTimeUtc { get; set; }
        public int TimeTypeName { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as TimeRange);
        }

        public bool Equals(TimeRange other)
        {
            return other != null &&
                   StartTimeUtc == other.StartTimeUtc &&
                   EndTimeUtc == other.EndTimeUtc &&
                   TimeTypeName == other.TimeTypeName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StartTimeUtc, EndTimeUtc, TimeTypeName);
        }

        public static bool operator ==(TimeRange left, TimeRange right)
        {
            return EqualityComparer<TimeRange>.Default.Equals(left, right);
        }

        public static bool operator !=(TimeRange left, TimeRange right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public class CounterVO : IEquatable<CounterVO>
    {

        public CounterVO()
        {
            timeRanges = new List<TimeRange>();
        }

        public LaunchUnitInfoVO launchUnitInfo { get; set; }
        public List<TimeRange> timeRanges { get; set; }
        public string nonce { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as CounterVO);
        }

        public bool Equals(CounterVO other)
        {
            if (other == null) return false;
            if (other.timeRanges.Count != timeRanges.Count) return false;

            for (int i = 0; i < timeRanges.Count; i++)
            {
                if (!other.timeRanges[i].Equals(timeRanges[i]))
                    return false;
            }

            return true;
            //return other != null &&
            //       EqualityComparer<LaunchUnitInfoVO>.Default.Equals(launchUnitInfo, other.launchUnitInfo) &&
            //       EqualityComparer<List<TimeRange>>.Default.Equals(timeRanges, other.timeRanges) &&
            //       nonce == other.nonce;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(launchUnitInfo, timeRanges, nonce);
        }

        public static bool operator ==(CounterVO left, CounterVO right)
        {
            return EqualityComparer<CounterVO>.Default.Equals(left, right);
        }

        public static bool operator !=(CounterVO left, CounterVO right)
        {
            return !(left == right);
        }
    }
}
