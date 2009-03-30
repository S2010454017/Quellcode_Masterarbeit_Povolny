﻿#region License Information
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
using System.Linq;
using System.Text;
using HeuristicLab.Hive.Server.DataAccess;
using HeuristicLab.Hive.Contracts.BusinessObjects;
using HeuristicLab.DataAccess.ADOHelper;
using HeuristicLab.Hive.Server.ADODataAccess.dsHiveServerTableAdapters;
using System.Data.Common;
using System.Data.SqlClient;

namespace HeuristicLab.Hive.Server.ADODataAccess {
  class ResourceAdapterWrapper :
    DataAdapterWrapperBase<
      dsHiveServerTableAdapters.ResourceTableAdapter,
      Resource,
      dsHiveServer.ResourceRow> {    
    public override void UpdateRow(dsHiveServer.ResourceRow row) {
      TransactionalAdapter.Update(row);
    }

    public override dsHiveServer.ResourceRow
      InsertNewRow(Resource resource) {
      dsHiveServer.ResourceDataTable data =
        new dsHiveServer.ResourceDataTable();

      dsHiveServer.ResourceRow row = data.NewResourceRow();
      row.ResourceId = resource.Id;
      data.AddResourceRow(row);

      return row;
    }

    public override IEnumerable<dsHiveServer.ResourceRow>
      FindById(Guid id) {
      return TransactionalAdapter.GetDataById(id);
    }

    public override IEnumerable<dsHiveServer.ResourceRow>
      FindAll() {
      return TransactionalAdapter.GetData();
    }

    protected override void SetConnection(DbConnection connection) {
      adapter.Connection = connection as SqlConnection;
    }

    protected override void SetTransaction(DbTransaction transaction) {
      adapter.Transaction = transaction as SqlTransaction;
    }
  }
  
  class ResourceAdapter: 
    DataAdapterBase<
      dsHiveServerTableAdapters.ResourceTableAdapter, 
      Resource, 
      dsHiveServer.ResourceRow>,  
    IResourceAdapter {
    #region Fields
    private IClientAdapter clientAdapter = null;

    private IClientAdapter ClientAdapter {
      get {
        if (clientAdapter == null)
          clientAdapter =
            this.Session.GetDataAdapter<ClientInfo, IClientAdapter>();
        
        return clientAdapter;
      }
    }
    #endregion

    public ResourceAdapter(): base(new ResourceAdapterWrapper()) {
    }

    #region Overrides
    protected override Resource ConvertRow(dsHiveServer.ResourceRow row,
      Resource resource) {
      if (row != null && resource != null) {
        resource.Id = row.ResourceId;
        if (!row.IsNameNull())
          resource.Name = row.Name;
        else
          resource.Name = String.Empty;

        return resource;
      } else
        return null;
    }

    protected override dsHiveServer.ResourceRow ConvertObj(Resource resource,
      dsHiveServer.ResourceRow row) {
      if (resource != null && row != null) {
        row.ResourceId = resource.Id;
        row.Name = resource.Name;

        return row;
      } else
        return null;
    }
    #endregion

    #region IResourceAdapter Members
    public bool GetById(Resource resource) {
      if (resource != null) {
        dsHiveServer.ResourceRow row =
          GetRowById(resource.Id);

        if (row != null) {
          Convert(row, resource);

          return true;
        }
      }

      return false;
    }

    public Resource GetByName(string name) {
      dsHiveServer.ResourceRow row =
        base.FindSingleRow(
          delegate() {
            return Adapter.GetDataByName(name);
          });

      if (row != null) {
        Resource res = new Resource();
        res = Convert(row, res);

        return res;
      } else {
        return null;
      }
    }

    #endregion
  }
}
