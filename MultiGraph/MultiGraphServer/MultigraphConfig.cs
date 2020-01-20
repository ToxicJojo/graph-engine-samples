using System;
using System.Linq;
using System.IO;
using Trinity;
using Trinity.Network;
using System.Collections.Generic;
using Trinity.Core.Lib;

namespace MultiGraphServer 
{
    class MultiGraphConfig 
    {

      public string EdgesFile {
        get;
        set;
      }

      public string LayersFile {
        get;
        set;
      }
      public string LayoutFile {
        get;
        set;
      }


      private MultiGraphConfig(string edgesFile, string layoutFile, string layersFile) {
        this.EdgesFile = edgesFile;
        this.LayersFile = layersFile;
        this.LayoutFile = layoutFile;
      }

      public static MultiGraphConfig Load(string configFile) {
        StreamReader reader = new StreamReader(configFile);
        string line = reader.ReadLine();
        string[] files = line.Split(';');


        return new MultiGraphConfig(files[0], files[1], files[2]);
      }

      public void LogInfo() {
        Console.WriteLine("___MultigraphConfig___");
        Console.WriteLine("Edges  File: {0}", this.EdgesFile);
        Console.WriteLine("Layout File: {0}", this.LayoutFile);
        Console.WriteLine("Layers File: {0}", this.LayersFile);
      }
    }
}
