﻿using System;
using System.Collections.Generic;
using System.Text;
using HeuristicLab.Persistence.Interfaces;
using HeuristicLab.Persistence.Default.Xml;
using HeuristicLab.Persistence.Core;
using HeuristicLab.Persistence.Core.Tokens;
using HeuristicLab.Persistence.Default.CompositeSerializers.Storable;

namespace HeuristicLab.Persistence.Default.DebugString {

  [StorableClass]
  public class DebugStringFormat : FormatBase<DebugString> {
    public override string Name { get { return "DebugString"; } }
  }

}
