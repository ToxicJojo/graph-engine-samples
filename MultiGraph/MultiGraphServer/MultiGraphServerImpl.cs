using Trinity;
using System;
using MultiGraphServer.MultiGraphServer;

namespace MultiGraphServer {
  public class MultiGraphServerImpl: MultiGraphServerBase {

    public override void AddNodesHandler(AddNodesRequestReader request) {

      foreach (NodeData node in request.Nodes) {
          Node newNode = new Node(node.NodeId, node.Name, node.Layer, node.Edges);
          Global.CloudStorage.SaveNode(newNode);
      }
    }

    public override void PingHandler() {
      Console.WriteLine("PING");
    }
  }

}
