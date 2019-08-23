using System;
using System.Collections.Generic;
using Trinity;
using System.Linq;
using Trinity.Storage;
using Trinity.Core.Lib;
using Trinity.TSL.Lib;
using Trinity.Network.Messaging;

namespace HITSServer {
  class HITSServerImpl: HITSServerBase {
    
    public override void GetCellCountHandler(CellCountResponseWriter response) {
      response.Count = Global.LocalStorage.CellCount;
    }

    public override void SetInitialScoresHandler(SetInitialScoresMessageReader request) {
      foreach(var journal in Global.LocalStorage.Journal_Accessor_Selector()) {
        journal.HubScore = request.initialHubScore;
        journal.AuthorityScore = request.initialAuthorityScore;
      }
    }
    


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
      using (var journal = Global.LocalStorage.UseJournal(request.Target)) {
        journal.AuthorityScore += request.AuthorityScore;
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


    public override void TopAuthoritiesHandler(TopAuthoritiesResponseWriter response) {
      response.Authorities = Global.LocalStorage.Journal_Selector().OrderByDescending(item => item.AuthorityScore).Take(5).Select(item => item.CellId).ToList();
    }

    public override void TopHubsHandler(TopHubsResponseWriter response) {
      response.Hubs = Global.LocalStorage.Journal_Selector().OrderByDescending(item => item.HubScore).Take(5).Select(item => item.CellId).ToList();
    }

  }
}
