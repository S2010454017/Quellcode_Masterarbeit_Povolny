﻿#region License Information
/* HeuristicLab
 * Copyright (C) 2002-2012 Heuristic and Evolutionary Algorithms Laboratory (HEAL)
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
using HeuristicLab.Common;
using HeuristicLab.Core;
using HeuristicLab.Persistence.Default.CompositeSerializers.Storable;

namespace HeuristicLab.Algorithms.DataAnalysis {
  [StorableClass]
  [Item(Name = "CovarianceLinear", Description = "Linear covariance function for Gaussian processes.")]
  public class CovarianceLinear : CovarianceFunction {
    public override int GetNumberOfParameters(int numberOfVariables) {
      return 0;
    }
    [StorableConstructor]
    protected CovarianceLinear(bool deserializing) : base(deserializing) { }
    protected CovarianceLinear(CovarianceLinear original, Cloner cloner)
      : base(original, cloner) {
    }
    public CovarianceLinear()
      : base() {
    }

    public override IDeepCloneable Clone(Cloner cloner) {
      return new CovarianceLinear(this, cloner);
    }

    public override void SetParameter(double[] hyp) {
      if (hyp.Length > 0) throw new ArgumentException("No hyperparameters are allowed for the linear covariance function.");
    }

    public override double GetCovariance(double[,] x, int i, int j) {
      return Util.ScalarProd(x, i, j);
    }

    public override IEnumerable<double> GetGradient(double[,] x, int i, int j) {
      yield break;
    }

    public override double GetCrossCovariance(double[,] x, double[,] xt, int i, int j) {
      return Util.ScalarProd(x, i, xt, j);
    }
  }
}
