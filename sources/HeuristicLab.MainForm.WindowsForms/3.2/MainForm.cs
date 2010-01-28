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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using HeuristicLab.PluginInfrastructure;
using System.Collections;

namespace HeuristicLab.MainForm.WindowsForms {
  public partial class MainForm : Form, IMainForm {
    private bool initialized;

    protected MainForm()
      : base() {
      InitializeComponent();
      this.views = new Dictionary<IView, Form>();
      this.userInterfaceItems = new List<IUserInterfaceItem>();
      this.initialized = false;
    }

    protected MainForm(Type userInterfaceItemType)
      : this() {
      this.userInterfaceItemType = userInterfaceItemType;
    }

    #region properties
    public string Title {
      get { return this.Text; }
      set {
        if (InvokeRequired) {
          Action<string> action = delegate(string s) { this.Title = s; };
          Invoke(action, value);
        } else
          this.Text = value;
      }
    }

    public override Cursor Cursor {
      get { return base.Cursor; }
      set {
        if (InvokeRequired) {
          Action<Cursor> action = delegate(Cursor c) { this.Cursor = c; };
          Invoke(action, value);
        } else
          base.Cursor = value;
      }
    }

    private Type userInterfaceItemType;
    public Type UserInterfaceItemType {
      get { return this.userInterfaceItemType; }
    }

    private Dictionary<IView, Form> views;
    public IEnumerable<IView> Views {
      get { return views.Keys; }
    }

    private IView activeView;
    public IView ActiveView {
      get { return this.activeView; }
      protected set {
        if (this.activeView != value) {
          if (InvokeRequired) {
            Action<IView> action = delegate(IView activeView) { this.ActiveView = activeView; };
            Invoke(action, value);
          } else {
            this.activeView = value;
            OnActiveViewChanged();
          }
        }
      }
    }

    private List<IUserInterfaceItem> userInterfaceItems;
    protected IEnumerable<IUserInterfaceItem> UserInterfaceItems {
      get { return this.userInterfaceItems; }
    }
    #endregion

    #region events
    public event EventHandler ActiveViewChanged;
    protected virtual void OnActiveViewChanged() {
      if (InvokeRequired)
        Invoke((MethodInvoker)OnActiveViewChanged);
      else if (ActiveViewChanged != null)
        ActiveViewChanged(this, new EventArgs());
    }

    public event EventHandler<ViewEventArgs> ViewClosed;
    protected virtual void OnViewClosed(IView view) {
      if (InvokeRequired) Invoke((Action<IView>)OnViewClosed, view);
      else if (this.ViewClosed != null) {
        this.ViewClosed(this, new ViewEventArgs(view));
      }
    }

    public event EventHandler<ViewShownEventArgs> ViewShown;
    protected virtual void OnViewShown(IView view, bool firstTimeShown) {
      if (InvokeRequired) Invoke((Action<IView, bool>)OnViewShown, view, firstTimeShown);
      else if (this.ViewShown != null) {
        this.ViewShown(this, new ViewShownEventArgs(view, firstTimeShown));
      }
    }

    public event EventHandler<ViewEventArgs> ViewHidden;
    protected virtual void OnViewHidden(IView view) {
      if (InvokeRequired) Invoke((Action<IView>)OnViewHidden, view);
      else if (this.ViewHidden != null) {
        this.ViewHidden(this, new ViewEventArgs(view));
      }
    }

    public event EventHandler Changed;
    protected void FireMainFormChanged() {
      if (InvokeRequired)
        Invoke((MethodInvoker)FireMainFormChanged);
      else if (Changed != null)
        Changed(this, new EventArgs());
    }

    private void MainFormBase_Load(object sender, EventArgs e) {
      if (!DesignMode) {
        MainFormManager.RegisterMainForm(this);
        if (!this.initialized) {
          this.initialized = true;
          this.OnInitialized(new EventArgs());
        }
        this.CreateGUI();
      }
    }

    protected virtual void OnInitialized(EventArgs e) {
    }

    private void ChildFormClosed(object sender, FormClosedEventArgs e) {
      Form form = (Form)sender;
      IView view = GetView(form);

      form.Activated -= new EventHandler(FormActivated);
      form.FormClosed -= new FormClosedEventHandler(ChildFormClosed);

      views.Remove(view);
      this.OnViewClosed(view);
      if (ActiveView == view)
        ActiveView = null;
    }

    private void FormActivated(object sender, EventArgs e) {
      this.ActiveView = GetView((Form)sender);
    }
    #endregion

    #region create, get, show, hide, close views
    protected virtual Form CreateForm(IView view) {
      throw new NotImplementedException("CreateForm must be implemented in subclasses of MainFormBase.");
    }

    internal Form GetForm(IView view) {
      if (views.ContainsKey(view))
        return views[view];
      return null;
    }

    internal IView GetView(Form form) {
      return views.Where(x => x.Value == form).Single().Key;
    }

    internal void ShowView(IView view, bool firstTimeShown) {
      if (InvokeRequired) Invoke((Action<IView, bool>)ShowView, view, firstTimeShown);
      else {
        if (firstTimeShown) {
          Form form = CreateForm(view);
          this.views[view] = form;
          form.Activated += new EventHandler(FormActivated);
          form.FormClosed += new FormClosedEventHandler(ChildFormClosed);
        }
        this.Show(view, firstTimeShown);
        this.OnViewShown(view, firstTimeShown);
      }
    }

    protected virtual void Show(IView view, bool firstTimeShown) {
    }

    internal void HideView(IView view) {
      if (InvokeRequired) Invoke((Action<IView>)HideView, view);
      else {
        if (this.views.ContainsKey(view)) {
          this.Hide(view);
          this.OnViewHidden(view);
        }
      }
    }

    protected virtual void Hide(IView view) {
    }

    internal void CloseView(IView view) {
      if (InvokeRequired) Invoke((Action<IView>)CloseView, view);
      else {
        if (this.views.ContainsKey(view)) {
          this.views[view].Close();
          this.OnViewClosed(view);
        }
      }
    }

    internal void CloseView(IView view, CloseReason closeReason) {
      if (InvokeRequired) Invoke((Action<IView>)CloseView, view);
      else {
        if (this.views.ContainsKey(view)) {
          ((View)view).closeReason = closeReason;
          this.CloseView(view);
        }
      }
    }

    public void CloseAllViews() {
      foreach (IView view in views.Keys.ToArray())
        CloseView(view);
    }

    public void CloseAllViews(CloseReason closeReason) {
      foreach (IView view in views.Keys.ToArray())
        CloseView(view, closeReason);
    }
    #endregion

    #region create menu and toolbar
    private void CreateGUI() {
      IEnumerable<object> allUserInterfaceItems = ApplicationManager.Manager.GetInstances(userInterfaceItemType);

      IEnumerable<IPositionableUserInterfaceItem> toolStripMenuItems =
        from mi in allUserInterfaceItems
        where (mi is IPositionableUserInterfaceItem) &&
              (mi is IMenuItem || mi is IMenuSeparatorItem)
        orderby ((IPositionableUserInterfaceItem)mi).Position
        select (IPositionableUserInterfaceItem)mi;

      foreach (IPositionableUserInterfaceItem menuItem in toolStripMenuItems) {
        if (menuItem is IMenuItem)
          AddToolStripMenuItem((IMenuItem)menuItem);
        else if (menuItem is IMenuSeparatorItem)
          AddToolStripMenuItem((IMenuSeparatorItem)menuItem);
      }

      IEnumerable<IPositionableUserInterfaceItem> toolStripButtonItems =
        from bi in allUserInterfaceItems
        where (bi is IPositionableUserInterfaceItem) &&
              (bi is IToolBarItem || bi is IToolBarSeparatorItem)
        orderby ((IPositionableUserInterfaceItem)bi).Position
        select (IPositionableUserInterfaceItem)bi;

      foreach (IPositionableUserInterfaceItem toolStripButtonItem in toolStripButtonItems) {
        if (toolStripButtonItem is IToolBarItem)
          AddToolStripButtonItem((IToolBarItem)toolStripButtonItem);
        else if (toolStripButtonItem is IToolBarSeparatorItem)
          AddToolStripButtonItem((IToolBarSeparatorItem)toolStripButtonItem);
      }

      this.AdditionalCreationOfGUIElements();
    }

    protected virtual void AdditionalCreationOfGUIElements() {
    }

    private void AddToolStripMenuItem(IMenuItem menuItem) {
      ToolStripMenuItem item = new ToolStripMenuItem();
      this.SetToolStripItemProperties(item, menuItem);
      if (menuItem is MenuItem) {
        MenuItem menuItemBase = (MenuItem)menuItem;
        menuItemBase.ToolStripItem = item;
        item.ShortcutKeys = menuItemBase.ShortCutKeys;
        item.DisplayStyle = menuItemBase.ToolStripItemDisplayStyle;
      }
      this.InsertItem(menuItem.Structure, typeof(ToolStripMenuItem), item, menuStrip.Items);
    }

    private void AddToolStripMenuItem(IMenuSeparatorItem menuItem) {
      this.InsertItem(menuItem.Structure, typeof(ToolStripMenuItem), new ToolStripSeparator(), menuStrip.Items);
    }

    private void AddToolStripButtonItem(IToolBarItem buttonItem) {
      ToolStripItem item = new ToolStripButton();
      if (buttonItem is ToolBarItem) {
        ToolBarItem buttonItemBase = (ToolBarItem)buttonItem;
        if (buttonItemBase.IsDropDownButton)
          item = new ToolStripDropDownButton();

        item.DisplayStyle = buttonItemBase.ToolStripItemDisplayStyle;
        buttonItemBase.ToolStripItem = item;
      }

      this.SetToolStripItemProperties(item, buttonItem);
      this.InsertItem(buttonItem.Structure, typeof(ToolStripDropDownButton), item, toolStrip.Items);
    }

    private void AddToolStripButtonItem(IToolBarSeparatorItem buttonItem) {
      this.InsertItem(buttonItem.Structure, typeof(ToolStripDropDownButton), new ToolStripSeparator(), toolStrip.Items);
    }

    private void InsertItem(IEnumerable<string> structure, Type t, ToolStripItem item, ToolStripItemCollection parentItems) {
      ToolStripDropDownItem parent = null;
      foreach (string s in structure) {
        if (parentItems.ContainsKey(s))
          parent = (ToolStripDropDownItem)parentItems[s];
        else {
          parent = (ToolStripDropDownItem)Activator.CreateInstance(t, s, null, null, s); ;
          parentItems.Add(parent);
        }
        parentItems = parent.DropDownItems;
      }
      parentItems.Add(item);
    }

    private void SetToolStripItemProperties(ToolStripItem toolStripItem, IActionUserInterfaceItem userInterfaceItem) {
      toolStripItem.Name = userInterfaceItem.Name;
      toolStripItem.Text = userInterfaceItem.Name;
      toolStripItem.ToolTipText = userInterfaceItem.ToolTipText;
      toolStripItem.Tag = userInterfaceItem;
      toolStripItem.Image = userInterfaceItem.Image;
      toolStripItem.Click += new EventHandler(ToolStripItemClicked);
      this.userInterfaceItems.Add(userInterfaceItem);
    }

    private void ToolStripItemClicked(object sender, EventArgs e) {
      System.Windows.Forms.ToolStripItem item = (System.Windows.Forms.ToolStripItem)sender;
      ((IActionUserInterfaceItem)item.Tag).Execute();
    }
    #endregion
  }
}
