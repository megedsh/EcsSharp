using System;

namespace EcsSharp.Logging.ThrottledLog
{
    public class ThrottledLogConfig
    {
        public static ThrottledLogConfig ByTime(int ms)            => new ThrottledLogConfig(TimeSpan.FromMilliseconds(ms));
        public static ThrottledLogConfig ByTime(TimeSpan timeSpan) => new ThrottledLogConfig(timeSpan);
        public static ThrottledLogConfig ByCount(uint count)       => new ThrottledLogConfig(count);

        private ThrottledLogConfig(TimeSpan timeSpan)
        {
            Span = timeSpan;
            Count = 0;
        }

        private ThrottledLogConfig(uint count)
        {
            Count = count;
            Span = TimeSpan.MinValue;
        }

        public TimeSpan Span { get; }
        public uint Count { get; }
    }
}