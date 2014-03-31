using System;
using System.Diagnostics;

namespace TraceRouteMapper.CSharp.ConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var cmd = "cmd";
            var startInfo = new ProcessStartInfo(cmd, "/c tracert www.vg.no");

            var proc = new Process();
            proc.StartInfo = startInfo;
            proc.Start();

            Console.WriteLine("Running cmd...");

            Console.ReadLine();
        }
    }
}
