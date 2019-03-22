using System;
using Trinity;
using Trinity.Core.Lib;

namespace DHTClient 
{
    class Program
    {
        static void Main(string[] args)
        {
          // Trinity doesn't load the config file correctly if we don't tell it to.
          TrinityConfig.LoadConfig();
          TrinityConfig.CurrentRunningMode = RunningMode.Client;

          while (true) {
            Console.WriteLine("Input a command 'set key value' 'get key'");
            string input = Console.ReadLine();
            string[] fields = input.Split(" ");

            int partitionId = GetPartitionIdByKey(fields[0]);

            if (fields[0] == "get") {
              using (var request = new GetMessageWriter(fields[1])) {
                using (var response = Global.CloudStorage.GetToDHTServer(partitionId, request)) {
                  if (response.IsFound) {
                    Console.WriteLine("Key:  {0} ; Value: {1}", request.Key, response.Value);
                  } else {
                    Console.WriteLine("Key: {0} wasn't found", request.Key);
                  }
                }
              }
            } else if (fields[0] == "set") {
              using (var request = new SetMessageWriter(fields[1], fields[2])) {
                Global.CloudStorage.SetToDHTServer(partitionId, request);
              }  
            }
          }
        }

        internal static int GetPartitionIdByKey(string key) {
            return Global.CloudStorage.GetPartitionIdByCellId(HashHelper.HashString2Int64(key));
        }
    }
}
