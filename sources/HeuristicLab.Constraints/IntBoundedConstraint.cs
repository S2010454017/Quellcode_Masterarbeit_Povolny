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
using HeuristicLab.Core;
using HeuristicLab.Data;

namespace HeuristicLab.Constraints {
  public class IntBoundedConstraint : ConstraintBase {
    private int lowerBound;
    public int LowerBound {
      get { return lowerBound; }
      set {
        lowerBound = value;
        OnChanged();
      }
    }
    private bool lowerBoundIncluded;
    public bool LowerBoundIncluded {
      get { return lowerBoundIncluded; }
      set {
        lowerBoundIncluded = value;
        OnChanged();
      }
    }
    private bool lowerBoundEnabled;
    public bool LowerBoundEnabled {
      get { return lowerBoundEnabled; }
      set {
        lowerBoundEnabled = value;
        OnChanged();
      }
    }
    private int upperBound;
    public int UpperBound {
      get { return upperBound; }
      set {
        upperBound = value;
        OnChanged();
      }
    }
    private bool upperBoundIncluded;
    public bool UpperBoundIncluded {
      get { return upperBoundIncluded; }
      set {
        upperBoundIncluded = value;
        OnChanged();
      }
    }
    private bool upperBoundEnabled;
    public bool UpperBoundEnabled {
      get { return upperBoundEnabled; }
      set {
        upperBoundEnabled = value;
        OnChanged();
      }
    }

    public override string Description {
      get { return "The integer is limited one or two sided by a lower and/or upper boundary"; }
    }

    public IntBoundedConstraint()
      : this(int.MinValue, int.MaxValue) {
    }

    public IntBoundedConstraint(int low, int high) : base() {
      lowerBound = low;
      lowerBoundIncluded = false;
      lowerBoundEnabled = true;
      upperBound = high;
      upperBoundIncluded = false;
      upperBoundEnabled = true;
    }

    public override bool Check(IItem data) {
      ConstrainedIntData d = (data as ConstrainedIntData);
      if (d == null) return false;
      if (LowerBoundEnabled && ((LowerBoundIncluded && d.CompareTo(LowerBound) < 0)
        || (!LowerBoundIncluded && d.CompareTo(LowerBound) <= 0))) return false;
      if (UpperBoundEnabled && ((UpperBoundIncluded && d.CompareTo(UpperBound) > 0)
        || (!UpperBoundIncluded && d.CompareTo(UpperBound) >= 0))) return false;
      return true;
    }

    public override IView CreateView() {
      return new IntBoundedConstraintView(this);
    }

    public override object Clone(IDictionary<Guid, object> clonedObjects) {
      IntBoundedConstraint clone = new IntBoundedConstraint();
      clonedObjects.Add(Guid, clone);
      clone.upperBound = UpperBound;
      clone.upperBoundIncluded = UpperBoundIncluded;
      clone.upperBoundEnabled = UpperBoundEnabled;
      clone.lowerBound = LowerBound;
      clone.lowerBoundIncluded = LowerBoundIncluded;
      clone.lowerBoundEnabled = LowerBoundEnabled;
      return clone;
    }

    #region persistence
    public override XmlNode GetXmlNode(string name, XmlDocument document, IDictionary<Guid, IStorable> persistedObjects) {
      XmlNode node = base.GetXmlNode(name, document, persistedObjects);
      XmlAttribute lb = document.CreateAttribute("LowerBound");
      lb.Value = LowerBound + "";
      XmlAttribute lbi = document.CreateAttribute("LowerBoundIncluded");
      lbi.Value = lowerBoundIncluded + "";
      XmlAttribute lbe = document.CreateAttribute("LowerBoundEnabled");
      lbe.Value = lowerBoundEnabled + "";
      XmlAttribute ub = document.CreateAttribute("UpperBound");
      ub.Value = upperBound + "";
      XmlAttribute ubi = document.CreateAttribute("UpperBoundIncluded");
      ubi.Value = upperBoundIncluded + "";
      XmlAttribute ube = document.CreateAttribute("UpperBoundEnabled");
      ube.Value = upperBoundEnabled + "";
      node.Attributes.Append(lb);
      if (!lowerBoundIncluded) node.Attributes.Append(lbi);
      if (!lowerBoundEnabled) node.Attributes.Append(lbe);
      node.Attributes.Append(ub);
      if (!upperBoundIncluded) node.Attributes.Append(ubi);
      if (!upperBoundEnabled) node.Attributes.Append(ube);
      return node;
    }

    public override void Populate(XmlNode node, IDictionary<Guid, IStorable> restoredObjects) {
      base.Populate(node, restoredObjects);
      lowerBound = int.Parse(node.Attributes["LowerBound"].Value);
      if (node.Attributes["LowerBoundIncluded"] != null) {
        lowerBoundIncluded = bool.Parse(node.Attributes["LowerBoundIncluded"].Value);
      } else {
        lowerBoundIncluded = true;
      }
      if (node.Attributes["LowerBoundEnabled"] != null) {
        lowerBoundEnabled = bool.Parse(node.Attributes["LowerBoundEnabled"].Value);
      } else {
        lowerBoundEnabled = true;
      }

      upperBound = int.Parse(node.Attributes["UpperBound"].Value);
      if (node.Attributes["UpperBoundIncluded"] != null) {
        upperBoundIncluded = bool.Parse(node.Attributes["UpperBoundIncluded"].Value);
      } else {
        upperBoundIncluded = true;
      }
      if (node.Attributes["UpperBoundEnabled"] != null) {
        upperBoundEnabled = bool.Parse(node.Attributes["UpperBoundEnabled"].Value);
      } else {
        upperBoundEnabled = true;
      }
    }
    #endregion
  }
}
