﻿using System;
using System.Windows.Forms;

namespace HeuristicLab.JsonInterface.OptimizerIntegration {
  public class ResultItemVM : JsonItemVMBase<ResultJsonItem> {
    public override Type TargetedJsonItemType => typeof(ResultJsonItem);
    public override UserControl Control => ResultJsonItemControl.Create(this);

    public string ResultFormatterType {
      get => Item.ResultFormatterType; 
      set {
        Item.ResultFormatterType = value;
        OnPropertyChange(this, nameof(ResultFormatterType));
      }
    }
  }
}