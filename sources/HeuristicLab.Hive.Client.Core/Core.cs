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
using HeuristicLab.Hive.Client.ExecutionEngine;
using HeuristicLab.Hive.Client.Common;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Security.Permissions;
using System.Security.Policy;
using System.Security;
using HeuristicLab.Hive.Client.Communication;
using HeuristicLab.Hive.Contracts.BusinessObjects;
using HeuristicLab.Hive.Contracts;
using System.Runtime.Remoting.Messaging;
using HeuristicLab.PluginInfrastructure;


namespace HeuristicLab.Hive.Client.Core {
  public class Core {

    public delegate string GetASnapshotDelegate();

    Dictionary<long, Executor> engines = new Dictionary<long, Executor>();
    Dictionary<long, AppDomain> appDomains = new Dictionary<long, AppDomain>();

    public static StrongName CreateStrongName(Assembly assembly) {
      if (assembly == null)
        throw new ArgumentNullException("assembly");

      AssemblyName assemblyName = assembly.GetName();
      Debug.Assert(assemblyName != null, "Could not get assembly name");

      // get the public key blob
      byte[] publicKey = assemblyName.GetPublicKey();
      if (publicKey == null || publicKey.Length == 0)
        throw new InvalidOperationException("Assembly is not strongly named");

      StrongNamePublicKeyBlob keyBlob = new StrongNamePublicKeyBlob(publicKey);

      // and create the StrongName
      return new StrongName(keyBlob, assemblyName.Name, assemblyName.Version);
    }

    private ClientCommunicatorClient clientCommunicator;

    public void Start() {
      Heartbeat beat = new Heartbeat { Interval = 5000 };
      beat.StartHeartbeat();

      ClientInfo clientInfo = new ClientInfo { ClientId = Guid.NewGuid() };

      clientCommunicator = ServiceLocator.GetClientCommunicator();
      clientCommunicator.LoginCompleted += new EventHandler<LoginCompletedEventArgs>(ClientCommunicator_LoginCompleted);
      clientCommunicator.PullJobCompleted += new EventHandler<PullJobCompletedEventArgs>(ClientCommunicator_PullJobCompleted);
      clientCommunicator.SendJobResultCompleted += new EventHandler<SendJobResultCompletedEventArgs>(ClientCommunicator_SendJobResultCompleted);
      clientCommunicator.LoginAsync(clientInfo);

      MessageQueue queue = MessageQueue.GetInstance();
      while (true) {
        MessageContainer container = queue.GetMessage();
        Debug.WriteLine("Main loop received this message: " + container.Message.ToString());
        Logging.GetInstance().Info(this.ToString(), container.Message.ToString());
        DetermineAction(container);
      }
    }

    void ClientCommunicator_LoginCompleted(object sender, LoginCompletedEventArgs e) {
      if (e.Result.Success) {
        Logging.GetInstance().Info(this.ToString(), "Login completed to Hive Server @ " + DateTime.Now);
        Status.LoginTime = DateTime.Now;
        Status.LoggedIn = true;
      } else
        Logging.GetInstance().Error(this.ToString(), e.Result.StatusMessage);
    }

    private AppDomain CreateNewAppDomain(bool sandboxed) {
      PermissionSet pset;
      if (sandboxed) {
        pset = new PermissionSet(PermissionState.None);
        pset.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
      } else {
        pset = new PermissionSet(PermissionState.Unrestricted);
      }
      AppDomainSetup setup = new AppDomainSetup();
      setup.ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
      //Temp Fix!
      setup.PrivateBinPath = "plugins";
      return System.AppDomain.CreateDomain("appD", AppDomain.CurrentDomain.Evidence, setup, pset, CreateStrongName(Assembly.GetExecutingAssembly()));

    }

    private void DetermineAction(MessageContainer container) {
      switch (container.Message) {
        case MessageContainer.MessageType.AbortJob:
          engines[container.JobId].Abort();
          break;
        case MessageContainer.MessageType.JobAborted:
          Debug.WriteLine("-- Job Aborted Message received");
          break;

        case MessageContainer.MessageType.RequestSnapshot:
          engines[container.JobId].RequestSnapshot();
          break;
        case MessageContainer.MessageType.SnapshotReady:
          Thread ssr = new Thread(new ParameterizedThreadStart(GetSnapshot));
          ssr.Start(container.JobId);          
          break;

        case MessageContainer.MessageType.FetchJob: 
          clientCommunicator.PullJobAsync(Guid.NewGuid());
          break;          
        case MessageContainer.MessageType.FinishedJob:
          Thread finThread = new Thread(new ParameterizedThreadStart(GetFinishedJob));
          finThread.Start(container.JobId);          
          break;      
      }
    }

    private void GetFinishedJob(object jobId) {
      long jId = (long)jobId;
      byte[] sJob = engines[jId].GetFinishedJob();
      
      JobResult jobResult = new JobResult { JobId = jId, Result = sJob, Client = null };
      clientCommunicator.SendJobResultAsync(jobResult, true);

      AppDomain.Unload(appDomains[jId]);
      appDomains.Remove(jId);
      engines.Remove(jId);
      Status.CurrentJobs--;
      Debug.WriteLine("Decrement CurrentJobs to:" + Status.CurrentJobs.ToString());        

    }

    private void GetSnapshot(object jobId) {
      long jId = (long)jobId;
      byte[] obj = engines[jId].GetSnapshot();
    }

    void ClientCommunicator_PullJobCompleted(object sender, PullJobCompletedEventArgs e) {
      bool sandboxed = false;

      //IJob job = new TestJob { JobId = e.Result.JobId };

      PluginManager pm = PluginManager.Manager;
      AppDomain appDomain =  pm.CreateAndInitAppDomain("AppDomain");

      //AppDomain appDomain = CreateNewAppDomain(sandboxed);
      appDomains.Add(e.Result.JobId, appDomain);

      Executor engine = (Executor)appDomain.CreateInstanceAndUnwrap(typeof(Executor).Assembly.GetName().Name, typeof(Executor).FullName);
      engine.JobId = e.Result.JobId;
      engine.Queue = MessageQueue.GetInstance();
      engine.Start(e.Result.SerializedJob);
      engines.Add(e.Result.JobId, engine);

      Status.CurrentJobs++;

      Debug.WriteLine("Increment CurrentJobs to:"+Status.CurrentJobs.ToString());
    }

    void ClientCommunicator_SendJobResultCompleted(object sender, SendJobResultCompletedEventArgs e) {      
      // TODO Removing of the Engines & AppDomains should happen here, not in the GetFinishedJob Method.
    }
  }
}
