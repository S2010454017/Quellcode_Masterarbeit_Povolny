﻿using System.Collections.Generic;
using System.Linq;
namespace HeuristicLab.JsonInterface {
  public abstract class ConcreteRestrictedArrayJsonItem<T> : ArrayJsonItem<T>, IConcreteRestrictedJsonItem<T> {
    public IEnumerable<T> ConcreteRestrictedItems { get; set; }

    protected override ValidationResult Validate() {
      bool res = true;
      IList<string> errors = new List<string>();
      if (ConcreteRestrictedItems == null) return ValidationResult.Successful();
      foreach(var x in Value) {
        bool tmp = false;
        foreach(var restrictedItem in ConcreteRestrictedItems) {
          tmp = tmp || x.Equals(restrictedItem); //if one tmp is true, it stays true (match found)
        }
        if (!tmp)
          errors.Add($"[{Path}]: Value '{x}' is not one of the allowed values: " +
                     $"'{ string.Join(",", ConcreteRestrictedItems.Select(s => s.ToString()).ToArray()) }'.");
        res = res && tmp; //if one tmp is false, res will set false
      }
      if (res) 
        return ValidationResult.Successful();
      else 
        return ValidationResult.Faulty(errors);
    }
  }
}