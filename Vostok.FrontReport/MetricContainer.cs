using Vostok.Metrics.Meters;

namespace Vostok.FrontReport
{
    public class MetricContainer
    {
        public ICounter SuccessCounter { get; set; }
        public ICounter ErrorCounter { get; set; }
    }
}