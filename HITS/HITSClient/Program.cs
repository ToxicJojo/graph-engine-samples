using System;
using System.Collections.Generic;
using System.Linq;
using Trinity;

namespace HITSClient 
{
    class Program
    {
        static void Main(string[] args)
        {
          // Trinity doesn't load the config file correctly if we don't tell it to.
          TrinityConfig.LoadConfig();
          //TrinityConfig.CurrentRunningMode = RunningMode.Proxy;
          //TrinityConfig.CurrentRunningMode = RunningMode.Client;

          CoordinatorImpl coordinator = new CoordinatorImpl();
          coordinator.Start();

          Coordinator.MessagePassingExtension.RunHITS(Global.CloudStorage.ProxyList[0]);

          //coordinator.RunHITSHandler();

          /*
          ulong cellCount = GetCellCount();
          Console.WriteLine("Journal Count: {0}", cellCount);

          double initialAuthScore = 1;
          double initialHubScore = 1;

          SetInitialScores(initialHubScore, initialAuthScore);

          for (int  i = 0; i < 5; i++) {
            Console.WriteLine("Starting Update Round: {0}", i);
            AuthUpdate();
            HubUpdate();
          }

          Console.WriteLine("Top Authorities:");
          Console.WriteLine("_________________");
          List<long> topAuthorities = GetTopAuthorities();
          foreach(long authorityId in topAuthorities) {
            Journal authority = Global.CloudStorage.LoadJournal(authorityId);
            Console.WriteLine("ID: {0}, Score: {1}", authority.CellId, authority.AuthorityScore);
          }


          Console.WriteLine("Top Hubs:");
          Console.WriteLine("_________________");
          List<long> topHubs = GetTopHubs();
          foreach(long hubId in topHubs) {
            Journal hub = Global.CloudStorage.LoadJournal(hubId);
            Console.WriteLine("ID: {0}, Score: {1}", hub.CellId, hub.HubScore);
          }
        */

        }

        private static void AuthUpdate () {
          PrepareAuthorityUpdate();
          StartAuthorityUpdate(); 

          double authoritySum = GetAuthoritySum();
          AuthorityNormalization(authoritySum);
        }

        private static void HubUpdate () {
          PrepareHubUpdate();
          StartHubUpdate();

          double hubSum = GetHubSum();
          HubNormalization(hubSum);
        }

        private static void ShowJournal(long id) {
          var journal = Global.CloudStorage.LoadJournal(id);
          Console.WriteLine("Journal: {0}", id);
          Console.WriteLine("HubScore: {0}", journal.HubScore);
          Console.WriteLine("AuthorityScore: {0}", journal.AuthorityScore);
          //Console.WriteLine("References: ");
          //foreach (long reference in journal.References) {
          //  Console.Write("{0} ", reference);
          //}
        }

        private static ulong GetCellCount() {
          ulong cellCount = 0;
          for(int i = 0; i < Global.ServerCount; i++) {
            cellCount += Global.CloudStorage.GetCellCountToHITSServer(i).Count;
          }

          return cellCount;
        }

        private static void SetInitialScores(double hubScore, double authScore) {
          Console.WriteLine("Setting hub to: {0}", hubScore);
          Console.WriteLine("Setting auth to: {0}", authScore);
          for(int i = 0; i < Global.ServerCount; i++) {
            SetInitialScoresMessageWriter msg = new SetInitialScoresMessageWriter(hubScore, authScore);
            Global.CloudStorage.SetInitialScoresToHITSServer(i, msg);
          }
        }

        private static void PrepareAuthorityUpdate() {
          for(int i = 0; i < Global.ServerCount; i++) {
            Global.CloudStorage.PrepareAuthorityUpdateToHITSServer(i);
          }
        }

        private static void StartAuthorityUpdate() {
          for(int i = 0; i < Global.ServerCount; i++) {
            Global.CloudStorage.StartAuthorityUpdateToHITSServer(i);
          }
        }

        private static double GetAuthoritySum() {
          double authoritySum = 0;
          for(int i = 0; i < Global.ServerCount; i++) {
            authoritySum += Global.CloudStorage.AuthoritySumToHITSServer(i).Sum;
          }

          return authoritySum;
        }

        private static void AuthorityNormalization (double authoritySum) {
          for(int i = 0; i < Global.ServerCount; i++) {
            AuthorityNormalizationRequestWriter msg = new AuthorityNormalizationRequestWriter(authoritySum);
            Global.CloudStorage.AuthorityNormalizationToHITSServer(i, msg);
          } 
        }


        private static void PrepareHubUpdate() {
          for(int i = 0; i < Global.ServerCount; i++) {
            Global.CloudStorage.PrepareHubUpdateToHITSServer(i);
          }
        }

        private static void StartHubUpdate() {
          for(int i = 0; i < Global.ServerCount; i++) {
            Global.CloudStorage.StartHubUpdateToHITSServer(i);
          }
        }

        private static double GetHubSum() {
          double hubSum = 0;
          for(int i = 0; i < Global.ServerCount; i++) {
            hubSum += Global.CloudStorage.HubSumToHITSServer(i).Sum;
          }

          return hubSum;
        }


        private static void HubNormalization (double hubSum) {
          for(int i = 0; i < Global.ServerCount; i++) {
            HubNormalizationRequestWriter msg = new HubNormalizationRequestWriter(hubSum);
            Global.CloudStorage.HubNormalizationToHITSServer(i, msg);
          } 
        }


        private static List<long> GetTopAuthorities() {
          List<long> topAuthorities = new List<long>();
          for(int i = 0; i < Global.ServerCount; i++) {
            topAuthorities.AddRange(Global.CloudStorage.TopAuthoritiesToHITSServer(i).Authorities.ToList());
          } 

          return topAuthorities.OrderByDescending(item => {
            Journal journal = Global.CloudStorage.LoadJournal(item);
            return journal.AuthorityScore;
          }).Take(5).ToList();
        }


        private static List<long> GetTopHubs() {
          List<long> topHubs = new List<long>();
          for(int i = 0; i < Global.ServerCount; i++) {
            topHubs.AddRange(Global.CloudStorage.TopHubsToHITSServer(i).Hubs.ToList());
          } 

          return topHubs.OrderByDescending(item => {
            Journal journal = Global.CloudStorage.LoadJournal(item);
            return journal.HubScore;
          }).Take(5).ToList();
        }
    }
}
