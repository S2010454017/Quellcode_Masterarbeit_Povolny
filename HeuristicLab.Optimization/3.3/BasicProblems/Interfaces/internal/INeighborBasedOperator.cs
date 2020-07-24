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
using HEAL.Attic;
using HeuristicLab.Core;

namespace HeuristicLab.Optimization {
  [StorableType("fda56e0b-9392-4711-9af1-55211bfa24ac")]
  internal interface INeighborBasedOperator<TEncodedSolution> : IEncodingOperator
  where TEncodedSolution : class, IEncodedSolution {
    Func<ISingleObjectiveSolutionContext<TEncodedSolution>, IRandom, IEnumerable<ISingleObjectiveSolutionContext<TEncodedSolution>>> GetNeighbors { get; set; }
  }
}
