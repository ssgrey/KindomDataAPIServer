using System;

namespace KindomDataAPIServer.DataService
{
    public sealed class ApiRequestTelemetry
    {
        public string TraceName { get; set; }
        public int? FinalStatusCode { get; set; }
        public bool Succeeded { get; set; }
        public bool Retried { get; set; }
        public int AttemptCount { get; set; }
        public TimeSpan TotalElapsed { get; set; }
        public string Signal { get; set; }

        public bool HasProtectionSignal =>
            Retried ||
            string.Equals(Signal, "timeout", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Signal, "network-retry", StringComparison.OrdinalIgnoreCase) ||
            FinalStatusCode == 408 ||
            FinalStatusCode == 429 ||
            FinalStatusCode >= 500;
    }
}
