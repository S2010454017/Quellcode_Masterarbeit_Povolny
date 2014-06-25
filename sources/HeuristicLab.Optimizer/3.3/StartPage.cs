#region License Information
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using HeuristicLab.Common;
using HeuristicLab.Core;
using HeuristicLab.MainForm;
using HeuristicLab.Persistence.Default.Xml;

namespace HeuristicLab.Optimizer {
  [View("Start Page")]
  public partial class StartPage : HeuristicLab.MainForm.WindowsForms.View {
    private const string StandardProblemsGroupName = "Standard Problems";
    private const string DataAnalysisGroupName = "Data Analysis";
    private const string ScriptsGroupName = "Scripts";
    private const string UncategorizedGroupName = "Uncategorized";
    private const string SampleNamePrefix = "HeuristicLab.Optimizer.Documents.";
    private const string SampleNameSuffix = ".hl";

    private readonly Dictionary<ListViewGroup, List<string>> GroupLookup = new Dictionary<ListViewGroup, List<string>>();
    private readonly ListViewGroup StandardProblemsGroup = new ListViewGroup(StandardProblemsGroupName);
    private readonly ListViewGroup DataAnalysisGroup = new ListViewGroup(DataAnalysisGroupName);
    private readonly ListViewGroup ScriptsGroup = new ListViewGroup(ScriptsGroupName);
    private readonly ListViewGroup UncategorizedGroup = new ListViewGroup(UncategorizedGroupName);

    private IProgress progress;

    public StartPage() {
      InitializeComponent();
    }

    protected override void OnInitialized(EventArgs e) {
      base.OnInitialized(e);
      Assembly assembly = Assembly.GetExecutingAssembly();
      AssemblyFileVersionAttribute version = assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true).
                                             Cast<AssemblyFileVersionAttribute>().FirstOrDefault();
      titleLabel.Text = "HeuristicLab Optimizer";
      if (version != null) titleLabel.Text += " " + version.Version;

      try {
        using (Stream stream = assembly.GetManifestResourceStream(typeof(StartPage), "Documents.FirstSteps.rtf"))
          firstStepsRichTextBox.LoadFile(stream, RichTextBoxStreamType.RichText);
      }
      catch (Exception) { }

      samplesListView.Enabled = false;
      samplesListView.Groups.Add(StandardProblemsGroup);
      samplesListView.Groups.Add(DataAnalysisGroup);
      samplesListView.Groups.Add(ScriptsGroup);
      samplesListView.Groups.Add(UncategorizedGroup);
      FillGroupLookup();

      showStartPageCheckBox.Checked = Properties.Settings.Default.ShowStartPage;

      ThreadPool.QueueUserWorkItem(new WaitCallback(LoadSamples));
    }

    protected override void OnClosing(FormClosingEventArgs e) {
      base.OnClosing(e);
      if (e.CloseReason == CloseReason.UserClosing) {
        e.Cancel = true;
        this.Hide();
      }
    }

    private void LoadSamples(object state) {
      progress = MainFormManager.GetMainForm<HeuristicLab.MainForm.WindowsForms.MainForm>().AddOperationProgressToView(samplesListView, "Loading...");
      try {
        var assembly = Assembly.GetExecutingAssembly();
        var samples = assembly.GetManifestResourceNames().Where(x => x.EndsWith(SampleNameSuffix));
        int count = samples.Count();
        string path = Path.GetTempFileName();

        foreach (var entry in GroupLookup) {
          var group = entry.Key;
          var sampleList = entry.Value;
          foreach (var sampleName in sampleList) {
            string resourceName = SampleNamePrefix + sampleName + SampleNameSuffix;
            LoadSample(resourceName, assembly, path, group, count);
          }
        }

        var categorizedSamples = GroupLookup.Select(x => x.Value).SelectMany(x => x).Select(x => SampleNamePrefix + x + SampleNameSuffix);
        var uncategorizedSamples = samples.Except(categorizedSamples);

        foreach (var resourceName in uncategorizedSamples) {
          LoadSample(resourceName, assembly, path, UncategorizedGroup, count);
        }

        OnAllSamplesLoaded();
      }
      finally {
        MainFormManager.GetMainForm<HeuristicLab.MainForm.WindowsForms.MainForm>().RemoveOperationProgressFromView(samplesListView);
      }
    }

    private void LoadSample(string name, Assembly assembly, string path, ListViewGroup group, int count) {
      try {
        using (var stream = assembly.GetManifestResourceStream(name)) {
          WriteStreamToTempFile(stream, path);
          var item = XmlParser.Deserialize<INamedItem>(path);
          OnSampleLoaded(item, group, 1.0 / count);
        }
      }
      catch (Exception) { }
    }

    private void FillGroupLookup() {
      var standardProblems = new List<string>(
        new[] { "ES_Griewank", "GA_TSP", "GA_VRP", "GE_ArtificialAnt",
                "IslandGA_TSP", "LS_Knapsack", "PSO_Schwefel", "RAPGA_JSSP",
                "SA_Rastrigin", "SGP_SantaFe", "SS_VRP", "TS_TSP", "TS_VRP", "VNS_TSP"
        });
      GroupLookup[StandardProblemsGroup] = standardProblems;
      var dataAnalysisProblems = new List<string>(new[] { "GE_SymbReg", "GPR", "SGP_SymbClass", "SGP_SymbReg" });
      GroupLookup[DataAnalysisGroup] = dataAnalysisProblems;
      var scripts = new List<string>(new[] { "GA_QAP_Script", "GUI_Automation_Script", "OSGA_Rastrigin_Script" });
      GroupLookup[ScriptsGroup] = scripts;
    }

    private void OnSampleLoaded(INamedItem sample, ListViewGroup group, double progress) {
      if (InvokeRequired)
        Invoke(new Action<INamedItem, ListViewGroup, double>(OnSampleLoaded), sample, group, progress);
      else {
        ListViewItem item = new ListViewItem(new string[] { sample.Name, sample.Description }, group);
        item.ToolTipText = sample.ItemName + ": " + sample.ItemDescription;
        samplesListView.SmallImageList.Images.Add(sample.ItemImage);
        item.ImageIndex = samplesListView.SmallImageList.Images.Count - 1;
        item.Tag = sample;
        samplesListView.Items.Add(item);
        this.progress.ProgressValue += progress;
      }
    }
    private void OnAllSamplesLoaded() {
      if (InvokeRequired)
        Invoke(new Action(OnAllSamplesLoaded));
      else {
        samplesListView.Enabled = samplesListView.Items.Count > 0;
        if (samplesListView.Items.Count > 0) {
          for (int i = 0; i < samplesListView.Columns.Count; i++)
            samplesListView.Columns[i].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
        }
      }
    }

    private void firstStepsRichTextBox_LinkClicked(object sender, LinkClickedEventArgs e) {
      System.Diagnostics.Process.Start(e.LinkText);
    }

    private void samplesListView_DoubleClick(object sender, EventArgs e) {
      if (samplesListView.SelectedItems.Count == 1)
        MainFormManager.MainForm.ShowContent((IContent)((IItem)samplesListView.SelectedItems[0].Tag).Clone());
    }
    private void samplesListView_ItemDrag(object sender, ItemDragEventArgs e) {
      ListViewItem listViewItem = (ListViewItem)e.Item;
      IItem item = (IItem)listViewItem.Tag;
      DataObject data = new DataObject();
      data.SetData(HeuristicLab.Common.Constants.DragDropDataFormat, item);
      DragDropEffects result = DoDragDrop(data, DragDropEffects.Copy);
    }

    private void showStartPageCheckBox_CheckedChanged(object sender, EventArgs e) {
      Properties.Settings.Default.ShowStartPage = showStartPageCheckBox.Checked;
      Properties.Settings.Default.Save();
    }

    #region Helpers
    private void WriteStreamToTempFile(Stream stream, string path) {
      using (FileStream output = new FileStream(path, FileMode.Create, FileAccess.Write)) {
        int cnt = 0;
        byte[] buffer = new byte[32 * 1024];
        while ((cnt = stream.Read(buffer, 0, buffer.Length)) != 0)
          output.Write(buffer, 0, cnt);
      }
    }
    #endregion
  }
}