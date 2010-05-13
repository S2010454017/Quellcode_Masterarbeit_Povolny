#region License Information
/* HeuristicLab
 * Copyright (C) 2002-2010 Heuristic and Evolutionary Algorithms Laboratory (HEAL)
 *
 * This file is part of HeuristicLab.
 *
 * HeuristicLab is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * HeuristicLab is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with HeuristicLab. If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using HeuristicLab.Core;
using HeuristicLab.Data;
using System;
using HeuristicLab.Persistence.Default.CompositeSerializers.Storable;
using HeuristicLab.Operators;
using HeuristicLab.Encodings.SymbolicExpressionTreeEncoding;
using HeuristicLab.Parameters;
using HeuristicLab.Problems.DataAnalysis.Symbolic.Symbols;

namespace HeuristicLab.Problems.DataAnalysis.Symbolic {
  [Item("VariableFrequencyAnalyser", "Calculates the accumulated frequencies of variable-symbols over the whole population.")]
  [StorableClass]
  public abstract class VariableFrequencyAnalyser : SingleSuccessorOperator {
    private const string SymbolicExpressionTreeParameterName = "SymbolicExpressionTree";
    private const string DataAnalysisProblemDataParameterName = "DataAnalysisProblemData";
    private const string VariableFrequenciesParameterName = "VariableFrequencies";

    #region parameter properties
    public ILookupParameter<DataAnalysisProblemData> DataAnalysisProblemDataParameter {
      get { return (ILookupParameter<DataAnalysisProblemData>)Parameters[DataAnalysisProblemDataParameterName]; }
    }
    public ILookupParameter<ItemArray<SymbolicExpressionTree>> SymbolicExpressionTreeParameter {
      get { return (ILookupParameter<ItemArray<SymbolicExpressionTree>>)Parameters[SymbolicExpressionTreeParameterName]; }
    }
    public ILookupParameter<DoubleMatrix> VariableFrequenciesParameter {
      get { return (ILookupParameter<DoubleMatrix>)Parameters[VariableFrequenciesParameterName]; }
    }
    #endregion
    #region properties
    public DataAnalysisProblemData DataAnalysisProblemData {
      get { return DataAnalysisProblemDataParameter.ActualValue; }
    }
    public ItemArray<SymbolicExpressionTree> SymbolicExpressionTrees {
      get { return SymbolicExpressionTreeParameter.ActualValue; }
    }
    public DoubleMatrix VariableFrequencies {
      get { return VariableFrequenciesParameter.ActualValue; }
      set { VariableFrequenciesParameter.ActualValue = value; }
    }
    #endregion
    public VariableFrequencyAnalyser()
      : base() {
      Parameters.Add(new ScopeTreeLookupParameter<SymbolicExpressionTree>(SymbolicExpressionTreeParameterName, "The symbolic expression trees that should be analyzed."));
      Parameters.Add(new LookupParameter<DataAnalysisProblemData>(DataAnalysisProblemDataParameterName, "The problem data on which the for which the symbolic expression tree is a solution."));
      Parameters.Add(new LookupParameter<DoubleMatrix>(VariableFrequenciesParameterName, "The relative variable reference frequencies aggregated over the whole population."));
    }

    public override IOperation Apply() {
      var inputVariables = DataAnalysisProblemData.InputVariables.Select(x => x.Value);
      if (VariableFrequencies == null) {
        VariableFrequencies = new DoubleMatrix(0, 1, inputVariables);
      }
      ((IStringConvertibleMatrix)VariableFrequencies).Rows = VariableFrequencies.Rows + 1;
      int lastRowIndex = VariableFrequencies.Rows - 1;
      var columnNames = VariableFrequencies.ColumnNames.ToList();
      foreach (var pair in CalculateVariableFrequencies(SymbolicExpressionTrees, inputVariables)) {
        int columnIndex = columnNames.IndexOf(pair.Key);
        VariableFrequencies[lastRowIndex, columnIndex] = pair.Value;
      }
      return null;
    }

    public static IEnumerable<KeyValuePair<string, double>> CalculateVariableFrequencies(IEnumerable<SymbolicExpressionTree> trees, IEnumerable<string> inputVariables) {
      int totalVariableReferences = 0;
      Dictionary<string, double> variableReferencesSum = new Dictionary<string, double>();
      Dictionary<string, double> variableFrequencies = new Dictionary<string, double>();
      foreach (var inputVariable in inputVariables)
        variableReferencesSum[inputVariable] = 0.0;
      foreach (var tree in trees) {
        var variableReferences = GetVariableReferenceCount(tree, inputVariables);
        foreach (var pair in variableReferences) {
          variableReferencesSum[pair.Key] += pair.Value;
        }
        totalVariableReferences += GetTotalVariableReferencesCount(tree);
      }
      foreach (string inputVariable in inputVariables) {
        double relFreq = variableReferencesSum[inputVariable] / (double)totalVariableReferences;
        variableFrequencies.Add(inputVariable, relFreq);
      }
      return variableFrequencies;
    }

    private static int GetTotalVariableReferencesCount(SymbolicExpressionTree tree) {
      return tree.IterateNodesPrefix().OfType<VariableTreeNode>().Count();
    }

    private static IEnumerable<KeyValuePair<string, int>> GetVariableReferenceCount(SymbolicExpressionTree tree, IEnumerable<string> inputVariables) {
      Dictionary<string, int> references = new Dictionary<string, int>();
      var groupedFuns = (from node in tree.IterateNodesPrefix().OfType<VariableTreeNode>()
                         select node.VariableName)
                         .GroupBy(x => x)
                         .Select(g => new { Key = g.Key, Count = g.Count() })
                         .ToArray();

      foreach (var inputVariable in inputVariables) {
        var matchingFuns = from g in groupedFuns
                           where g.Key == inputVariable
                           select g.Count;
        if (matchingFuns.Count() == 0)
          references.Add(inputVariable, 0);
        else {
          references.Add(inputVariable, matchingFuns.Single());
        }
      }
      return references;
    }
  }
}
