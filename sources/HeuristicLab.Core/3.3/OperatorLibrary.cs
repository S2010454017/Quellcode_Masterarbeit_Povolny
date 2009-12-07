#region License Information
/* HeuristicLab
 * Copyright (C) 2002-2008 Heuristic and Evolutionary Algorithms Laboratory (HEAL)
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
using System.Text;
using System.Xml;
using HeuristicLab.Persistence.Default.CompositeSerializers.Storable;

namespace HeuristicLab.Core {
  /// <summary>
  /// Represents a library of operators consisting of one <see cref="IOperatorGroup"/>.
  /// </summary>
  [Creatable("Libraries")]
  [Item("Operator Library", "A library that contains multiple operators.")]
  public class OperatorLibrary : ItemBase, IOperatorLibrary {

    [Storable]
    private IOperatorGroup myGroup;
    /// <summary>
    /// Gets the operator group of the current instance.
    /// </summary>
    public IOperatorGroup Group {
      get { return myGroup; }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="OperatorLibrary"/>.
    /// </summary>
    public OperatorLibrary() {
      myGroup = new OperatorGroup();
    }

    /// <summary>
    /// Clones the current instance (deep clone).
    /// </summary>
    /// <remarks>Deep clone through <see cref="cloner.Clone"/> method of helper class 
    /// <see cref="Auxiliary"/>.</remarks>
    /// <param name="clonedObjects">Dictionary of all already cloned objects. (Needed to avoid cycles.)</param>
    /// <returns>The cloned object as <see cref="OperatorLibrary"/>.</returns>
    public override IItem Clone(ICloner cloner) {
      OperatorLibrary clone = new OperatorLibrary();
      cloner.RegisterClonedObject(this, clone);
      clone.myGroup = (IOperatorGroup)cloner.Clone(Group);
      return clone;
    }
  }
}
