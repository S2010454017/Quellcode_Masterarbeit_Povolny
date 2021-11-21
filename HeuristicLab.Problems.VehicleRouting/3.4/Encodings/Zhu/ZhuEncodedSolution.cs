﻿#region License Information
/* HeuristicLab
 * Copyright (C) Heuristic and Evolutionary Algorithms Laboratory (HEAL)
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
using System.Collections.Generic;
using System.Linq;
using HEAL.Attic;
using HeuristicLab.Common;
using HeuristicLab.Core;
using HeuristicLab.Encodings.PermutationEncoding;
using HeuristicLab.Problems.VehicleRouting.Interfaces;

namespace HeuristicLab.Problems.VehicleRouting.Encodings.Zhu {
  [Item("ZhuEncodedSolution", "Represents a Zhu encoded solution of the VRP. It is implemented as described in Zhu, K.Q. (2000). A New Genetic Algorithm For VRPTW. Proceedings of the International Conference on Artificial Intelligence.")]
  [StorableType("1A5F5A1D-E4F5-4477-887E-45FC488BC459")]
  public class ZhuEncodedSolution : General.PermutationEncodedSolution {
    #region IVRPEncoding Members
    public override int GetTourIndex(Tour tour) {
      return 0;
    }

    public override List<Tour> GetTours() {
      List<Tour> result = new List<Tour>();

      Tour newTour = new Tour();

      for (int i = 0; i < this.Length; i++) {
        int city = this[i] + 1;
        newTour.Stops.Add(city);
        var evalNewTour = ProblemInstance.EvaluateTour(newTour, this);
        if (!evalNewTour.IsFeasible) {
          newTour.Stops.Remove(city);
          if (newTour.Stops.Count > 0)
            result.Add(newTour);

          newTour = new Tour();
          newTour.Stops.Add(city);
        }
      }

      if (newTour.Stops.Count > 0)
        result.Add(newTour);

      //if there are too many vehicles - repair
      while (result.Count > ProblemInstance.Vehicles.Value) {
        Tour tour = result[result.Count - 1];

        //find predecessor / successor in permutation
        int predecessorIndex = Array.IndexOf(this.array, tour.Stops[0] - 1) - 1;
        if (predecessorIndex >= 0) {
          int predecessor = this[predecessorIndex] + 1;

          foreach (Tour t in result) {
            int insertPosition = t.Stops.IndexOf(predecessor) + 1;
            if (insertPosition != -1) {
              t.Stops.InsertRange(insertPosition, tour.Stops);
              break;
            }
          }
        } else {
          int successorIndex = Array.IndexOf(this.array,
            tour.Stops[tour.Stops.Count - 1] - 1) + 1;
          int successor = this[successorIndex] + 1;

          foreach (Tour t in result) {
            int insertPosition = t.Stops.IndexOf(successor);
            if (insertPosition != -1) {
              t.Stops.InsertRange(insertPosition, tour.Stops);
              break;
            }
          }
        }

        result.Remove(tour);
      }

      return result;
    }
    #endregion

    public ZhuEncodedSolution(Permutation permutation, IVRPProblemInstance problemInstance)
      : base(permutation, problemInstance) {
    }

    public override IDeepCloneable Clone(Cloner cloner) {
      return new ZhuEncodedSolution(this, cloner);
    }

    protected ZhuEncodedSolution(ZhuEncodedSolution original, Cloner cloner)
      : base(original, cloner) {
    }

    [StorableConstructor]
    protected ZhuEncodedSolution(StorableConstructorFlag _) : base(_) {
    }

    public static ZhuEncodedSolution ConvertFrom(IVRPEncodedSolution encoding, IVRPProblemInstance problemInstance) {
      List<Tour> tours = encoding.GetTours();
      var count = tours.Sum(x => x.Stops.Count);
      var route = new int[count];

      var i = 0;
      foreach (Tour tour in tours) {
        foreach (int city in tour.Stops)
          route[i++] = city - 1;
      }

      return new ZhuEncodedSolution(
        new Permutation(PermutationTypes.RelativeUndirected, route), problemInstance);
    }

    public static ZhuEncodedSolution ConvertFrom(List<int> routeParam, IVRPProblemInstance problemInstance) {
      var route = routeParam.Where(x => x != 0).Select(x => x - 1).ToArray();

      return new ZhuEncodedSolution(
        new Permutation(PermutationTypes.RelativeUndirected, route), problemInstance);
    }
  }
}