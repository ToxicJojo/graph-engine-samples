using System;
using System.Linq;
using System.IO;
using Trinity;
using Trinity.Network;
using System.Collections.Generic;
using Trinity.Core.Lib;

namespace MultiGraphServer 
{

    class Program
    {
        static void Main(string[] args)
        {
          // Trinity doesn't load the config file correctly if we don't tell it to
          //TrinityConfig.LoadConfig("trinity" + args[0] + ".xml");
          TrinityConfig.LoadConfig();
          TrinityConfig.StorageRoot =  "/home/thiel/ge-storage/server" + args[0];
          TrinityConfig.LogToFile =  false;



          MultiGraphServerImpl server = new MultiGraphServerImpl();
          server.Start();


          if (Global.MyPartitionId == 0) {

            MultiGraphConfig graphConfig = MultiGraphConfig.Load("general_multilayer_config.txt");
            graphConfig.LogInfo();


            // There are a bunch of nodes that only have edges pointing to them
            // for those we need to remember them and add them later
            HashSet<string> nodeNames = new HashSet<string>();

            using (var reader = new StreamReader(graphConfig.EdgesFile)) {
              string line;
              long currentNode = -1;
              int currentLayer = -1;
              List<Edge> edges = new List<Edge>();

              Dictionary<int, List<NodeData>> remoteNodes = new Dictionary<int, List<NodeData>>();
              for (int i = 0; i < TrinityConfig.Servers.Count; i++) {
                remoteNodes[i] = new List<NodeData>();
              }
              

              while((line = reader.ReadLine()) != null) {
                Edge newEdgde = LoadMultiplexEdge(line);

                if (currentNode == newEdgde.StartId || currentNode == -1) {
                  edges.Add(newEdgde);
                } else {
                  string nodeName = "n" + currentNode + "l" + currentLayer;
                  long newNodeId = HashHelper.HashString2Int64(nodeName);
                  Node newNode = new Node(newNodeId, nodeName, currentLayer, edges);

                  if (Global.CloudStorage.IsLocalCell(newNodeId)) {
                    Global.CloudStorage.SaveNode(newNode);
                  } else {
                    int serverId = Global.CloudStorage.GetPartitionIdByCellId(newNodeId);
                    NodeData nodeData = new NodeData(newNode.CellId, newNode.Name, newNode.Layer, newNode.Edges);
                    remoteNodes[serverId].Add(nodeData);
                  }

                  edges.Clear();
                  edges.Add(newEdgde);
                }
                currentNode = newEdgde.StartId;
                currentLayer = newEdgde.StartLayer;
                nodeNames.Add("n" + newEdgde.DestinationId + "l" + newEdgde.DestinationLayer);
              }

              foreach (KeyValuePair<int, List<NodeData>> remoteNode in remoteNodes) {
                if (remoteNode.Key != Global.MyPartitionId) {
                  AddNodesRequestWriter request = new AddNodesRequestWriter(remoteNode.Value);
                  Global.CloudStorage.AddNodesToMultiGraphServer(remoteNode.Key, request);
                }
              }
            }


            


            foreach (string nodeName in nodeNames) {
              if (!IsNode(nodeName)) {
                long id = HashHelper.HashString2Int64(nodeName);
                int layer = int.Parse(nodeName.Split('l')[1]);
                Global.CloudStorage.SaveNode(id, nodeName, layer, new List<Edge>());
                Console.WriteLine("Adding Node {0}", nodeName);
              }
            }

            Console.WriteLine(IsNode("n1l1"));

            


            //LogNodeInfo("n2l1");
            int[] nodeCount = CountNodesPerLayer();
            for (int i = 0; i < nodeCount.Count(); i++) {
              Console.WriteLine("Node Count Layer{0} : {1}", i + 1, nodeCount[i]);
            }

            Console.WriteLine("---");

            int[] edgeCount = CountEdgesPerLayer();
            for (int i = 0; i < edgeCount.Count(); i++) {
              Console.WriteLine("Edge Count Layer{0} : {1}", i + 1, edgeCount[i]);
            }


          }
          Global.CloudStorage.BarrierSync(1);
          Console.WriteLine("After Barrier");
          Logger.LogNodeInfo();
          
          Console.ReadLine();
          server.Stop();

        }


        private static bool IsNode (string name) {
          long cellId = HashHelper.HashString2Int64(name);
          return Global.CloudStorage.Contains(cellId);
        }

        private static int[] CountNodesPerLayer() {
          int[] nodeCount = new int[32];

          foreach(var node in Global.LocalStorage.Node_Selector()) {
            nodeCount[node.Layer - 1]++;
          }

          return nodeCount;
        }

        private static int[] CountEdgesPerLayer() {
          int[] edgeCount = new int[32];

          foreach(var node in Global.LocalStorage.Node_Selector()) {
            foreach(Edge edge in node.Edges) {
              edgeCount[edge.StartLayer - 1]++;
            }
          }

          return edgeCount;
        }


        private static Edge LoadMultiplexEdge(string line) {
          string[] fields = line.Split();

          long startId = long.Parse(fields[0]);
          int startLayer = int.Parse(fields[1]);
          long endId = long.Parse(fields[2]);
          int endLayer = int.Parse(fields[3]);
          float weight = float.Parse(fields[4]);
          
          return new Edge(startId, startLayer, endId, endLayer, weight);
        }

        private static void LogNodeInfo(string nodeName) {
          long cellId = HashHelper.HashString2Int64(nodeName);
          LogNodeInfo(cellId);
        }

        private static void LogNodeInfo(long cellId) {
          Node node = Global.CloudStorage.LoadNode(cellId);
          Console.WriteLine("---NodeInfo---");
          Console.WriteLine("NodeId: {0}", cellId);
          Console.WriteLine("Name: {0}", node.Name);
          Console.WriteLine("Layer: {0}", node.Layer);
          Console.WriteLine("EdgeCount: {0}", node.Edges.Count);
        }
    }
}
