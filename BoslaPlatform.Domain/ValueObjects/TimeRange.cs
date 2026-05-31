namespace BoslaPlatform.Domain.ValueObjects
{
    public class TimeRange
    {
        public DateTimeOffset Start { get; }

        public DateTimeOffset End { get; }

        public TimeRange(DateTimeOffset start, DateTimeOffset end)
        {
            if (end <= start)
                throw new ArgumentException("Invalid time range");
            Start = start;
            End = end;
        }
    }
}
