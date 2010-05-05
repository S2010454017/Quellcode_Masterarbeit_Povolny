﻿#region License Information
/* HeuristicLab
 * Copyright (C) 2002-2010 Heuristic and Evolutionary Algorithms Laboratory (HEAL)
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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HeuristicLab.MainForm.WindowsForms;
using HeuristicLab.MainForm;
using System.Windows.Forms.DataVisualization.Charting;
using HeuristicLab.Common;
using HeuristicLab.Core;
using HeuristicLab.Data;
using System.Threading;

namespace HeuristicLab.Optimization.Views {
  [View("RunCollection BubbleChart")]
  [Content(typeof(RunCollection), false)]
  public partial class RunCollectionBubbleChartView : AsynchronousContentView {
    private enum SizeDimension { Constant = 0 }
    private enum AxisDimension { Index = 0 }

    private Dictionary<int, Dictionary<object, double>> categoricalMapping;
    private Dictionary<IRun, double> xJitter;
    private Dictionary<IRun, double> yJitter;
    private double xJitterFactor = 0.0;
    private double yJitterFactor = 0.0;
    private Random random;
    private bool isSelecting = false;

    public RunCollectionBubbleChartView() {
      InitializeComponent();
      Caption = "Run Collection Bubble Chart";

      this.categoricalMapping = new Dictionary<int, Dictionary<object, double>>();
      this.xJitter = new Dictionary<IRun, double>();
      this.yJitter = new Dictionary<IRun, double>();
      this.random = new Random();
      this.colorDialog.Color = Color.Black;
      this.colorButton.Image = this.GenerateImage(16, 16, this.colorDialog.Color);
      this.isSelecting = false;

      this.chart.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
      this.chart.ChartAreas[0].CursorY.IsUserSelectionEnabled = true;
      this.chart.ChartAreas[0].CursorX.Interval = 0;
      this.chart.ChartAreas[0].CursorY.Interval = 0;
      this.chart.ChartAreas[0].AxisX.ScaleView.Zoomable = !this.isSelecting;
      this.chart.ChartAreas[0].AxisY.ScaleView.Zoomable = !this.isSelecting;
    }

    public new RunCollection Content {
      get { return (RunCollection)base.Content; }
      set { base.Content = value; }
    }

    public IStringConvertibleMatrix Matrix {
      get { return this.Content; }
    }

    protected override void RegisterContentEvents() {
      base.RegisterContentEvents();
      Content.Reset += new EventHandler(Content_Reset);
      Content.ColumnNamesChanged += new EventHandler(Content_ColumnNamesChanged);
      Content.ItemsAdded += new HeuristicLab.Collections.CollectionItemsChangedEventHandler<IRun>(Content_ItemsAdded);
      Content.ItemsRemoved += new HeuristicLab.Collections.CollectionItemsChangedEventHandler<IRun>(Content_ItemsRemoved);
      Content.CollectionReset += new HeuristicLab.Collections.CollectionItemsChangedEventHandler<IRun>(Content_CollectionReset);
      RegisterRunEvents(Content);
    }
    protected virtual void RegisterRunEvents(IEnumerable<IRun> runs) {
      foreach (IRun run in runs)
        run.Changed += new EventHandler(run_Changed);
    }
    protected override void DeregisterContentEvents() {
      base.DeregisterContentEvents();
      Content.Reset -= new EventHandler(Content_Reset);
      Content.ColumnNamesChanged -= new EventHandler(Content_ColumnNamesChanged);
      Content.ItemsAdded -= new HeuristicLab.Collections.CollectionItemsChangedEventHandler<IRun>(Content_ItemsAdded);
      Content.ItemsRemoved -= new HeuristicLab.Collections.CollectionItemsChangedEventHandler<IRun>(Content_ItemsRemoved);
      Content.CollectionReset -= new HeuristicLab.Collections.CollectionItemsChangedEventHandler<IRun>(Content_CollectionReset);
      DeregisterRunEvents(Content);
    }
    protected virtual void DeregisterRunEvents(IEnumerable<IRun> runs) {
      foreach (IRun run in runs)
        run.Changed -= new EventHandler(run_Changed);
    }

    private void Content_CollectionReset(object sender, HeuristicLab.Collections.CollectionItemsChangedEventArgs<IRun> e) {
      DeregisterRunEvents(e.OldItems);
      RegisterRunEvents(e.Items);
    }
    private void Content_ItemsRemoved(object sender, HeuristicLab.Collections.CollectionItemsChangedEventArgs<IRun> e) {
      DeregisterRunEvents(e.Items);
    }
    private void Content_ItemsAdded(object sender, HeuristicLab.Collections.CollectionItemsChangedEventArgs<IRun> e) {
      RegisterRunEvents(e.Items);
    }
    private void run_Changed(object sender, EventArgs e) {
      if (InvokeRequired)
        this.Invoke(new EventHandler(run_Changed), sender, e);
      else {
        IRun run = (IRun)sender;
        UpdateRun(run);
      }
    }

    private void UpdateRun(IRun run) {
      DataPoint point = this.chart.Series[0].Points.Where(p => p.Tag == run).SingleOrDefault();
      if (point != null) {
        point.Color = run.Color;
        if (!run.Visible)
          this.chart.Series[0].Points.Remove(point);
      } else
        AddDataPoint(run);

      if (this.chart.Series[0].Points.Count == 0)
        noRunsLabel.Visible = true;
      else
        noRunsLabel.Visible = false;
    }

    protected override void OnContentChanged() {
      base.OnContentChanged();
      this.categoricalMapping.Clear();
      UpdateComboBoxes();
      UpdateDataPoints();
      foreach (IRun run in Content)
        UpdateRun(run);
    }
    private void Content_ColumnNamesChanged(object sender, EventArgs e) {
      if (InvokeRequired)
        Invoke(new EventHandler(Content_ColumnNamesChanged), sender, e);
      else
        UpdateComboBoxes();
    }

    private void UpdateComboBoxes() {
      this.xAxisComboBox.Items.Clear();
      this.yAxisComboBox.Items.Clear();
      this.sizeComboBox.Items.Clear();
      if (Content != null) {
        string[] additionalAxisDimension = Enum.GetNames(typeof(AxisDimension));
        this.xAxisComboBox.Items.AddRange(additionalAxisDimension);
        this.xAxisComboBox.Items.AddRange(Matrix.ColumnNames.ToArray());
        this.yAxisComboBox.Items.AddRange(additionalAxisDimension);
        this.yAxisComboBox.Items.AddRange(Matrix.ColumnNames.ToArray());
        string[] additionalSizeDimension = Enum.GetNames(typeof(SizeDimension));
        this.sizeComboBox.Items.AddRange(additionalSizeDimension);
        this.sizeComboBox.Items.AddRange(Matrix.ColumnNames.ToArray());
        this.sizeComboBox.SelectedItem = SizeDimension.Constant.ToString();
      }
    }

    private void Content_Reset(object sender, EventArgs e) {
      if (InvokeRequired)
        Invoke(new EventHandler(Content_Reset), sender, e);
      else {
        this.categoricalMapping.Clear();
        UpdateDataPoints();
      }
    }

    private void UpdateDataPoints() {
      Series series = this.chart.Series[0];
      series.Points.Clear();
      if (Content != null) {
        foreach (IRun run in this.Content)
          this.AddDataPoint(run);

        //check to correct max bubble size
        if (this.chart.Series[0].Points.Select(p => p.YValues[1]).Distinct().Count() == 1)
          this.chart.Series[0]["BubbleMaxSize"] = "2";
        else
          this.chart.Series[0]["BubbleMaxSize"] = "7";

        if (this.chart.Series[0].Points.Count == 0)
          noRunsLabel.Visible = true;
        else
          noRunsLabel.Visible = false;
      }
    }
    private void AddDataPoint(IRun run) {
      double? xValue;
      double? yValue;
      double? sizeValue;
      Series series = this.chart.Series[0];
      int row = this.Content.ToList().IndexOf(run);
      xValue = GetValue(run, (string)xAxisComboBox.SelectedItem);
      yValue = GetValue(run, (string)yAxisComboBox.SelectedItem);
      sizeValue = GetValue(run, (string)sizeComboBox.SelectedItem);
      if (xValue.HasValue && yValue.HasValue && sizeValue.HasValue) {
        xValue = xValue.Value + xValue.Value * GetXJitter(run) * xJitterFactor;
        yValue = yValue.Value + yValue.Value * GetYJitter(run) * yJitterFactor;
        if (run.Visible) {
          DataPoint point = new DataPoint(xValue.Value, new double[] { yValue.Value, sizeValue.Value });
          point.Tag = run;
          point.Color = run.Color;
          series.Points.Add(point);
        }
      }
    }
    private double? GetValue(IRun run, string columnName) {
      if (run == null || string.IsNullOrEmpty(columnName))
        return null;

      if (Enum.IsDefined(typeof(AxisDimension), columnName)) {
        AxisDimension axisDimension = (AxisDimension)Enum.Parse(typeof(AxisDimension), columnName);
        return GetValue(run, axisDimension);
      } else if (Enum.IsDefined(typeof(SizeDimension), columnName)) {
        SizeDimension sizeDimension = (SizeDimension)Enum.Parse(typeof(SizeDimension), columnName);
        return GetValue(run, sizeDimension);
      } else {
        int columnIndex = Matrix.ColumnNames.ToList().IndexOf(columnName);
        IItem value = Content.GetValue(run, columnIndex);
        if (value == null)
          return null;

        DoubleValue doubleValue = value as DoubleValue;
        IntValue intValue = value as IntValue;
        double? ret = null;
        if (doubleValue != null) {
          if (!double.IsNaN(doubleValue.Value) && !double.IsInfinity(doubleValue.Value))
            ret = doubleValue.Value;
        } else if (intValue != null)
          ret = intValue.Value;
        else
          ret = GetCategoricalValue(columnIndex, value.ToString());

        return ret;
      }
    }
    private double GetCategoricalValue(int dimension, string value) {
      if (!this.categoricalMapping.ContainsKey(dimension))
        this.categoricalMapping[dimension] = new Dictionary<object, double>();
      if (!this.categoricalMapping[dimension].ContainsKey(value)) {
        if (this.categoricalMapping[dimension].Values.Count == 0)
          this.categoricalMapping[dimension][value] = 1.0;
        else
          this.categoricalMapping[dimension][value] = this.categoricalMapping[dimension].Values.Max() + 1.0;
      }
      return this.categoricalMapping[dimension][value];
    }
    private double GetValue(IRun run, AxisDimension axisDimension) {
      double value = double.NaN;
      switch (axisDimension) {
        case AxisDimension.Index: {
            value = Content.ToList().IndexOf(run);
            break;
          }
        default: {
            throw new ArgumentException("No handling strategy for " + axisDimension.ToString() + " is defined.");
          }
      }
      return value;
    }
    private double GetValue(IRun run, SizeDimension sizeDimension) {
      double value = double.NaN;
      switch (sizeDimension) {
        case SizeDimension.Constant: {
            value = 2;
            break;
          }
        default: {
            throw new ArgumentException("No handling strategy for " + sizeDimension.ToString() + " is defined.");
          }
      }
      return value;
    }

    #region drag and drop and tooltip
    private IRun draggedRun;
    private void chart_MouseDown(object sender, MouseEventArgs e) {
      HitTestResult h = this.chart.HitTest(e.X, e.Y);
      if (h.ChartElementType == ChartElementType.DataPoint) {
        IRun run = (IRun)((DataPoint)h.Object).Tag;
        if (e.Clicks >= 2) {
          IContentView view = MainFormManager.MainForm.ShowContent(run);
          if (view != null) {
            view.ReadOnly = this.ReadOnly;
            view.Locked = this.Locked;
          }
        } else
          this.draggedRun = run;
        this.chart.ChartAreas[0].CursorX.SetSelectionPosition(double.NaN, double.NaN);
        this.chart.ChartAreas[0].CursorY.SetSelectionPosition(double.NaN, double.NaN);
      }
    }

    private void chart_MouseUp(object sender, MouseEventArgs e) {
      if (isSelecting) {
        System.Windows.Forms.DataVisualization.Charting.Cursor xCursor = chart.ChartAreas[0].CursorX;
        System.Windows.Forms.DataVisualization.Charting.Cursor yCursor = chart.ChartAreas[0].CursorY;

        double minX = Math.Min(xCursor.SelectionStart, xCursor.SelectionEnd);
        double maxX = Math.Max(xCursor.SelectionStart, xCursor.SelectionEnd);
        double minY = Math.Min(yCursor.SelectionStart, yCursor.SelectionEnd);
        double maxY = Math.Max(yCursor.SelectionStart, yCursor.SelectionEnd);

        //check for click to select model
        if (minX == maxX && minY == maxY) {
          HitTestResult hitTest = chart.HitTest(e.X, e.Y);
          if (hitTest.ChartElementType == ChartElementType.DataPoint) {
            int pointIndex = hitTest.PointIndex;
            IRun run = (IRun)this.chart.Series[0].Points[pointIndex].Tag;
            run.Color = colorDialog.Color;
          }
        } else {
          List<DataPoint> selectedPoints = new List<DataPoint>();
          foreach (DataPoint p in this.chart.Series[0].Points) {
            if (p.XValue >= minX && p.XValue < maxX &&
              p.YValues[0] >= minY && p.YValues[0] < maxY) {
              selectedPoints.Add(p);
            }
          }
          foreach (DataPoint p in selectedPoints) {
            IRun run = (IRun)p.Tag;
            run.Color = colorDialog.Color;
          }
        }
        this.chart.ChartAreas[0].CursorX.SelectionStart = this.chart.ChartAreas[0].CursorX.SelectionEnd;
        this.chart.ChartAreas[0].CursorY.SelectionStart = this.chart.ChartAreas[0].CursorY.SelectionEnd;
      }
    }

    private void chart_MouseMove(object sender, MouseEventArgs e) {
      HitTestResult h = this.chart.HitTest(e.X, e.Y);
      if (!Locked) {
        if (this.draggedRun != null && h.ChartElementType != ChartElementType.DataPoint) {
          DataObject data = new DataObject();
          data.SetData("Type", draggedRun.GetType());
          data.SetData("Value", draggedRun);
          if (ReadOnly)
            DoDragDrop(data, DragDropEffects.Copy | DragDropEffects.Link);
          else {
            DragDropEffects result = DoDragDrop(data, DragDropEffects.Copy | DragDropEffects.Link | DragDropEffects.Move);
            if ((result & DragDropEffects.Move) == DragDropEffects.Move)
              Content.Remove(draggedRun);
          }
          this.chart.ChartAreas[0].AxisX.ScaleView.Zoomable = !isSelecting;
          this.chart.ChartAreas[0].AxisY.ScaleView.Zoomable = !isSelecting;
          this.draggedRun = null;
        }
      }
      string newTooltipText = string.Empty;
      string oldTooltipText;
      if (h.ChartElementType == ChartElementType.DataPoint) {
        IRun run = (IRun)((DataPoint)h.Object).Tag;
        newTooltipText = BuildTooltip(run);
      }

      oldTooltipText = this.tooltip.GetToolTip(chart);
      if (newTooltipText != oldTooltipText)
        this.tooltip.SetToolTip(chart, newTooltipText);
    }

    private string BuildTooltip(IRun run) {
      string tooltip;
      tooltip = run.Name + System.Environment.NewLine;

      double? xValue = this.GetValue(run, (string)xAxisComboBox.SelectedItem);
      double? yValue = this.GetValue(run, (string)yAxisComboBox.SelectedItem);
      double? sizeValue = this.GetValue(run, (string)sizeComboBox.SelectedItem);

      string xString = xValue == null ? string.Empty : xValue.Value.ToString();
      string yString = yValue == null ? string.Empty : yValue.Value.ToString();
      string sizeString = sizeValue == null ? string.Empty : sizeValue.Value.ToString();

      tooltip += xAxisComboBox.SelectedItem + " : " + xString + Environment.NewLine;
      tooltip += yAxisComboBox.SelectedItem + " : " + yString + Environment.NewLine;
      tooltip += sizeComboBox.SelectedItem + " : " + sizeString + Environment.NewLine;

      return tooltip;
    }
    #endregion

    #region GUI events and updating
    private double GetXJitter(IRun run) {
      if (!this.xJitter.ContainsKey(run))
        this.xJitter[run] = random.NextDouble() * 2.0 - 1.0;
      return this.xJitter[run];
    }
    private double GetYJitter(IRun run) {
      if (!this.yJitter.ContainsKey(run))
        this.yJitter[run] = random.NextDouble() * 2.0 - 1.0;
      return this.yJitter[run];
    }
    private void jitterTrackBar_ValueChanged(object sender, EventArgs e) {
      this.xJitterFactor = xTrackBar.Value / 100.0;
      this.yJitterFactor = yTrackBar.Value / 100.0;
      this.UpdateDataPoints();
    }

    private void AxisComboBox_SelectedIndexChanged(object sender, EventArgs e) {
      UpdateDataPoints();
      UpdateAxisLabels();
    }
    private void UpdateAxisLabels() {
      Axis xAxis = this.chart.ChartAreas[0].AxisX;
      Axis yAxis = this.chart.ChartAreas[0].AxisY;
      int axisDimensionCount = Enum.GetNames(typeof(AxisDimension)).Count();
      SetCustomAxisLabels(xAxis, xAxisComboBox.SelectedIndex - axisDimensionCount);
      SetCustomAxisLabels(yAxis, yAxisComboBox.SelectedIndex - axisDimensionCount);
    }
    private void SetCustomAxisLabels(Axis axis, int dimension) {
      axis.CustomLabels.Clear();
      if (categoricalMapping.ContainsKey(dimension)) {
        CustomLabel label = null;
        foreach (var pair in categoricalMapping[dimension]) {
          string labelText = pair.Key.ToString();
          if (labelText.Length > 25)
            labelText = labelText.Substring(0, 25) + " ... ";
          label = axis.CustomLabels.Add(pair.Value - 0.5, pair.Value + 0.5, labelText);
          label.GridTicks = GridTickTypes.TickMark;
        }
        axis.IsLabelAutoFit = false;
        axis.LabelStyle.Enabled = true;
        axis.LabelStyle.Angle = 0;
        axis.LabelStyle.TruncatedLabels = true;
      }
    }

    private void zoomButton_CheckedChanged(object sender, EventArgs e) {
      this.isSelecting = selectButton.Checked;
      this.colorButton.Enabled = this.isSelecting;
      this.chart.ChartAreas[0].AxisX.ScaleView.Zoomable = !isSelecting;
      this.chart.ChartAreas[0].AxisY.ScaleView.Zoomable = !isSelecting;
    }
    private void colorButton_Click(object sender, EventArgs e) {
      if (colorDialog.ShowDialog(this) == DialogResult.OK) {
        this.colorButton.Image = this.GenerateImage(16, 16, this.colorDialog.Color);
      }
    }
    private Image GenerateImage(int width, int height, Color fillColor) {
      Image colorImage = new Bitmap(width, height);
      using (Graphics gfx = Graphics.FromImage(colorImage)) {
        using (SolidBrush brush = new SolidBrush(fillColor)) {
          gfx.FillRectangle(brush, 0, 0, width, height);
        }
      }
      return colorImage;
    }
    #endregion
  }
}
