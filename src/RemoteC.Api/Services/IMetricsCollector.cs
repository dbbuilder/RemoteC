using System.Collections.Generic;

namespace RemoteC.Api.Services
{
    public interface IMetricsCollector
    {
        void RecordGauge(string name, double value, Dictionary<string, string>? tags = null);
        void RecordCounter(string name, double value = 1, Dictionary<string, string>? tags = null);
        void RecordHistogram(string name, double value, Dictionary<string, string>? tags = null);
        void RecordTimer(string name, double milliseconds, Dictionary<string, string>? tags = null);
        double GetGaugeValue(string name, Dictionary<string, string>? tags = null);
        long GetCounterValue(string name, Dictionary<string, string>? tags = null);
    }
}