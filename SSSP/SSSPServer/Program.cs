using System;
using System.IO;
using System.Collections.Generic;
using Trinity;

namespace SSSPServer 
{
    class Program
    {
        static void Main(string[] args)
        {
          // Trinity doesn't load the config file correctly if we don't tell it to
          TrinityConfig.LoadConfig();
          TrinityConfig.CurrentRunningMode = RunningMode.Server;


          SSSPServerImpl server = new SSSPServerImpl();
          server.Start();

          //using (var reader = new StreamReader("p2p-Gnutella31.txt")) {
          //using (var reader = new StreamReader("com-youtube.ungraph.txt")) {
          using (var reader = new StreamReader("com-orkut.ungraph.txt")) {
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
                  Global.CloudStorage.SaveSSSPCell(currentNode, distance: int.MaxValue, parent: -1, neighbors: neighbors);
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

          Console.ReadKey();
        }
    }
}
