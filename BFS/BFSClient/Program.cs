using System;
using Trinity;
using System.Collections.Generic;

namespace BFSClient 
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
              StartBFS(id);
            } else if (command.StartsWith("distance")) {
              var cell = Global.CloudStorage.LoadBFSCell(id);
              //Console.WriteLine("id: {0}", cell.cellId);

              Console.WriteLine("level: {0}", cell.level);
              

              Console.Write("Path to root: ");
              List<long> neighbors =  GetPathToRoot(id);
              for (int i = 0; i < neighbors.Count; i++) {
                  Console.Write("{0} ", neighbors[i]);
              }
              Console.WriteLine();
            }
          }
        }

        private static void StartBFS(long root) {
          for (int i = 0; i < Global.ServerCount; i++) {
              using (var msg = new StartBFSMessageWriter(root)) {
                Global.CloudStorage.StartBFSToBFSServer(i, msg);
              }
          }
        }

        private static List<long> GetPathToRoot(long cellId) {
          List<long> path = new List<long>();
          path.Add(cellId);
          var cell = Global.CloudStorage.LoadBFSCell(cellId);
          if (cell.parent == cellId) {
            return path;
          }
          //return path;
          path.AddRange(GetPathToRoot(cell.parent));
          return path;
        }
    }
}
