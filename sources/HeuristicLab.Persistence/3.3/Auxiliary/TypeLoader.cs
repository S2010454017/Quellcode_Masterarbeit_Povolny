﻿#region License Information
/* HeuristicLab
 * Copyright (C) 2002-2012 Heuristic and Evolutionary Algorithms Laboratory (HEAL)
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
using System.Reflection;
using HeuristicLab.Persistence.Core;
using HeuristicLab.Tracing;

namespace HeuristicLab.Persistence.Auxiliary {
  internal class TypeLoader {
    public static Type Load(string typeNameString) {
      try {
        // try to load type normally
        return LoadInternal(typeNameString);
      }
      catch (PersistenceException) {
        #region Mono Compatibility
        // if that fails, try to load Mono type
        string monoTypeNameString = GetMonoType(typeNameString);
        Logger.Info(String.Format(@"Trying to load Mono type ""{0}"" instead of type ""{1}""",
                                  monoTypeNameString, typeNameString));
        return LoadInternal(monoTypeNameString);
      }
        #endregion
    }

    private static Type LoadInternal(string typeNameString) {
      Type type;
      try {
        type = Type.GetType(typeNameString, true);
        #region Mono Compatibility
        // mono: workaround until Mono bug #580 (http://bugzilla.xamarin.com/show_bug.cgi?id=580) is fixed
        if (type.AssemblyQualifiedName != typeNameString)
          throw new TypeLoadException(
            String.Format(
            @"Could not load requested type ""{0}"", loaded ""{1}"" instead.",
            typeNameString, type.AssemblyQualifiedName));
        #endregion
      }
      catch (Exception) {
        Logger.Warn(String.Format(
          "Cannot load type \"{0}\", falling back to partial name", typeNameString));
        type = LoadWithPartialName(typeNameString);
        CheckCompatibility(typeNameString, type);
      }
      return type;
    }

    private static Type LoadWithPartialName(string typeNameString) {
      try {
        TypeName typeName = TypeNameParser.Parse(typeNameString);
#pragma warning disable 0618
        Assembly a = Assembly.LoadWithPartialName(typeName.AssemblyName);
        // the suggested Assembly.Load() method fails to load assemblies outside the GAC
#pragma warning restore 0618
        return a.GetType(typeName.ToString(false, false), true);
      }
      catch (Exception) {
        throw new PersistenceException(String.Format(
          "Could not load type \"{0}\"",
          typeNameString));
      }
    }

    private static void CheckCompatibility(string typeNameString, Type type) {
      try {
        TypeName requestedTypeName = TypeNameParser.Parse(typeNameString);
        TypeName loadedTypeName = TypeNameParser.Parse(type.AssemblyQualifiedName);
        if (!requestedTypeName.IsCompatible(loadedTypeName))
          throw new PersistenceException(String.Format(
            "Serialized type is incompatible with available type: serialized: {0}, loaded: {1}",
            typeNameString,
            type.AssemblyQualifiedName));
        if (requestedTypeName.IsNewerThan(loadedTypeName))
          throw new PersistenceException(String.Format(
            "Serialized type is newer than available type: serialized: {0}, loaded: {1}",
            typeNameString,
            type.AssemblyQualifiedName));
      }
      catch (PersistenceException) {
        throw;
      }
      catch (Exception e) {
        Logger.Warn(String.Format(
          "Could not perform version check requested type was {0} while loaded type is {1}:",
          typeNameString,
          type.AssemblyQualifiedName),
                    e);
      }
    }

    #region Mono Compatibility
    /// <summary>
    /// Returns the corresponding type for the Mono runtime
    /// </summary>
    /// <returns>
    /// The remapped typeNameString, or the original string if no mapping was found
    /// </returns>
    private static string GetMonoType(string typeNameString) {
      TypeName typeName = TypeNameParser.Parse(typeNameString);

      // map System.RuntimeType to System.MonoType
      if (typeName.Namespace == "System" && typeName.ClassName == "RuntimeType") {
        // we use Int32 here because we get all the information about Mono's mscorlib and just have to change the class name
        typeName = TypeNameParser.Parse(typeof(System.Int32).AssemblyQualifiedName);
        typeName.ClassName = "MonoType";
      } else if (typeName.Namespace == "System.Collections.Generic" && typeName.ClassName == "ObjectEqualityComparer") {
        // map System.Collections.Generic.ObjectEqualityComparer to HeuristicLab.Mono.ObjectEqualityComparer       
        // we need the information about the Persistence assembly, so we use TypeName here because it is contained in this assembly
        TypeName oecInfo = TypeNameParser.Parse(typeof(TypeName).AssemblyQualifiedName);
        typeName.Namespace = "HeuristicLab.Persistence.Mono";
        typeName.AssemblyName = oecInfo.AssemblyName;
        typeName.AssemblyAttribues.Clear();
        typeName.AssemblyAttribues["Version"] = oecInfo.AssemblyAttribues["Version"];
        typeName.AssemblyAttribues["Culture"] = oecInfo.AssemblyAttribues["Culture"];
        typeName.AssemblyAttribues["PublicKeyToken"] = oecInfo.AssemblyAttribues["PublicKeyToken"];
      }

      return typeName.ToString(true, true);
    }
    #endregion
  }
}