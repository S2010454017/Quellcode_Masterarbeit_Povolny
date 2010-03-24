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

using System;
using HeuristicLab.Core;
using HeuristicLab.Data;
using HeuristicLab.Encodings.PermutationEncoding;
using HeuristicLab.Operators;
using HeuristicLab.Parameters;
using HeuristicLab.Persistence.Default.CompositeSerializers.Storable;

namespace HeuristicLab.Problems.TravelingSalesman {
  /// <summary>
  /// An operator to evaluate 2-opt moves.
  /// </summary>
  [Item("TSPTwoOptPathMoveEvaluator", "Evaluates a 2-opt move by summing up the length of all added edges and subtracting the length of all deleted edges.")]
  [StorableClass]
  public abstract class TSPTwoOptPathMoveEvaluator : TSPPathMoveEvaluator, ITwoOptPermutationMoveOperator {
    public ILookupParameter<TwoOptMove> TwoOptMoveParameter {
      get { return (ILookupParameter<TwoOptMove>)Parameters["TwoOptMove"]; }
    }

    public TSPTwoOptPathMoveEvaluator()
      : base() {
      Parameters.Add(new LookupParameter<TwoOptMove>("TwoOptMove", "The move to evaluate."));
    }

    protected override double EvaluateByCoordinates(Permutation permutation, DoubleMatrix coordinates) {
      TwoOptMove m = TwoOptMoveParameter.ActualValue;
      int edge1source = permutation.GetCircular(m.Index1 - 1);
      int edge1target = permutation[m.Index1];
      int edge2source = permutation[m.Index2];
      int edge2target = permutation.GetCircular(m.Index2 + 1);
      double moveQuality = 0;
      // remove two edges
      moveQuality -= CalculateDistance(coordinates[edge1source, 0], coordinates[edge1source, 1],
            coordinates[edge1target, 0], coordinates[edge1target, 1]);
      moveQuality -= CalculateDistance(coordinates[edge2source, 0], coordinates[edge2source, 1],
        coordinates[edge2target, 0], coordinates[edge2target, 1]);
      // add two edges
      moveQuality += CalculateDistance(coordinates[edge1source, 0], coordinates[edge1source, 1],
        coordinates[edge2source, 0], coordinates[edge2source, 1]);
      moveQuality += CalculateDistance(coordinates[edge1target, 0], coordinates[edge1target, 1],
        coordinates[edge2target, 0], coordinates[edge2target, 1]);
      return moveQuality;
    }

    protected override double EvaluateByDistanceMatrix(Permutation permutation, DoubleMatrix distanceMatrix) {
      TwoOptMove m = TwoOptMoveParameter.ActualValue;
      int edge1source = permutation.GetCircular(m.Index1 - 1);
      int edge1target = permutation[m.Index1];
      int edge2source = permutation[m.Index2];
      int edge2target = permutation.GetCircular(m.Index2 + 1);
      double moveQuality = 0;
      // remove two edges
      moveQuality -= distanceMatrix[edge1source, edge1target];
      moveQuality -= distanceMatrix[edge2source, edge2target];
      // add two edges
      moveQuality += distanceMatrix[edge1source, edge2source];
      moveQuality += distanceMatrix[edge1target, edge2target];
      return moveQuality;
    }
  }
}
