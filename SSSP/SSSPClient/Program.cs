using System;
using System.Collections.Generic;
using Trinity;

namespace SSSPClient 
{
    class Program
    {
        static void Main(string[] args)
        {
          // Trinity doesn't load the config file correctly if we don't tell it to.
          TrinityConfig.LoadConfig();
          TrinityConfig.CurrentRunningMode = RunningMode.Client;


          while (true) {
            string command = Console.ReadLine();
            long id = long.Parse(command.Split(' ')[1]);

            if (command.StartsWith("root")) {
              StartSSSP(id);
            } else if (command.StartsWith("distance")) {
              var cell = Global.CloudStorage.LoadSSSPCell(id);

              Console.WriteLine("Distance: {0}", cell.distance);

              Console.Write("Neighbors: ");
              List<long> neighbors = cell.neighbors;
              for (int i = 0; i < neighbors.Count; i++) {
                  Console.Write("{0} ", neighbors[i]);
              }
              Console.WriteLine();
            }
          }
        }

        private static void StartSSSP(long root) {
          for (int i = 0; i < Global.ServerCount; i++) {
              using (var msg = new StartSSSPMessageWriter(root)) {
                Global.CloudStorage.StartSSSPToSSSPServer(i, msg);
              }
          }
        }
    }
}
