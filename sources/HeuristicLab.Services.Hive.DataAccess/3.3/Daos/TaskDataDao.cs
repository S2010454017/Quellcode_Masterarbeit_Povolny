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
using System.Data.Linq;
using System.Linq;

namespace HeuristicLab.Services.Hive.DataAccess.Daos {
  public class TaskDataDao : GenericDao<Guid, TaskData> {
    public TaskDataDao(DataContext dataContext) : base(dataContext) { }

    public override TaskData GetById(Guid id) {
      return GetByIdQuery(DataContext, id);
    }

    #region Compiled queries
    private static readonly Func<DataContext, Guid, TaskData> GetByIdQuery =
      CompiledQuery.Compile((DataContext db, Guid taskId) =>
        (from taskData in db.GetTable<TaskData>()
         where taskData.TaskId == taskId
         select taskData).SingleOrDefault());
    #endregion
  }
}