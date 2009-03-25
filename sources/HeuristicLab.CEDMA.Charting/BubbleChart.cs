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
using System.Drawing;
using System.Linq;
using HeuristicLab.Charting;
using System.Windows.Forms;
using HeuristicLab.CEDMA.Core;
using HeuristicLab.PluginInfrastructure;
using HeuristicLab.Core;
using HeuristicLab.CEDMA.DB.Interfaces;

namespace HeuristicLab.CEDMA.Charting {
  public class BubbleChart : Chart {
    private const string X_JITTER = "__X_JITTER";
    private const string Y_JITTER = "__Y_JITTER";
    private const double maxXJitterPercent = 0.05;
    private const double maxYJitterPercent = 0.05;
    private const int minBubbleSize = 5;
    private const int maxBubbleSize = 30;
    private const int maxAlpha = 255;
    private const int minAlpha = 50;
    private static readonly Color defaultColor = Color.Blue;
    private static readonly Color selectionColor = Color.Red;

    private double xJitterFactor = 0.0;
    private double yJitterFactor = 0.0;
    private string xDimension;
    private string yDimension;
    private string sizeDimension;
    private bool invertSize;
    private double minX = double.PositiveInfinity;
    private double minY = double.PositiveInfinity;
    private double maxX = double.NegativeInfinity;
    private double maxY = double.NegativeInfinity;
    private List<ResultsEntry> records;
    private Results results;
    private Dictionary<IPrimitive, ResultsEntry> primitiveToEntryDictionary;
    private Dictionary<ResultsEntry, IPrimitive> entryToPrimitiveDictionary;
    private Random random = new Random();
    private Group points;

    public BubbleChart(Results results, PointD lowerLeft, PointD upperRight)
      : base(lowerLeft, upperRight) {
      records = new List<ResultsEntry>();
      primitiveToEntryDictionary = new Dictionary<IPrimitive, ResultsEntry>();
      entryToPrimitiveDictionary = new Dictionary<ResultsEntry, IPrimitive>();
      this.results = results;

      foreach (var resultsEntry in results.GetEntries()) {
        if (resultsEntry.Get(X_JITTER) == null)
          resultsEntry.Set(X_JITTER, random.NextDouble() * 2.0 - 1.0);
        if (resultsEntry.Get(Y_JITTER) == null)
          resultsEntry.Set(Y_JITTER, random.NextDouble() * 2.0 - 1.0);
        records.Add(resultsEntry);
      }

      results.Changed += new EventHandler(results_Changed);
    }

    void results_Changed(object sender, EventArgs e) {
      Repaint();
      EnforceUpdate();
    }

    public BubbleChart(Results results, double x1, double y1, double x2, double y2)
      : this(results, new PointD(x1, y1), new PointD(x2, y2)) {
    }

    public void SetBubbleSizeDimension(string dimension, bool inverted) {
      this.sizeDimension = dimension;
      this.invertSize = inverted;
      Repaint();
      EnforceUpdate();
    }

    public void ShowXvsY(string xDimension, string yDimension) {
      if (this.xDimension != xDimension || this.yDimension != yDimension) {
        this.xDimension = xDimension;
        this.yDimension = yDimension;

        ResetViewSize();
        Repaint();
        ZoomToViewSize();
      }
    }

    internal void SetJitter(double xJitterFactor, double yJitterFactor) {
      this.xJitterFactor = xJitterFactor * maxXJitterPercent * Size.Width;
      this.yJitterFactor = yJitterFactor * maxYJitterPercent * Size.Height;
      Repaint();
      EnforceUpdate();
    }

    private void Repaint() {
      if (xDimension == null || yDimension == null) return;
      double maxSize = 1;
      double minSize = 1;
      if (sizeDimension != null && results.OrdinalVariables.Contains(sizeDimension)) {
        var sizes = records
          .Select(x => Convert.ToDouble(x.Get(sizeDimension)))
          .Where(size => !double.IsInfinity(size) && size != double.MaxValue && size != double.MinValue)
          .OrderBy(r => r);
        minSize = sizes.ElementAt((int)(sizes.Count() * 0.1));
        maxSize = sizes.ElementAt((int)(sizes.Count() * 0.9));
      } else {
        minSize = 1;
        maxSize = 1;
      }
      UpdateEnabled = false;
      Group.Clear();
      primitiveToEntryDictionary.Clear();
      entryToPrimitiveDictionary.Clear();
      points = new Group(this);
      Group.Add(new Axis(this, 0, 0, AxisType.Both));
      UpdateViewSize(0, 0, 5);
      foreach (ResultsEntry r in records) {
        double x, y;
        int size;
        if (results.OrdinalVariables.Contains(xDimension)) {
          x = Convert.ToDouble(r.Get(xDimension)) + (double)r.Get(X_JITTER) * xJitterFactor;
        } else if (results.CategoricalVariables.Contains(xDimension)) {
          x = results.IndexOfCategoricalValue(xDimension, r.Get(xDimension)) + (double)r.Get(X_JITTER) * xJitterFactor;
        } else {
          x = double.NaN;
        }
        if (results.OrdinalVariables.Contains(yDimension)) {
          y = Convert.ToDouble(r.Get(yDimension)) + (double)r.Get(Y_JITTER) * yJitterFactor;
        } else if (results.CategoricalVariables.Contains(yDimension)) {
          y = results.IndexOfCategoricalValue(yDimension, r.Get(yDimension)) + (double)r.Get(Y_JITTER) * yJitterFactor;
        } else {
          y = double.NaN;
        }
        size = CalculateSize(Convert.ToDouble(r.Get(sizeDimension)), minSize, maxSize);

        if (double.IsInfinity(x) || x == double.MaxValue || x == double.MinValue) x = double.NaN;
        if (double.IsInfinity(y) || y == double.MaxValue || y == double.MinValue) y = double.NaN;
        if (!double.IsNaN(x) && !double.IsNaN(y)) {
          UpdateViewSize(x, y, size);
          int alpha = CalculateAlpha(size);
          Pen pen = new Pen(Color.FromArgb(alpha, r.Selected ? selectionColor : defaultColor));
          Brush brush = pen.Brush;
          FixedSizeCircle c = new FixedSizeCircle(this, x, y, size, pen, brush);
          c.ToolTipText = r.GetToolTipText();
          points.Add(c);
          if (!r.Selected) c.IntoBackground();
          primitiveToEntryDictionary[c] = r;
          entryToPrimitiveDictionary[r] = c;
        }
      }
      Group.Add(points);
      UpdateEnabled = true;
    }

    private int CalculateSize(double size, double minSize, double maxSize) {
      if (double.IsNaN(size) || double.IsInfinity(size) || size == double.MaxValue || size == double.MinValue) return minBubbleSize;
      if (size > maxSize) size = maxSize;
      if (size < minSize) size = minSize;
      if (Math.Abs(maxSize - minSize) < 1.0E-10) return minBubbleSize;
      double sizeDifference = ((size - minSize) / (maxSize - minSize) * (maxBubbleSize - minBubbleSize));
      if (invertSize) return maxBubbleSize - (int)sizeDifference;
      else return minBubbleSize + (int)sizeDifference;
    }

    private int CalculateAlpha(int size) {
      return maxAlpha - (int)((double)(size - minBubbleSize) / (double)(maxBubbleSize - minBubbleSize) * (double)(maxAlpha - minAlpha));
    }

    private void ZoomToViewSize() {
      if (minX < maxX && minY < maxY) {
        // enlarge view by 5% on each side
        double width = maxX - minX;
        double height = maxY - minY;
        minX = minX - width * 0.05;
        maxX = maxX + width * 0.05;
        minY = minY - height * 0.05;
        maxY = maxY + height * 0.05;
        ZoomIn(minX, minY, maxX, maxY);
      }
    }

    private void UpdateViewSize(double x, double y, double size) {
      if (x - size < minX) minX = x - size;
      if (x + size > maxX) maxX = x + size;
      if (y - size < minY) minY = y + size;
      if (y + size > maxY) maxY = y + size;
    }

    private void ResetViewSize() {
      minX = double.PositiveInfinity;
      maxX = double.NegativeInfinity;
      minY = double.PositiveInfinity;
      maxY = double.NegativeInfinity;
    }

    internal ResultsEntry GetResultsEntry(Point point) {
      ResultsEntry r = null;
      IPrimitive p = points.GetPrimitive(TransformPixelToWorld(point));
      if (p != null) {
        primitiveToEntryDictionary.TryGetValue(p, out r);
      }
      return r;
    }

    public override void MouseDrag(Point start, Point end, MouseButtons button) {
      if (button == MouseButtons.Left && Mode == ChartMode.Select) {
        PointD a = TransformPixelToWorld(start);
        PointD b = TransformPixelToWorld(end);
        double minX = Math.Min(a.X, b.X);
        double minY = Math.Min(a.Y, b.Y);
        double maxX = Math.Max(a.X, b.X);
        double maxY = Math.Max(a.Y, b.Y);
        HeuristicLab.Charting.Rectangle rect = new HeuristicLab.Charting.Rectangle(this, minX, minY, maxX, maxY);

        List<IPrimitive> primitives = new List<IPrimitive>();
        primitives.AddRange(points.Primitives);

        foreach (FixedSizeCircle p in primitives) {
          if (rect.ContainsPoint(p.Point)) {
            ResultsEntry r;
            primitiveToEntryDictionary.TryGetValue(p, out r);
            if (r != null) r.ToggleSelected();
          }
        }
        if (primitives.Count() > 0) results.FireChanged();
      } else {
        base.MouseDrag(start, end, button);
      }
    }

    public override void MouseClick(Point point, MouseButtons button) {
      if (button == MouseButtons.Left) {
        ResultsEntry r = GetResultsEntry(point);
        if (r != null) {
          r.ToggleSelected();
          results.FireChanged();
        }
      } else {
        base.MouseClick(point, button);
      }
    }

    public override void MouseDoubleClick(Point point, MouseButtons button) {
      if (button == MouseButtons.Left) {
        ResultsEntry entry = GetResultsEntry(point);
        if (entry != null) {
          string serializedData = (string)entry.Get(Ontology.PredicateSerializedData.Uri.Replace(Ontology.CedmaNameSpace, ""));
          var model = (IItem)PersistenceManager.RestoreFromGZip(Convert.FromBase64String(serializedData));
          PluginManager.ControlManager.ShowControl(model.CreateView());
        }
      } else {
        base.MouseDoubleClick(point, button);
      }
    }
  }
}
