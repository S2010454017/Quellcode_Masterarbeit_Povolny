﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HeuristicLab.Core;
using HeuristicLab.Data;
using HeuristicLab.Optimization;
using Newtonsoft.Json.Linq;

namespace HeuristicLab.Manufacture {

  
  public class JCInstantiator {

    private JToken Config { get; set; }

    public IAlgorithm Instantiate(string configFile) {
      Config = JToken.Parse(File.ReadAllText(configFile));

      Component algorithmData = GetData(Config["Metadata"]["Algorithm"].ToString());
      ResolveReferences(algorithmData);
      IAlgorithm algorithm = CreateObject<IAlgorithm>(algorithmData);
     
      Component problemData = GetData(Config["Metadata"]["Problem"].ToString());
      ResolveReferences(problemData);
      IProblem problem = CreateObject<IProblem>(problemData);
      algorithm.Problem = problem;

      Transformer.Inject(algorithm, algorithmData);
      Transformer.Inject(algorithm, problemData);

      return algorithm;
    }

    /*
     * resolve references
     */

    private void ResolveReferences(Component data) {
      foreach (var p in data.Parameters) {
        if (p.Default is string && p.Reference == null) {
          p.Reference = GetData(p.Default.Cast<string>());
        }
      }
    }

    private Component GetData(string key)
    {
      foreach(JObject item in Config["Objects"])
      {
        Component data = BuildDataFromJObject(item);
        if (data.Name == key) return data;
      }
      return null;
    }

    private Component BuildDataFromJObject(JObject obj) {
      Component data = new Component() {
        Name = obj["Name"]?.ToString(),
        Default = obj["Default"]?.ToObject<object>(),
        Range = obj["Range"]?.ToObject<object[]>(),
        Type = obj["Type"]?.ToObject<string>()
      };

      if(obj["StaticParameters"] != null)
        foreach (JObject sp in obj["StaticParameters"])
          data[sp["Name"].ToString()] = BuildDataFromJObject(sp);

      if (obj["FreeParameters"] != null)
        foreach (JObject sp in obj["FreeParameters"])
          data[sp["Name"].ToString()] = BuildDataFromJObject(sp);

      if (obj["Operators"] != null) {
        data.Operators = new List<Component>();
        foreach (JObject sp in obj["Operators"])
          data.Operators.Add(BuildDataFromJObject(sp));
      }

      return data;
    }

    private T CreateObject<T>(Component data) {
      Type type = Type.GetType(data.Type);
      return (T)Activator.CreateInstance(type);
    }

  }
}
