using System;
using Trinity;
using Trinity.Core.Lib;
using Trinity.TSL.Lib;

namespace DHTServer {
  class DistributedHashtableServer: DHTServerBase {
    public override void SetHandler(SetMessageReader request) {
      long cellId = HashHelper.HashString2Int64(request.Key);

      using (var cell = Global.LocalStorage.UseBucketCell(cellId, CellAccessOptions.CreateNewOnCellNotFound)) {
        int count = cell.KVList.Count;
        int index = -1;

        for (int i = 0; i < count; i++) {
          if (cell.KVList[i].Key == request.Key) {
            index = i;
            break;
          }
        }

        if (index != -1) {
          cell.KVList[index].Value = request.Value;
        } else {
          cell.KVList.Add(new KVPair(request.Key, request.Value));
        }
      }
    }

    public override void GetHandler(GetMessageReader request, GetResponseWriter response) {
      long cellId = HashHelper.HashString2Int64(request.Key);
      response.IsFound = false;

      using (var cell = Global.LocalStorage.UseBucketCell(cellId, CellAccessOptions.ReturnNullOnCellNotFound)) {
        if (cell == null) {
          return;
        } else {
          int count = cell.KVList.Count;

          for (int i = 0; i < count; i++) {
            if (cell.KVList[i].Key == request.Key) {
              response.IsFound = true;
              response.Value = cell.KVList[i].Value;
              break;
            }
          }
        }
      }
    }
  }
}
