using System;

namespace TCXFileLapExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            var pointCollector = new PointCollector();
            pointCollector.ProcessActivityFiles();
        }
    }
}
