using System;
using Trinity;

namespace PingClient 
{
    class Program
    {
        static void Main(string[] args)
        {
          // Trinity doesn't load the config file correctly if we don't tell it to.
          TrinityConfig.LoadConfig();
          TrinityConfig.CurrentRunningMode = RunningMode.Client;

          using (var request = new PingMessageWriter("Ping!1")) {
            Global.CloudStorage.SynPingToPingServer(0, request);
          }

          using (var request = new PingMessageWriter("Ping!2")) {
            Global.CloudStorage.AsynPingToPingServer(0, request);
          }

          using (var request = new PingMessageWriter("Ping!3")) {
            using (var response = Global.CloudStorage.SynEchoPingToPingServer(0, request)) {
              Console.WriteLine("Server Response: {0}", response.Message);
            }
          }
        }
    }
}
