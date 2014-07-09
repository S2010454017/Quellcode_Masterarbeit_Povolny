﻿#region License Information
/* HeuristicLab
 * Copyright (C) 2002-2014 Heuristic and Evolutionary Algorithms Laboratory (HEAL)
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
using HeuristicLab.Common;

namespace HeuristicLab.Clients.Hive {
  public partial class MessageContainer : IDeepCloneable, IContent {

    protected MessageContainer() { }
    public MessageContainer(MessageType message) {
      Message = message;
      TaskId = Guid.Empty;
    }
    public MessageContainer(MessageType message, Guid jobId) {
      Message = message;
      TaskId = jobId;
    }

    protected MessageContainer(MessageContainer original, Cloner cloner) {
      cloner.RegisterClonedObject(original, this);
      this.Message = original.Message;
      this.TaskId = original.TaskId;
    }
    public virtual IDeepCloneable Clone(Cloner cloner) {
      return new MessageContainer(this, cloner);
    }
    public object Clone() {
      return Clone(new Cloner());
    }
  }
}