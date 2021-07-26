﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HeuristicLab.Core;
using HeuristicLab.Optimization;
using Newtonsoft.Json.Linq;

namespace HeuristicLab.JsonInterface.App {
  internal static class Runner {
    internal static void Run(string template, string config, string outputFile) {
      try {
        InstantiatorResult instantiatorResult = JsonTemplateInstantiator.Instantiate(template, config);
        IOptimizer optimizer = instantiatorResult.Optimizer;
        IEnumerable<IResultJsonItem> configuredResultItem = instantiatorResult.ConfiguredResultItems;

        optimizer.Runs.Clear();
        if (optimizer is EngineAlgorithm e)
          e.Engine = new ParallelEngine.ParallelEngine();

        Task task = optimizer.StartAsync();
        while (!task.IsCompleted) {
          WriteResultsToFile(outputFile, optimizer, configuredResultItem);
          Thread.Sleep(100);
        }

        WriteResultsToFile(outputFile, optimizer, configuredResultItem);
      } catch (Exception e) {
        Console.Error.WriteLine($"{e.Message} \n\n\n\n {e.StackTrace}");
        File.WriteAllText(outputFile, e.Message + "\n\n\n\n" + e.StackTrace);
        Environment.Exit(-1);
      }
    }

    private static void WriteResultsToFile(string file, IOptimizer optimizer, IEnumerable<IResultJsonItem> configuredResultItem) =>
      File.WriteAllText(file, FetchResults(optimizer, configuredResultItem));

    private static IEnumerable<IResultFormatter> ResultFormatter { get; } =
      PluginInfrastructure.ApplicationManager.Manager.GetInstances<IResultFormatter>();

    private static IResultFormatter GetResultFormatter(string fullName) =>
      ResultFormatter?.Where(x => x.GetType().FullName == fullName).Last();

    private static string FetchResults(IOptimizer optimizer, IEnumerable<IResultJsonItem> configuredResultItems) {
      JArray arr = new JArray();
      IEnumerable<string> configuredResults = configuredResultItems.Select(x => x.Name);

      foreach (var run in optimizer.Runs) {
        JObject obj = new JObject();
        arr.Add(obj);
        obj.Add("Run", JToken.FromObject(run.ToString()));

        // zip and filter the results with the ResultJsonItems
        var filteredResults = new List<Tuple<IResultJsonItem, IItem>>();
        foreach(var resultItem in configuredResultItems) {
          foreach(var result in run.Results) {
            if(resultItem.Name == result.Key) {
              filteredResults.Add(Tuple.Create(resultItem, result.Value));
            }
          }
        }

        // add results to the JObject
        foreach(var result in filteredResults) {
          var formatter = GetResultFormatter(result.Item1.ResultFormatterType);
          obj.Add(result.Item1.Name, formatter.Format(result.Item2));
        }
      }
      return SingleLineArrayJsonWriter.Serialize(arr);
    }
  }
}