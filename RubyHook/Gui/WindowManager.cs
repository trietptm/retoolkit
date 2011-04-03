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
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using Retoolkit.Gui.Controls;
using Retoolkit.Interfaces;
using Retoolkit.Utilities;
using ScintillaNet;
using WeifenLuo.WinFormsUI.Docking;
using Retoolkit.Properties;

namespace Retoolkit.Gui
{
  public class WindowManager
  {
    #region Fields
    
    private DockPanel m_dockPanel;
    private Dictionary<string, EditorContentBox> m_fileEditorMap;
    private EditorContentBox m_activeEditor;
    private IPathResolver m_pathResolver;
    private Settings m_settings;

    #endregion

    #region Events
    public event EventHandler<CaretChangedEventArgs> OnCaretChanged;
    #endregion

    #region Properties
    
    public EditorContentBox ActiveEditor
    {
      get { return m_activeEditor; }
    }

    public bool HasEditor
    {
      get { return m_activeEditor != null; }
    }

    public IEnumerable<EditorContentBox> ActiveEditors
    {
      get
      {
        return m_dockPanel.ActiveDocumentPane.Contents.OfType<EditorContentBox>().ToArray();
      }
    }

    #endregion
    
    #region Constructors
    public WindowManager(IPathResolver pathResolver, DockPanel dockPanel, Settings settings)
    {
      m_settings = settings;
      m_fileEditorMap = new Dictionary<string, EditorContentBox>();
      m_pathResolver = pathResolver;
      m_dockPanel = dockPanel;
    }

    #endregion

    #region New editor methods

    public EditorContentBox NewEditorWindow()
    {
      return MakeNewEditor();
    }

    public EditorContentBox AddEditorWindow(string fileName, bool shouldCreate)
    {
      var fullFilePath = m_pathResolver.Resolve(fileName); 
      var fileKey = Helpers.NormalizePath(fullFilePath);
      EditorContentBox editor = null;
      
      // Already opened editor
      if (m_fileEditorMap.ContainsKey(fileKey))
      {
        editor = m_fileEditorMap[fileKey];
      }
      else // Create new editor, load file if given non-null filename
      {
        editor = NewEditorWindow();
        if (!String.IsNullOrEmpty(fileName))
        {
          editor.LoadFile(fullFilePath, shouldCreate);
          m_fileEditorMap.Add(fileKey, editor);
        }
      }

      editor.Show(m_dockPanel);
      return editor;
    }

    private EditorContentBox MakeNewEditor()
    {
      var editor = new EditorContentBox(m_pathResolver);
      editor.Enter += new EventHandler(OnEditorEntered);
      editor.Editor.FileDrop += new EventHandler<FileDropEventArgs>(OnFileDropped);
      editor.FormClosed += new FormClosedEventHandler(OnEditorClosed);
      editor.Editor.SelectionChanged += new EventHandler(OnEditorSelectionChanged);
      editor.Show(m_dockPanel);

      // fire scintilla oncaretchange
      OnEditorSelectionChanged(editor.Editor, new EventArgs());

      return editor;
    }
    
    #endregion

    #region Editor event handlers

    void OnEditorSelectionChanged(object sender, EventArgs e)
    {
      FireCaretChange(sender as Scintilla);
    }

    private void FireCaretChange(Scintilla sender)
    {
      if (OnCaretChanged != null)
      {
        var scintilla = sender as Scintilla;
        int line = 1;
        int column = 1;
        if (scintilla != null)
        {
          line = scintilla.Caret.LineNumber + 1;
          column = scintilla.GetColumn(scintilla.Caret.Position) + 1;
        }
        var ccea = new CaretChangedEventArgs
        {
          Editor = scintilla,
          Line = line,
          Column = column
        };
        OnCaretChanged(this, ccea);
      }
    }
    
    void OnFileDropped(object sender, FileDropEventArgs e)
    {
      foreach(var file in e.FileNames)
      {
        this.AddEditorWindow(file, false);
      }
    }

    void OnEditorClosed(object sender, FormClosedEventArgs e)
    {
      var editorControl = sender as EditorContentBox;
      if (editorControl != null && !String.IsNullOrEmpty(editorControl.FilePath))
      {
        var fileName = editorControl.FilePath;
        m_fileEditorMap.Remove(Helpers.NormalizePath(fileName));
      }
      m_activeEditor = null;
      FireCaretChange(null);
    }

    void OnEditorEntered(object sender, EventArgs e)
    {
      m_activeEditor = sender as EditorContentBox;
      FireCaretChange(m_activeEditor.Editor);
    }
    
    #endregion

    #region Save methods
    
    public void SaveCurrent()
    {

      if (m_activeEditor != null)
      {
        m_activeEditor.SaveFile();
      }
    }

    public void SaveAll()
    {
      foreach (var editor in ActiveEditors)
      {
        editor.SaveFile();
      }
    }
    
    #endregion

    #region Methods

    public void CloseAll()
    {
      foreach (var editor in ActiveEditors)
      {
        editor.Close();
      }
    }

    public EditorContentBox FindOrCreateEditor(string fileName)
    {
      var fileKey = Helpers.NormalizePath(fileName);
      if (m_fileEditorMap.ContainsKey(fileKey))
      {
        var editor = m_fileEditorMap[fileKey];
        editor.Show(m_dockPanel);
        return editor;
      }
      else
      {
        var editor = AddEditorWindow(fileName, false);
        return editor;
      }
    }

    public void GotoFile(string fileName, int line, int? column)
    {
      var editor = FindOrCreateEditor(fileName);
      if (editor != null)
        editor.Editor.GoTo.Line(line - 1);
    }

    #endregion
  }
}
