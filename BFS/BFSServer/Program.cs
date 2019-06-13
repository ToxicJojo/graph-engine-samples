using System;
using System.IO;
using Trinity;
using System.Collections.Generic;

namespace BFSServer 
{
    class Program
    {
        static void Main(string[] args)
        {
          // Trinity doesn't load the config file correctly if we don't tell it to
          TrinityConfig.LoadConfig();
          TrinityConfig.CurrentRunningMode = RunningMode.Server;

          BFSServerImpl server = new BFSServerImpl();
          server.Start();

          Console.WriteLine("Starting to load data");

          using (var reader = new StreamReader("p2p-Gnutella31.txt")) {
          //using (var reader = new StreamReader("com-youtube.ungraph.txt")) {
          //using (var reader = new StreamReader("com-orkut.ungraph.txt")) {
            string line;
            string[] fields;
            long currentNode = 0;
            List<long> neighbors = new List<long>();

            while ((line = reader.ReadLine()) != null) {
              try {
                fields = line.Split(null);
                long from = long.Parse(fields[0]);
                long to = long.Parse(fields[1]);



                if (from == currentNode) {
                  neighbors.Add(to);
                } else {
                  Global.CloudStorage.SaveBFSCell(currentNode, level: int.MaxValue, parent: -1, neighbors: neighbors);
                  neighbors.Clear();
                  neighbors.Add(to);
                  currentNode = from;
                }
              } catch (Exception e) {
                Console.WriteLine(e.ToString());
                //break;
              }
            }
          }



          Console.WriteLine("Finished loading data");
          Console.WriteLine("Press Enter to close");

          Console.ReadLine();
        }

        private static void GenerateData() {
          
        }
    }
}
