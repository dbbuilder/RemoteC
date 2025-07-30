using System;
using System.Threading.Tasks;

namespace RemoteC.Tests.Performance
{
    /// <summary>
    /// Entry point for performance tests
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await PerformanceTestRunner.Run(args);
        }
    }
}