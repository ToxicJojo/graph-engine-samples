using System;
using System.Linq;
using System.IO;
using Trinity;
using Trinity.Network;
using System.Collections.Generic;

namespace HITSServer 
{
    class Program
    {
        private static bool loadData = false;
        static void Main(string[] args)
        {
          // Trinity doesn't load the config file correctly if we don't tell it to
          TrinityConfig.LoadConfig();
          TrinityConfig.CurrentRunningMode = RunningMode.Server;

          HITSServerImpl server = new HITSServerImpl();
          server.Start();

          if (loadData)  {
            Console.WriteLine("Starting to load data");

            using (var reader = new StreamReader("Model_Journals_Fields.txt")) {
              string line;
              string[] fields;
              long currentNode = -1;
              //List<long> references = new List<long>();
              HashSet<long> references = new HashSet<long>();

              while((line = reader.ReadLine()) != null) {
                try {
                  fields = line.Split();
                  long from = long.Parse(fields[0]);
                  long to = long.Parse(fields[2]);
                  

                  if (from == currentNode || currentNode == -1) {
                    references.Add(to);
                    currentNode = from;
                  } else {
                    if (currentNode == 2155) {
                      Console.WriteLine("Saving Journal 2155");
                      foreach (long reference in references) {
                        Console.Write("{0} ", reference);
                      }
                    }
                    Global.CloudStorage.SaveJournal(currentNode, References: references.ToList(), HubScore: 0, AuthorityScore: 0);
                    references.Clear();
                    references.Add(to);
                    currentNode = from;
                  }
                } catch (Exception e) {
                  Console.WriteLine(e.ToString());
                }
              }
            //Global.CloudStorage.SaveJournal(currentNode, References: references, HubScore: 0, AuthorityScore: 0);
              Global.CloudStorage.SaveStorage();
  
              Console.WriteLine("Finished loading data");
              }
            } else {
              Global.CloudStorage.LoadStorage();
            }
            ShowJournal(2414);
            Console.ReadLine();
        }

        private static void ShowJournal(long id) {
          var journal = Global.CloudStorage.LoadJournal(id);
          Console.WriteLine("Journal: {0}", id);
          Console.WriteLine("HubScore: {0}", journal.HubScore);
          Console.WriteLine("AuthorityScore: {0}", journal.AuthorityScore);
        }
    }
}
