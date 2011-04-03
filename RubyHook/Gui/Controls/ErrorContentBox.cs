// Retoolkit - Scripting-based reverse engineering toolkit for Windows OS'es
// Copyright (C) 2010  James Leskovar
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Retoolkit.Gui.Controls
{
  public delegate void EventOnErrorSelected(ErrorListBoxItem error);

  public partial class ErrorContentBox : WeifenLuo.WinFormsUI.Docking.DockContent
  {
    public event EventOnErrorSelected ErrorSelected;

    public ErrorContentBox()
    {
      InitializeComponent();
    }

    public void ClearErrors()
    {
      this.Invoke(new Action(errorListBox.Items.Clear));
    }

    public void AddErrorMessage(int line, int column, string path, string message)
    {
      this.Invoke(new Action<int, int, string, string>(_AddErrorMessage), line, column, path, message);
    }

    void _AddErrorMessage(int line, int column, string path, string message)
    {
      var eitem = new ErrorListBoxItem { Line = line, Column = column, Message = message, Path = path };
      errorListBox.Items.Add(eitem);
      this.Show();
      errorListBox.Focus();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Control && e.KeyCode == Keys.C)
      {
        e.Handled = true;
        var eitem = errorListBox.SelectedItem as ErrorListBoxItem;
        Clipboard.SetText(eitem.Message);
      }
      if (e.KeyCode == Keys.Enter && errorListBox.SelectedItem != null)
      {
        e.Handled = true;
        var eitem = errorListBox.SelectedItem as ErrorListBoxItem;
        if (eitem != null && eitem.Path != null && ErrorSelected != null)
        {
          ErrorSelected(eitem);
        }
      }
    }

    private void OnClosing(object sender, FormClosingEventArgs e)
    {
      e.Cancel = true;
    }
  }

  public class ErrorListBoxItem
  {
    public int Line       { get; set; }
    public int Column     { get; set; }
    public string Path    { get; set; }
    public string Message { get; set; }

    public override string ToString()
    {
      var p = System.IO.Path.GetFileName(Path);
      return String.Format("{0}@{1}: {2}", p, Line, Message);
    }
  }
}
