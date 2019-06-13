using System;
using System.Collections.Generic;
using Trinity;
using Trinity.Core.Lib;
using Trinity.TSL.Lib;
using Trinity.Network.Messaging;

namespace BFSServer {
  class BFSServerImpl: BFSServerBase {

    public override void StartBFSHandler(StartBFSMessageReader request) {
      if (Global.CloudStorage.IsLocalCell(request.root)) {
        using (var rootCell = Global.LocalStorage.UseBFSCell(request.root)) {
          rootCell.level = 0;
          rootCell.parent = request.root;

          MessageSorter sorter = new MessageSorter(rootCell.neighbors);
          for (int i = 0; i < Global.ServerCount; i++) {
            BFSUpdateMessageWriter msg = new BFSUpdateMessageWriter(rootCell.CellId, 0, sorter.GetCellRecipientList(i));
            Global.CloudStorage.BFSUpdateToBFSServer(i, msg);
          }
        }
      }
    }    

    public override void BFSUpdateHandler(BFSUpdateMessageReader request) {
      request.recipients.ForEach((cellId) => {
        using (var cell = Global.LocalStorage.UseBFSCell(cellId)) {
          if (cell.level > request.level + 1) {
            cell.level = request.level + 1;
            cell.parent = request.senderId;

            List<long> aliveNeighbors = new List<long>();
            for (int i = 0; i < cell.neighbors.Count; i++) {
              if (Global.CloudStorage.Contains(cell.neighbors[i])) {
                aliveNeighbors.Add(cell.neighbors[i]);
              }
            }

            //MessageSorter sorter = new MessageSorter(cell.neighbors);
            MessageSorter sorter = new MessageSorter(aliveNeighbors);

            for (int i = 0; i < Global.ServerCount; i++) {
              BFSUpdateMessageWriter msg = new BFSUpdateMessageWriter(cell.CellId, cell.level, sorter.GetCellRecipientList(i));
              Global.CloudStorage.BFSUpdateToBFSServer(i, msg);
            }
          }
        }
      });
    }
  }
}
