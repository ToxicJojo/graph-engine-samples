using Trinity;
using Trinity.Storage;
using System;

namespace MultiGraphServer {
  public class Logger {
    

    public static void LogNodeInfo() {

      Console.WriteLine("---Server Info---");
      Console.WriteLine("Server Partition:{0}", Global.MyPartitionId);
      Console.WriteLine("Cell Count: {0}", Global.LocalStorage.CellCount);
      Console.WriteLine("Index Commited Memory: {0}", Global.LocalStorage.CommittedIndexMemory);
      Console.WriteLine("Trunk Commited Memory: {0}", Global.LocalStorage.CommittedTrunkMemory);
      Console.WriteLine("Total Commited Memory: {0}", Global.LocalStorage.TotalCommittedMemory);
    }


  }
}
