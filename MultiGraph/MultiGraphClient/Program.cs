using System;
using Trinity;

namespace MultiGraphClient 
{
    class Program
    {
        static void Main(string[] args)
        {
          // Trinity doesn't load the config file correctly if we don't tell it to.
          TrinityConfig.LoadConfig();
          TrinityConfig.CurrentRunningMode = RunningMode.Client;
        }
    }
}
