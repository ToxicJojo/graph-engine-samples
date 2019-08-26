using System;
using System.Collections.Generic;
using Trinity;
using System.Linq;
using Trinity.Storage;
using Trinity.Core.Lib;
using Trinity.TSL.Lib;
using Trinity.Network.Messaging;
using System.Threading;

namespace HITSClient {
  class CoordinatorImpl: CoordinatorBase  {

    private ulong cellCount = 0;

    private int initialScoresFinished = 0;


    public override void RunHITSHandler() {
      Console.WriteLine("proxy {0}", Global.MyProxyId);
      Console.WriteLine("server {0}", Global.MyPartitionId);
      this.cellCount = GetCellCount();
      Console.WriteLine("Cell Count: {0}", this.cellCount);


      SetInitialScores(1, 1);

      SpinWait wait = new SpinWait();
      while (initialScoresFinished != Global.ServerCount) {
        wait.SpinOnce();
      }

      Console.WriteLine("Finished setting inital scores");

    }



    public override void PhaseFinishedHandler(PhaseFinishedRequestReader request) {
      Console.WriteLine("Phase Finished {0}", request.Phase);
      if (request.Phase == "initialScores") {
        this.initialScoresFinished++;
      }
    }



    private static ulong GetCellCount() {
      ulong cellCount = 0;
      for(int i = 0; i < Global.ServerCount; i++) {
        cellCount += Global.CloudStorage.GetCellCountToHITSServer(i).Count;
      }

      return cellCount;
    }

    private static void SetInitialScores(double hubScore, double authScore) {
      Console.WriteLine("Setting Hub to: {0}", hubScore);
      Console.WriteLine("Setting Auth to: {0}", authScore);
      for(int i = 0; i < Global.ServerCount; i++) {
        SetInitialScoresMessageWriter msg = new SetInitialScoresMessageWriter(hubScore, authScore);
        Global.CloudStorage.SetInitialScoresAsynToHITSServer(i, msg);
      }
    }




  }
}
