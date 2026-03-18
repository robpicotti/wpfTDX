using System;

namespace wpfTDX
{
    public class ProcessRunDataModel
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double DurationMinutes { get; set; }
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public double? AvgPerTicker { get; set; }   // seconds per ticker, null when not in log
    }
}
