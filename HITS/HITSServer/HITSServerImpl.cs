using System;
using System.Collections.Generic;
using Trinity;
using System.Linq;
using Trinity.Storage;
using Trinity.Core.Lib;
using Trinity.TSL.Lib;
using Trinity.Network.Messaging;
using System.Threading;

namespace HITSServer {
  class HITSServerImpl: HITSServerBase {

    private double authDelta = 0;
    private double hubDelta = 0;

    public override void GetCellCountHandler(CellCountResponseWriter response) {
      response.Count = Global.LocalStorage.CellCount;
    }
    
    #region Sync

    public override void SetInitialScoresHandler(SetInitialScoresMessageReader request) {
      foreach(var journal in Global.LocalStorage.Journal_Accessor_Selector()) {
        journal.HubScore = request.initialHubScore;
        journal.AuthorityScore = request.initialAuthorityScore;
      }
    }

    #region Auth

    public override void PrepareAuthorityUpdateHandler() {
      foreach (var journal in Global.LocalStorage.Journal_Accessor_Selector()) {
        journal.AuthorityScore = 0;
      }
    }

    public override void StartAuthorityUpdateHandler() {
      foreach (var journal in Global.LocalStorage.Journal_Accessor_Selector()) {
        foreach (var journalId in journal.References) {
          // Don't allow self references
          // I have to talk to Mikel if self references make sense in this context
          // If they do I will have to keep the check and update the journal as the using
          // statement will stall as the resuqested journal is already locked.
          if (journalId == journal.CellId) continue;

          //Console.WriteLine("Send updates to {0}", journalId);
          // I need to test which of two methods is faster.
          // Eighter I update the score for local cells withouth passing a message
          // Or I also send messages for local cells.
          if (Global.CloudStorage.IsLocalCell(journalId)) {
            try {
              using (var referencedJournal = Global.LocalStorage.UseJournal(journalId)) {
                referencedJournal.AuthorityScore += journal.HubScore;
              }
            } catch (Exception e) {
              //Console.WriteLine(e.ToString());
            }
          } else {
            // Send Update Message to Peer
            AuthorityUpdateRequestWriter msg = new AuthorityUpdateRequestWriter(journal.HubScore, journalId);
            int serverId = Global.CloudStorage.GetPartitionIdByCellId(journalId);
            Global.CloudStorage.AuthorityUpdateToHITSServer(serverId, msg);
          }
        }
      }
    }

    public override void AuthorityUpdateHandler(AuthorityUpdateRequestReader request) {
      try {
       using (var journal = Global.LocalStorage.UseJournal(request.Target)) {
         journal.AuthorityScore += request.AuthorityScore;
        }
      } catch (Exception e) {

      }
    }


    // We could maybe keep track of this during the authority update
    public override void AuthoritySumHandler(AuthoritySumResponseWriter response) {
      double authoritySum = 0;
      foreach (var journal in Global.LocalStorage.Journal_Accessor_Selector()) {
        // We need to sum for normalization.
        // The normalization aims to make sure the sum of squares adds up to 1.
        // For this we need to collect the sum of squares instead of the individual values.
        authoritySum += journal.AuthorityScore * journal.AuthorityScore; 
      }

      response.Sum = authoritySum;
    }

    public override void AuthorityNormalizationHandler(AuthorityNormalizationRequestReader request) {
      double normFactor = 1 / Math.Sqrt(request.Sum);
      //double normFactor = 1 / request.Sum;
      foreach (var journal in Global.LocalStorage.Journal_Accessor_Selector()) {
        journal.AuthorityScore *= normFactor;
      }
    }

    #endregion


    #region Hub
    public override void PrepareHubUpdateHandler() {
      foreach (var journal in Global.LocalStorage.Journal_Accessor_Selector()) {
        journal.HubScore = 0;
      } 
    }

    public override void StartHubUpdateHandler() {
      foreach (var journal in Global.LocalStorage.Journal_Accessor_Selector()) {
        foreach (var journalId in journal.References) {
          // Don't allow self references
          // I have to talk to Mikel if self references make sense in this context
          // If they do I will have to keep the check and update the journal as the using
          // statement will stall as the resuqested journal is already locked.
          if (journalId == journal.CellId) continue;
          //Console.WriteLine("Send updates to {0}", journalId);
          // I need to test which of two methods is faster.
          // Eighter I update the score for local cells withouth passing a message
          // Or I also send messages for local cells.
          if (Global.CloudStorage.IsLocalCell(journalId)) {
            try {
              using (var referencedJournal = Global.LocalStorage.UseJournal(journalId)) {
                journal.HubScore += referencedJournal.AuthorityScore;
              }
            } catch (Exception e) {
              //Console.WriteLine(e.ToString());
            }
          } else {
            // Send Update Message to Peer
            // TODO UPDATE THIS TO HUB INSTEAD OF AUTHORITY
            AuthorityUpdateRequestWriter msg = new AuthorityUpdateRequestWriter(journal.HubScore, journalId);
            int serverId = Global.CloudStorage.GetPartitionIdByCellId(journalId);
            Global.CloudStorage.AuthorityUpdateToHITSServer(serverId, msg);
          }
        }
      }
    }

    public override void HubSumHandler(HubSumResponseWriter response) {
      double hubSum = 0;
      foreach (var journal in Global.LocalStorage.Journal_Accessor_Selector()) {
        // We need to sum for normalization.
        // The normalization aims to make sure the sum of squares adds up to 1.
        // For this we need to collect the sum of squares instead of the individual values.
        hubSum += journal.HubScore * journal.HubScore; 
      }

      response.Sum = hubSum; 
    }

    public override void HubNormalizationHandler(HubNormalizationRequestReader request) {
      double normFactor = 1 / Math.Sqrt(request.Sum);
      foreach (var journal in Global.LocalStorage.Journal_Accessor_Selector()) {
        journal.HubScore *= normFactor;
      }
    }



    #endregion

    #endregion


    #region Async
    
    private void PhaseFinished(string phase) {
      PhaseFinished(phase, 0);
    }

    private void PhaseFinished(string phase, double result) {
      using (var msg = new PhaseFinishedRequestWriter(phase, result)) {
        Coordinator.MessagePassingExtension.PhaseFinished(Global.CloudStorage.ProxyList[0], msg);
      }
    }
    
    #region Auth


    public override void SetInitialScoresAsynHandler(SetInitialScoresMessageReader request) {
      foreach(var journal in Global.LocalStorage.Journal_Accessor_Selector()) {
        journal.HubScore = request.initialHubScore;
        journal.OldHubScore = request.initialHubScore;
        journal.AuthorityScore = request.initialAuthorityScore;
        journal.OldAuthorityScore = request.initialAuthorityScore;
      } 
      
      PhaseFinished("initialScores");
    }


    public override void PrepareAuthorityUpdateAsynHandler() {
      this.authDelta = 0;
      foreach (var journal in Global.LocalStorage.Journal_Accessor_Selector()) {
        journal.OldAuthorityScore = journal.AuthorityScore;
        journal.AuthorityScore = 0;
      }

      PhaseFinished("prepareAuthorityUpdate");
    }


    private Dictionary<long, double> pendingAuthorityUpdates = new Dictionary<long, double>();
    private long authorityUpdatesSend = 0;
    private long authorityUpdatesConfirmed = 0;
    public override void StartAuthorityUpdateAsynHandler() {
      authorityUpdatesSend = 0;
      authorityUpdatesConfirmed = 0;
      pendingHubUpdates.Clear();
      foreach (var journal in Global.LocalStorage.Journal_Accessor_Selector()) {
        foreach (var journalId in journal.References) {
          // Don't allow self references
          // I have to talk to Mikel if self references make sense in this context
          // If they do I will have to keep the check and update the journal as the using
          // statement will stall as the resuqested journal is already locked.
          if (journalId == journal.CellId) continue;

          //Console.WriteLine("Send updates to {0}", journalId);
          // I need to test which of two methods is faster.
          // Eighter I update the score for local cells withouth passing a message
          // Or I also send messages for local cells.
          if (Global.CloudStorage.IsLocalCell(journalId)) {
            try {
              using (var referencedJournal = Global.LocalStorage.UseJournal(journalId)) {
                referencedJournal.AuthorityScore += journal.HubScore;
              }
            } catch (Exception e) {

            }
          } else {
            if (pendingAuthorityUpdates.ContainsKey(journalId)) {
              pendingAuthorityUpdates[journalId] += journal.HubScore;
            } else {
              pendingAuthorityUpdates[journalId] = journal.HubScore;
            }
          }
        }
      }
      
      foreach (KeyValuePair<long, double> pendingUpdate in pendingAuthorityUpdates) {
        using (var msg = new AuthorityUpdateRequestAsynWriter(pendingUpdate.Value, pendingUpdate.Key, Global.MyPartitionId)) {
          this.authorityUpdatesSend++;
          Global.CloudStorage.AuthorityUpdateAsynToHITSServer(Global.CloudStorage.GetPartitionIdByCellId(pendingUpdate.Key), msg);
        }
      }

      SpinWait wait = new SpinWait();
      while(authorityUpdatesSend != authorityUpdatesConfirmed) {
        Console.WriteLine("Waiting for {0} updates to finish", authorityUpdatesSend - authorityUpdatesConfirmed);
        wait.SpinOnce();
      }

      PhaseFinished("authorityUpdate");
    }

    public override void AuthorityUpdateAsynHandler(AuthorityUpdateRequestAsynReader request) {
      //Console.WriteLine("Update Handler for {0}", request.Target);
      try {
        using (var journal = Global.LocalStorage.UseJournal(request.Target)) {
         journal.AuthorityScore += request.AuthorityScore;
        } 
      } catch (Exception e) {
      } finally {
        Global.CloudStorage.AuthorityUpdateAnswerToHITSServer(request.From);
      }
    }

    public override void AuthorityUpdateAnswerHandler() {
      Interlocked.Increment(ref this.authorityUpdatesConfirmed);
    }

    public override void AuthoritySumAsynHandler() {
      double authoritySum = 0;
      foreach (var journal in Global.LocalStorage.Journal_Accessor_Selector()) {
        // We need to sum for normalization.
        // The normalization aims to make sure the sum of squares adds up to 1.
        // For this we need to collect the sum of squares instead of the individual values.
        authoritySum += journal.AuthorityScore * journal.AuthorityScore; 
      }

      PhaseFinished("authoritySum", authoritySum);
    }

    public override void AuthorityNormalizationAsynHandler(AuthorityNormalizationRequestReader request) {
      double normFactor = 1 / Math.Sqrt(request.Sum);
      //double normFactor = 1 / request.Sum;
      foreach (var journal in Global.LocalStorage.Journal_Accessor_Selector()) {
        journal.AuthorityScore *= normFactor;
        this.authDelta += Math.Abs(journal.AuthorityScore - journal.OldAuthorityScore);
      }

      PhaseFinished("authorityNormalization", this.authDelta);
    }

    #endregion


    #region Hub

    public override void PrepareHubUpdateAsynHandler() {
      this.hubDelta = 0;
      foreach (var journal in Global.LocalStorage.Journal_Accessor_Selector()) {
        journal.OldHubScore = journal.HubScore;
        journal.HubScore = 0;
      } 

      PhaseFinished("prepareHubUpdate");
    }




    Dictionary<long, double> pendingHubUpdates = new Dictionary<long, double>();
    int hubUpdatesSend;
    int hubUpdatesConfirmed;
    public override void StartHubUpdateAsynHandler() {
      this.hubUpdatesSend = 0;
      this.hubUpdatesConfirmed = 0;

      pendingHubUpdates.Clear();

      foreach (var journal in Global.LocalStorage.Journal_Accessor_Selector()) {
        foreach (var journalId in journal.References) {
          // Don't allow self references
          // I have to talk to Mikel if self references make sense in this context
          // If they do I will have to keep the check and update the journal as the using
          // statement will stall as the resuqested journal is already locked.
          if (journalId == journal.CellId) continue;
          //Console.WriteLine("Send updates to {0}", journalId);
          // I need to test which of two methods is faster.
          // Eighter I update the score for local cells withouth passing a message
          // Or I also send messages for local cells.
          if (Global.CloudStorage.IsLocalCell(journalId)) {
            try {
              using (var referencedJournal = Global.LocalStorage.UseJournal(journalId)) {
                journal.HubScore += referencedJournal.AuthorityScore;
              }
            } catch (Exception e) {
              //Console.WriteLine(e.ToString());
            }
          } else {
            if (pendingHubUpdates.ContainsKey(journalId)) {
              pendingHubUpdates[journalId] += journal.AuthorityScore;
            } else {
              pendingHubUpdates[journalId] = journal.AuthorityScore;
            }
          }
        }
      }

      foreach (KeyValuePair<long, double> pendingUpdate in pendingHubUpdates) {
        using (var msg = new HubUpdateRequestAsynWriter(pendingUpdate.Value, pendingUpdate.Key, Global.MyPartitionId)) {
          this.hubUpdatesSend++;
          Global.CloudStorage.HubUpdateAsynToHITSServer(Global.CloudStorage.GetPartitionIdByCellId(pendingUpdate.Key), msg);
        } 
      }

      
      SpinWait wait = new SpinWait();
      while(hubUpdatesSend != hubUpdatesConfirmed) {
        Console.WriteLine("Waiting for {0} updates to finish", hubUpdatesSend - hubUpdatesConfirmed);
        wait.SpinOnce();
      }
        

      PhaseFinished("hubUpdate");
    }


    public override void HubUpdateAsynHandler(HubUpdateRequestAsynReader request) {
      try {
        using (var journal = Global.LocalStorage.UseJournal(request.Target)) {
         journal.HubScore += request.HubScore;
        } 
      } catch (Exception e) {
      } 
      Global.CloudStorage.HubUpdateAnswerToHITSServer(request.From);
    }

    public override void HubUpdateAnswerHandler() {
      Interlocked.Increment(ref this.hubUpdatesConfirmed);
    }


    public override void HubSumAsynHandler() {
      double hubSum = 0;
      foreach (var journal in Global.LocalStorage.Journal_Accessor_Selector()) {
        // We need to sum for normalization.
        // The normalization aims to make sure the sum of squares adds up to 1.
        // For this we need to collect the sum of squares instead of the individual values.
        hubSum += journal.HubScore * journal.HubScore; 
      } 

      PhaseFinished("hubSum", hubSum);
    }

    public override void HubNormalizationAsynHandler(HubNormalizationRequestReader request) {
      double normFactor = 1 / Math.Sqrt(request.Sum);
      foreach (var journal in Global.LocalStorage.Journal_Accessor_Selector()) {
        journal.HubScore *= normFactor;
        this.hubDelta += Math.Abs(journal.OldHubScore - journal.HubScore);
      }

      PhaseFinished("hubNormalization", this.hubDelta);
    }  

    #endregion


  #endregion



    #region Stats
    public override void TopAuthoritiesHandler(TopAuthoritiesResponseWriter response) {
      response.Authorities = Global.LocalStorage.Journal_Selector().OrderByDescending(item => item.AuthorityScore).Take(5).Select(item => item.CellId).ToList();
    }

    public override void TopHubsHandler(TopHubsResponseWriter response) {
      response.Hubs = Global.LocalStorage.Journal_Selector().OrderByDescending(item => item.HubScore).Take(5).Select(item => item.CellId).ToList();
    }
    #endregion


  }
}
