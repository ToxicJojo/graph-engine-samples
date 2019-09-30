using System;
using System.Collections.Generic;
using Trinity;
using System.Linq;
using Trinity.Storage;
using Trinity.Core.Lib;
using Trinity.TSL.Lib;
using Trinity.Network.Messaging;
using System.Threading;
using System.Diagnostics;

namespace HITSClient {
  class CoordinatorImpl: CoordinatorBase  {
    // Names for the different phases of HITS. These will be compared to the Phase attribute of a PhaseFinishedRequest to
    // indicate which phase has been finished.
    private string INITIAL_SCORES = "initialScores";
    private string PREPARE_AUTHORITY_UPDATE = "prepareAuthorityUpdate";
    private string AUTHORITY_UPDATE = "authorityUpdate";
    private string AUTHORITY_SUM = "authoritySum";
    private string AUTHORITY_NORMALIZATION = "authorityNormalization";
    private string PREPARE_HUB_UPDATE = "prepareHubUpdate";
    private string HUB_UPDATE = "hubUpdate";
    private string HUB_SUM = "hubSum";
    private string HUB_NORMALIZATION = "hubNormalization";

    // Counts how many servers have finished a phase.
    private Dictionary<string, int> phaseFinishedCount = new Dictionary<string, int>();
    // We need to lock the phaseFinishedCount Dictonary as multiple PhaseFinished message can be recived at the same time.
    private readonly object phaseFinishedCountLock = new object();

    private ulong cellCount = 0;

    // The epislon used by the HITS algorithm. The algorithm runs until the delta of authorities and hubs is
    // below epsilon.
    private double espilon = 0.1;

    // Counts the sum of all authority values. Used to normalize them.
    private double authoritySum = 0;
    // Indicates the absolute change in authority values compared to the last round.
    private double authorityDelta = 1;
    // Counts the sum of all hub values. Used to normalize them.
    private double hubSum = 0;
    // Indicates the absolute change in hub values compared to the last round.
    private double hubDelta = 1;

    public override void RunHITSHandler() {
      // Initialize the dictionary to count how many servers have finished a phase.
      phaseFinishedCount.Add(INITIAL_SCORES, 0);
      phaseFinishedCount.Add(PREPARE_AUTHORITY_UPDATE, 0);
      phaseFinishedCount.Add(AUTHORITY_UPDATE, 0);
      phaseFinishedCount.Add(AUTHORITY_NORMALIZATION, 0);
      phaseFinishedCount.Add(PREPARE_HUB_UPDATE, 0);
      phaseFinishedCount.Add(HUB_UPDATE, 0);
      phaseFinishedCount.Add(HUB_SUM, 0);
      phaseFinishedCount.Add(HUB_NORMALIZATION, 0);
      // This is just debug info and not needed.
      this.cellCount = GetCellCount();
      Console.WriteLine("Cell Count: {0}", this.cellCount);

      // Initial Scores to 1 for both authority and hub values.
      SetInitialScores(1, 1);

      Stopwatch stopwatch = new Stopwatch();
      int i = 0;
      while (this.hubDelta > this.espilon || this.authorityDelta > this.espilon) {
        Console.WriteLine("Starting Update Round: {0}", i);
        stopwatch.Restart();
        AuthUpdate();
        HubUpdate();
        stopwatch.Stop();
        TimeSpan ts = stopwatch.Elapsed;

        // Format and display the TimeSpan value.
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);

        Console.WriteLine("Auth Delta: {0} | Hub Delta: {1}", this.authorityDelta, this.hubDelta);
        Console.WriteLine("Finished Update Round: {0} in {1}", i, elapsedTime);
        i++;
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

    }

    private void AuthUpdate () {
      PrepareAuthorityUpdate();
      StartAuthorityUpdate();
      GetAuthoritySum();
      AuthorityNormalization(this.authoritySum);
    }

    private void HubUpdate () {
      PrepareHubUpdate();
      StartHubUpdate();
      GetHubSum();
      HubNormalization(this.hubSum);
    }


    private void ShowJournal(long id) {
      var journal = Global.CloudStorage.LoadJournal(id);
      Console.WriteLine("Journal: {0}", id);
      Console.WriteLine("HubScore: {0}", journal.HubScore);
      Console.WriteLine("AuthorityScore: {0}", journal.AuthorityScore);
    }

    /// <summary>
    /// Blocks until a phase is finished.
    /// </summary>
    /// <param name="phaseName">The name of the phase to wait for.</param>    
    private void WaitForPhase(string phaseName) {
      SpinWait wait = new SpinWait();
      // Spin until the phaseFinishedCount is equal to the server count.
      while (phaseFinishedCount[phaseName] != Global.ServerCount) {
        wait.SpinOnce();
      }
      // Reset the count back to 0/
      phaseFinishedCount[phaseName] = 0;
    }


    /// <summary>
    /// Handles PhaseFinishedRequests send by servers.These indicate that the server has
    /// finished a phase. Which phase is determined by the request.Phase value 
    /// The request also has a Result attribute that stores the phases result if the phase returns one.
    /// The value is 0 otherwise.
    /// </summary>
    /// <param name="request">The Request send by the server.</param>
    public override void PhaseFinishedHandler(PhaseFinishedRequestReader request) {
      // Lock the count to prevent a lost update.
      lock (phaseFinishedCountLock) {
        phaseFinishedCount[request.Phase]++;

        if (request.Phase == AUTHORITY_SUM) {
          this.authoritySum += request.Result;
        }

        if (request.Phase == AUTHORITY_NORMALIZATION) {
          this.authorityDelta += request.Result;
        }

        if (request.Phase == HUB_SUM) {
          this.hubSum += request.Result;
        }

        if (request.Phase == HUB_NORMALIZATION) {
          this.hubDelta += request.Result;
        }
      }
    }


    private ulong GetCellCount() {
      ulong cellCount = 0;
      for(int i = 0; i < Global.ServerCount; i++) {
        cellCount += Global.CloudStorage.GetCellCountToHITSServer(i).Count;
      }

      return cellCount;
    }

    private void SetInitialScores(double hubScore, double authScore) {
      Console.WriteLine("Setting Hub to: {0}", hubScore);
      Console.WriteLine("Setting Auth to: {0}", authScore);

      for(int i = 0; i < Global.ServerCount; i++) {
        SetInitialScoresMessageWriter msg = new SetInitialScoresMessageWriter(hubScore, authScore);
        Global.CloudStorage.SetInitialScoresAsynToHITSServer(i, msg);
      }
      WaitForPhase(INITIAL_SCORES);
    }

    private void PrepareAuthorityUpdate() {
      this.authorityDelta = 0;
      for(int i = 0; i < Global.ServerCount; i++) {
        Global.CloudStorage.PrepareAuthorityUpdateAsynToHITSServer(i);
      }
      WaitForPhase(PREPARE_AUTHORITY_UPDATE);
    }
        

    private void StartAuthorityUpdate() {
      for(int i = 0; i < Global.ServerCount; i++) {
        Global.CloudStorage.StartAuthorityUpdateAsynToHITSServer(i);
      }
      WaitForPhase(AUTHORITY_UPDATE);
    }
        
    private void GetAuthoritySum() {
      phaseFinishedCount[AUTHORITY_SUM] = 0;
      for(int i = 0; i < Global.ServerCount; i++) {
        Global.CloudStorage.AuthoritySumAsynToHITSServer(i);
      }
      WaitForPhase(AUTHORITY_SUM);
    }  

    private void AuthorityNormalization (double authoritySum) {
      for(int i = 0; i < Global.ServerCount; i++) {
        AuthorityNormalizationRequestWriter msg = new AuthorityNormalizationRequestWriter(authoritySum);
        Global.CloudStorage.AuthorityNormalizationAsynToHITSServer(i, msg);
      } 
      WaitForPhase(AUTHORITY_NORMALIZATION);
    }

    private void PrepareHubUpdate() {
      this.hubDelta = 0;
      for(int i = 0; i < Global.ServerCount; i++) {
        Global.CloudStorage.PrepareHubUpdateAsynToHITSServer(i);
      }
      WaitForPhase(PREPARE_HUB_UPDATE);
    }

    private void StartHubUpdate() {
      for(int i = 0; i < Global.ServerCount; i++) {
        Global.CloudStorage.StartHubUpdateAsynToHITSServer(i);
      }
      WaitForPhase(HUB_UPDATE);
    }
        
    private void GetHubSum() {
      this.hubSum = 0;
      for(int i = 0; i < Global.ServerCount; i++) {
        Global.CloudStorage.HubSumAsynToHITSServer(i);
      }
      WaitForPhase(HUB_SUM);
    }

    private void HubNormalization (double hubSum) {
      for(int i = 0; i < Global.ServerCount; i++) {
        HubNormalizationRequestWriter msg = new HubNormalizationRequestWriter(hubSum);
        Global.CloudStorage.HubNormalizationAsynToHITSServer(i, msg);
      } 
      WaitForPhase(HUB_NORMALIZATION);
    }


    private List<long> GetTopAuthorities() {
      List<long> topAuthorities = new List<long>();
      for(int i = 0; i < Global.ServerCount; i++) {
        topAuthorities.AddRange(Global.CloudStorage.TopAuthoritiesToHITSServer(i).Authorities.ToList());
      } 

      return topAuthorities.OrderByDescending(item => {
        Journal journal = Global.CloudStorage.LoadJournal(item);
        return journal.AuthorityScore;
      }).Take(5).ToList();
    }


    private List<long> GetTopHubs() {
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
