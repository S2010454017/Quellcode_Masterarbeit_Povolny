﻿#region License Information
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

using HeuristicLab.Core;
using HeuristicLab.Data;
using HeuristicLab.Operators;
using HeuristicLab.Parameters;
using HeuristicLab.Persistence.Default.CompositeSerializers.Storable;

namespace HeuristicLab.Analysis {
  /// <summary>
  /// An operator that updates the best quality found so far with those qualities in the subscopes.
  /// </summary>
  [Item("BestQualityMemorizer", "An operator that updates the best quality found so far with those qualities in the subscopes.")]
  [StorableClass]
  public class BestQualityMemorizer : SingleSuccessorOperator {
    public IValueLookupParameter<BoolData> MaximizationParameter {
      get { return (IValueLookupParameter<BoolData>)Parameters["Maximization"]; }
    }
    public ILookupParameter<ItemArray<DoubleData>> QualityParameter {
      get { return (ILookupParameter<ItemArray<DoubleData>>)Parameters["Quality"]; }
    }
    public IValueLookupParameter<DoubleData> BestQualityParameter {
      get { return (IValueLookupParameter<DoubleData>)Parameters["BestQuality"]; }
    }

    public BestQualityMemorizer()
      : base() {
      Parameters.Add(new ValueLookupParameter<BoolData>("Maximization", "True if the current problem is a maximization problem, otherwise false."));
      Parameters.Add(new SubScopesLookupParameter<DoubleData>("Quality", "The value contained in each sub-scope which represents the solution quality."));
      Parameters.Add(new ValueLookupParameter<DoubleData>("BestQuality", "The quality value of the best solution."));
    }

    public override IOperation Apply() {
      ItemArray<DoubleData> qualities = QualityParameter.ActualValue;
      bool maximization = MaximizationParameter.ActualValue.Value;
      DoubleData best = BestQualityParameter.ActualValue;
      double max = (best != null) ? (best.Value) : ((maximization) ? (double.MinValue) : (double.MaxValue));
      
      foreach (DoubleData quality in qualities)
        if (IsBetter(maximization, quality.Value, max)) max = quality.Value;

      if (best == null) BestQualityParameter.ActualValue = new DoubleData(max);
      else best.Value = max;
      return base.Apply();
    }

    private bool IsBetter(bool maximization, double quality, double max) {
      return (maximization && quality > max || !maximization && quality < max);
    }
  }
}
