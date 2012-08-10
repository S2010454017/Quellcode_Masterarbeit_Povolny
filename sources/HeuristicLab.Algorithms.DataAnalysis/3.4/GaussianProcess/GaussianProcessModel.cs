#region License Information
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
using System.Linq;
using HeuristicLab.Common;
using HeuristicLab.Core;
using HeuristicLab.Persistence.Default.CompositeSerializers.Storable;
using HeuristicLab.Problems.DataAnalysis;

namespace HeuristicLab.Algorithms.DataAnalysis {
  /// <summary>
  /// Represents a Gaussian process model.
  /// </summary>
  [StorableClass]
  [Item("GaussianProcessModel", "Represents a Gaussian process posterior.")]
  public sealed class GaussianProcessModel : NamedItem, IGaussianProcessModel {
    [Storable]
    private double negativeLogLikelihood;
    public double NegativeLogLikelihood {
      get { return negativeLogLikelihood; }
    }

    [Storable]
    private ICovarianceFunction covarianceFunction;
    public ICovarianceFunction CovarianceFunction {
      get { return covarianceFunction; }
    }
    [Storable]
    private IMeanFunction meanFunction;
    public IMeanFunction MeanFunction {
      get { return meanFunction; }
    }
    [Storable]
    private string targetVariable;
    public string TargetVariable {
      get { return targetVariable; }
    }
    [Storable]
    private string[] allowedInputVariables;
    public string[] AllowedInputVariables {
      get { return allowedInputVariables; }
    }

    [Storable]
    private double[] alpha;
    [Storable]
    private double sqrSigmaNoise;

    [Storable]
    private double[,] l;

    [Storable]
    private double[,] x;
    [Storable]
    private Scaling inputScaling;


    [StorableConstructor]
    private GaussianProcessModel(bool deserializing) : base(deserializing) { }
    private GaussianProcessModel(GaussianProcessModel original, Cloner cloner)
      : base(original, cloner) {
      this.meanFunction = cloner.Clone(original.meanFunction);
      this.covarianceFunction = cloner.Clone(original.covarianceFunction);
      this.inputScaling = cloner.Clone(original.inputScaling);
      this.negativeLogLikelihood = original.negativeLogLikelihood;
      this.targetVariable = original.targetVariable;
      this.sqrSigmaNoise = original.sqrSigmaNoise;

      // shallow copies of arrays because they cannot be modified
      this.allowedInputVariables = original.allowedInputVariables;
      this.alpha = original.alpha;
      this.l = original.l;
      this.x = original.x;
    }
    public GaussianProcessModel(Dataset ds, string targetVariable, IEnumerable<string> allowedInputVariables, IEnumerable<int> rows,
      IEnumerable<double> hyp, IMeanFunction meanFunction, ICovarianceFunction covarianceFunction)
      : base() {
      this.name = ItemName;
      this.description = ItemDescription;
      this.meanFunction = (IMeanFunction)meanFunction.Clone();
      this.covarianceFunction = (ICovarianceFunction)covarianceFunction.Clone();
      this.targetVariable = targetVariable;
      this.allowedInputVariables = allowedInputVariables.ToArray();


      int nVariables = this.allowedInputVariables.Length;
      this.meanFunction.SetParameter(hyp
        .Take(this.meanFunction.GetNumberOfParameters(nVariables))
        .ToArray());
      this.covarianceFunction.SetParameter(hyp.Skip(this.meanFunction.GetNumberOfParameters(nVariables))
        .Take(this.covarianceFunction.GetNumberOfParameters(nVariables))
        .ToArray());
      sqrSigmaNoise = Math.Exp(2.0 * hyp.Last());

      CalculateModel(ds, rows);
    }

    private void CalculateModel(Dataset ds, IEnumerable<int> rows) {
      inputScaling = new Scaling(ds, allowedInputVariables, rows);
      x = AlglibUtil.PrepareAndScaleInputMatrix(ds, allowedInputVariables, rows, inputScaling);
      var y = ds.GetDoubleValues(targetVariable, rows);

      int n = x.GetLength(0);
      l = new double[n, n];

      meanFunction.SetData(x);
      covarianceFunction.SetData(x);

      // calculate means and covariances
      double[] m = meanFunction.GetMean(x);
      for (int i = 0; i < n; i++) {

        for (int j = i; j < n; j++) {
          l[j, i] = covarianceFunction.GetCovariance(i, j) / sqrSigmaNoise;
          if (j == i) l[j, i] += 1.0;
        }
      }

      // cholesky decomposition
      int info;
      alglib.densesolverreport denseSolveRep;

      var res = alglib.trfac.spdmatrixcholesky(ref l, n, false);
      if (!res) throw new ArgumentException("Matrix is not positive semidefinite");

      // calculate sum of diagonal elements for likelihood
      double diagSum = Enumerable.Range(0, n).Select(i => Math.Log(l[i, i])).Sum();

      // solve for alpha
      double[] ym = y.Zip(m, (a, b) => a - b).ToArray();

      alglib.spdmatrixcholeskysolve(l, n, false, ym, out info, out denseSolveRep, out alpha);
      for (int i = 0; i < alpha.Length; i++)
        alpha[i] = alpha[i] / sqrSigmaNoise;
      negativeLogLikelihood = 0.5 * Util.ScalarProd(ym, alpha) + diagSum + (n / 2.0) * Math.Log(2.0 * Math.PI * sqrSigmaNoise);
    }

    public double[] GetHyperparameterGradients() {
      // derivatives
      int n = x.GetLength(0);
      int nAllowedVariables = x.GetLength(1);

      int info;
      alglib.matinvreport matInvRep;

      alglib.spdmatrixcholeskyinverse(ref l, n, false, out info, out matInvRep);
      if (info != 1) throw new ArgumentException("Can't invert matrix to calculate gradients.");
      for (int i = 0; i < n; i++) {
        for (int j = 0; j <= i; j++)
          l[i, j] = l[i, j] / sqrSigmaNoise - alpha[i] * alpha[j];
      }

      double noiseGradient = sqrSigmaNoise * Enumerable.Range(0, n).Select(i => l[i, i]).Sum();

      double[] meanGradients = new double[meanFunction.GetNumberOfParameters(nAllowedVariables)];
      for (int i = 0; i < meanGradients.Length; i++) {
        var meanGrad = meanFunction.GetGradients(i, x);
        meanGradients[i] = -Util.ScalarProd(meanGrad, alpha);
      }

      double[] covGradients = new double[covarianceFunction.GetNumberOfParameters(nAllowedVariables)];
      if (covGradients.Length > 0) {
        for (int i = 0; i < n; i++) {
          for (int k = 0; k < covGradients.Length; k++) {
            for (int j = 0; j < i; j++) {
              covGradients[k] += l[i, j] * covarianceFunction.GetGradient(i, j, k);
            }
            covGradients[k] += 0.5 * l[i, i] * covarianceFunction.GetGradient(i, i, k);
          }
        }
      }

      return
        meanGradients
        .Concat(covGradients)
        .Concat(new double[] { noiseGradient }).ToArray();
    }


    public override IDeepCloneable Clone(Cloner cloner) {
      return new GaussianProcessModel(this, cloner);
    }

    #region IRegressionModel Members
    public IEnumerable<double> GetEstimatedValues(Dataset dataset, IEnumerable<int> rows) {
      return GetEstimatedValuesHelper(dataset, rows);
    }
    public GaussianProcessRegressionSolution CreateRegressionSolution(IRegressionProblemData problemData) {
      return new GaussianProcessRegressionSolution(this, problemData);
    }
    IRegressionSolution IRegressionModel.CreateRegressionSolution(IRegressionProblemData problemData) {
      return CreateRegressionSolution(problemData);
    }
    #endregion

    private IEnumerable<double> GetEstimatedValuesHelper(Dataset dataset, IEnumerable<int> rows) {
      var newX = AlglibUtil.PrepareAndScaleInputMatrix(dataset, allowedInputVariables, rows, inputScaling);
      int newN = newX.GetLength(0);
      int n = x.GetLength(0);
      // var predMean = new double[newN];
      // predVar = new double[newN];



      // var kss = new double[newN];
      var Ks = new double[newN, n];
      //double[,] sWKs = new double[n, newN];
      // double[,] v;


      // for stddev 
      //covarianceFunction.SetParameter(covHyp, newX);
      //kss = covarianceFunction.GetDiagonalCovariances();

      covarianceFunction.SetData(x, newX);
      meanFunction.SetData(newX);
      var ms = meanFunction.GetMean(newX);
      for (int i = 0; i < newN; i++) {
        for (int j = 0; j < n; j++) {
          Ks[i, j] = covarianceFunction.GetCovariance(j, i);
          //sWKs[j, i] = Ks[i, j] / Math.Sqrt(sqrSigmaNoise);
        }
      }

      // for stddev 
      // alglib.rmatrixsolvem(l, n, sWKs, newN, true, out info, out denseSolveRep, out v);

      return Enumerable.Range(0, newN)
        .Select(i => ms[i] + Util.ScalarProd(Util.GetRow(Ks, i), alpha));
      //for (int i = 0; i < newN; i++) {
      //  // predMean[i] = ms[i] + prod(GetRow(Ks, i), alpha);
      //  // var sumV2 = prod(GetCol(v, i), GetCol(v, i));
      //  // predVar[i] = kss[i] - sumV2;
      //}

    }

    public IEnumerable<double> GetEstimatedVariance(Dataset dataset, IEnumerable<int> rows) {
      var newX = AlglibUtil.PrepareAndScaleInputMatrix(dataset, allowedInputVariables, rows, inputScaling);
      int newN = newX.GetLength(0);
      int n = x.GetLength(0);

      var kss = new double[newN];
      double[,] sWKs = new double[n, newN];


      // for stddev 
      covarianceFunction.SetData(newX);
      for (int i = 0; i < newN; i++)
        kss[i] = covarianceFunction.GetCovariance(i, i);

      covarianceFunction.SetData(x, newX);
      for (int i = 0; i < n; i++) {
        for (int j = 0; j < newN; j++) {
          sWKs[i, j] = covarianceFunction.GetCovariance(i, j) / Math.Sqrt(sqrSigmaNoise);
        }
      }

      // for stddev 
      int info;
      alglib.densesolverreport denseSolveRep;
      double[,] v;
      double[,] lTrans = new double[l.GetLength(1), l.GetLength(0)];
      for (int i = 0; i < lTrans.GetLength(0); i++)
        for (int j = 0; j < lTrans.GetLength(1); j++)
          lTrans[i, j] = l[j, i];
      alglib.rmatrixsolvem(lTrans, n, sWKs, newN, true, out info, out denseSolveRep, out v); // not working!
      // alglib.spdmatrixcholeskysolvem(lTrans, n, true, sWKs, newN, out info, out denseSolveRep, out v);

      for (int i = 0; i < newN; i++) {
        var sumV2 = Util.ScalarProd(Util.GetCol(v, i), Util.GetCol(v, i));
        yield return kss[i] - sumV2;
      }
    }
  }
}
