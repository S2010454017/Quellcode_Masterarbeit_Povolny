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

using System;
using HeuristicLab.Collections;
using HeuristicLab.Common;
using HeuristicLab.Core;

namespace HeuristicLab.Optimization {
  /// <summary>
  /// Interface to represent an algorithm.
  /// </summary>
  public interface IAlgorithm : IParameterizedNamedItem {
    Type ProblemType { get; }
    IProblem Problem { get; set; }
    ResultCollection Results { get; }
    TimeSpan ExecutionTime { get; }
    bool Running { get; }
    bool Finished { get; }

    void Prepare();
    void Start();
    void Stop();

    event EventHandler ProblemChanged;
    event EventHandler ExecutionTimeChanged;
    event EventHandler RunningChanged;
    event EventHandler Prepared;
    event EventHandler Started;
    event EventHandler Stopped;
    event EventHandler<EventArgs<Exception>> ExceptionOccurred;
  }
}
