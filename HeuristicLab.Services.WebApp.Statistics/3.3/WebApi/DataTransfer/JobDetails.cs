﻿#region License Information
/* HeuristicLab
 * Copyright (C) 2002-2015 Heuristic and Evolutionary Algorithms Laboratory (HEAL)
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

namespace HeuristicLab.Services.WebApp.Statistics.WebApi.DataTransfer {
  public class JobDetails {
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public DateTime DateCreated { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public DateTime? DateCompleted { get; set; }
    public long AvgTransferringTime { get; set; }
    public long MinCalculatingTime { get; set; }
    public long MaxCalculatingTime { get; set; }
    public long AvgCalculatingTime { get; set; }
    public long TotalCalculatingTime { get; set; }
    public long TotalWaitingTime { get; set; }
    public long TotalTime { get; set; }
    public IEnumerable<TaskStateCount> TasksStates { get; set; }
  }
}