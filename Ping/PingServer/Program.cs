using System;
using Trinity;

namespace PingServer 
{
    class Program
    {
        static void Main(string[] args)
        {
          // Trinity doesn't load the config file correctly if we don't tell it to
          TrinityConfig.LoadConfig();
          TrinityConfig.CurrentRunningMode = RunningMode.Server;
          // Create our PingServer implementation an start it.
          SimplePingServer server = new SimplePingServer();
          server.Start();

          Console.ReadKey();
        }
    }
}
