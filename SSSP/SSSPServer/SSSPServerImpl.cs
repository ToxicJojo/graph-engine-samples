using System;
using System.Collections.Generic;
using Trinity;
using Trinity.Core.Lib;
using Trinity.TSL.Lib;
using Trinity.Network.Messaging;


namespace SSSPServer {
  class SSSPServerImpl: SSSPServerBase {
    public override void DistanceUpdatingHandler(DistanceUpdatingMessageReader request) {
      List<DistanceUpdatingMessage> DistanceUpdatingMessageList = new List<DistanceUpdatingMessage>();

      request.recipients.ForEach((cellId) => {
        using (var cell = Global.LocalStorage.UseSSSPCell(cellId)) {
          if (cell.distance > request.distance + 1) {
            cell.distance = request.distance + 1;
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
              DistanceUpdatingMessageWriter msg = new DistanceUpdatingMessageWriter(cell.CellId, cell.distance, sorter.GetCellRecipientList(i));
              Global.CloudStorage.DistanceUpdatingToSSSPServer(i, msg);
            }
          }
        }
      });
    }

    public override void StartSSSPHandler(StartSSSPMessageReader request) {
      if (Global.CloudStorage.IsLocalCell(request.root)) {
        using (var rootCell = Global.LocalStorage.UseSSSPCell(request.root)) {
          rootCell.distance = 0;
          rootCell.parent = -1;

          MessageSorter sorter = new MessageSorter(rootCell.neighbors);
          for (int i = 0; i < Global.ServerCount; i++) {
            DistanceUpdatingMessageWriter msg = new DistanceUpdatingMessageWriter(rootCell.CellId, 0, sorter.GetCellRecipientList(i));
            Global.CloudStorage.DistanceUpdatingToSSSPServer(i, msg);
          }
        }
      }
    }
  }
}
