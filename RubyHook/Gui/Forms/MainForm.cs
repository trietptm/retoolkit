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
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Retoolkit.Utilities;
using Retoolkit.Scripting;
using Retoolkit.Gui.Controls;
using Retoolkit.Interfaces;
using WeifenLuo.WinFormsUI.Docking;
using System.IO;
using ScintillaNet;
using Retoolkit.Properties;
using System.Collections.Specialized;
using Retoolkit.Configuration;
using System.Configuration;

namespace Retoolkit.Gui.Forms
{
  public partial class MainForm : Form
  {
    #region Fields
    private OutputContentBox m_outputContent;
    private ErrorContentBox m_errorContent;
    private WindowManager m_windowMgr;
    private IScriptManager m_scriptMgr;
    private IDictionary<String, EditorContentBox> m_openFiles = new Dictionary<String, EditorContentBox>();
    private IPathResolver m_pathResolver;
    private Settings m_settings;

    #endregion

    #region Properties

    
    #endregion

    #region Constructors
    public MainForm(IPathResolver pathResolver, IScriptManager scriptManager, Settings settings)
    {
      m_settings = settings;
      m_pathResolver = pathResolver;
      m_scriptMgr = scriptManager;
      
      InitializeComponent();

      // Window manager
      m_windowMgr = new WindowManager(m_pathResolver, dockPanel, m_settings);
      m_windowMgr.OnCaretChanged += new EventHandler<CaretChangedEventArgs>(WindowManagerOnCaretChanged);

      // Post component initialization
      openFileDialog.InitialDirectory = m_pathResolver.BaseDirectory;
            
      // Load output window
      m_outputContent = new OutputContentBox();
      m_outputContent.Show(dockPanel, DockState.DockBottom);

      // Load error window
      m_errorContent = new ErrorContentBox();
      m_errorContent.Show(dockPanel, DockState.DockBottom);
      m_errorContent.ErrorSelected += new EventOnErrorSelected(OnErrorSelected);
      
      // Adjust bottom panel
      dockPanel.DockBottomPortion = m_settings.DockBottom;
      dockPanel.DockRightPortion = m_settings.DockRight;
      dockPanel.DockLeftPortion = m_settings.DockLeft;
      dockPanel.DockTopPortion = m_settings.DockTop;

      // Attach event handlers
      m_scriptMgr.CompileFinished += new EventHandler(OnCompileFinished);
      m_scriptMgr.CompileInterrupted += new EventHandler(OnCompileInterrupted);
      m_scriptMgr.CompileStarting += new EventHandler(OnCompileStarting);
      m_scriptMgr.ScriptError += new EventHandler<ScriptErrorEventArgs>(OnScriptError);
      m_scriptMgr.ScriptOutput += new EventHandler<ScriptOutputEventArgs>(OnScriptOutput);
      
      m_scriptMgr.ScriptEngineRestarted += new EventHandler(OnScriptEngineRestarted);
      m_scriptMgr.ScriptEngineRestarting += new EventHandler(OnScriptEngineRestarting);

      // Disable Windows-XP default theme; use system colours
      ToolStripProfessionalRenderer renderer = new ToolStripProfessionalRenderer();
      renderer.ColorTable.UseSystemColors = true;
      renderer.RoundedEdges = true;
      ToolStripManager.Renderer = renderer;
    }

    #endregion

    #region Script event handlers

    void OnScriptOutput(object sender, ScriptOutputEventArgs e)
    {
      m_outputContent.AppendText(e.Message);
    }

    void OnScriptError(object sender, ScriptErrorEventArgs e)
    {
      if (String.IsNullOrEmpty(e.Message))
        return;

      switch (e.Type)
      {
        case ErrorMessageFormat.Error:
          m_errorContent.AddErrorMessage(e.Line, e.Column, e.Path, e.Message);
          break;
        case ErrorMessageFormat.Warning:
          m_outputContent.AppendText(e.Message);
          break;
        default:
          break;
      }
    }

    void OnCompileStarting(object sender, EventArgs e)
    {
      this.Invoke(new Action( () => {
        tsLabelStatus.Text = "Running";
        abortScriptToolStripMenuItem.Enabled = true;
        refreshToolStripMenuItem.Enabled = false;
        tsBtnRestart.Enabled = false;
        tsBtnStop.Enabled = true;
        tsBtnStart.Enabled = false;
      }));
    }

    void OnCompileInterrupted(object sender, EventArgs e)
    {
    }

    void OnCompileFinished(object sender, EventArgs e)
    {
      this.Invoke(new Action( () => {
        tsLabelStatus.Text = "Ready";
        abortScriptToolStripMenuItem.Enabled = false;
        refreshToolStripMenuItem.Enabled = true;
        tsBtnRestart.Enabled = true;
        tsBtnStop.Enabled = false;
        tsBtnStart.Enabled = true;
      }));
    }

    private void OnErrorSelected(ErrorListBoxItem error)
    {
      m_windowMgr.GotoFile(error.Path, error.Line, error.Column);
    }

    void OnScriptEngineRestarting(object sender, EventArgs e)
    {
      this.tsLabelStatus.Text = "Restarting";
      m_errorContent.ClearErrors();
      m_outputContent.AppendText("Restarting engine\n");
    }

    void OnScriptEngineRestarted(object sender, EventArgs e)
    {
      this.tsLabelStatus.Text = "Ready!";
      m_outputContent.AppendText("Ready\n");
    }
    
    #endregion

    #region Form event handlers
    private void OnMainFormLoad(object sender, EventArgs e)
    {
      ApplySettings();
      m_windowMgr.AddEditorWindow(m_scriptMgr.MainScript, true);
      m_outputContent.Show(dockPanel);
      m_windowMgr.ActiveEditor.Show(dockPanel);
    }
    
    private void OnMainFormSizeChanged(object sender, EventArgs e)
    {
      m_settings.FormWidth = this.Size.Width;
      m_settings.FormHeight = this.Size.Height;
    }

    private void OnMainFormLocChanged(object sender, EventArgs e)
    {
      m_settings.FormX = this.Location.X;
      m_settings.FormY = this.Location.Y;
    }

    #endregion

    #region Window manager events
    void WindowManagerOnCaretChanged(object sender, CaretChangedEventArgs e)
    {
      Invoke(new Action(() => {
        tsLineLabel.Text = "" + (e.Line);
        tsColLabel.Text = "" + (e.Column);
      }));
    }
    #endregion

    #region File menu handlers
    
    private void newToolStripMenuItem_Click(object sender, EventArgs e)
    {
      NewEditor();
    }

    private void closeToolStripMenuItem_Click(object sender, EventArgs e)
    {
      if (m_windowMgr.HasEditor)
        m_windowMgr.ActiveEditor.Close();
    }

    private void openToolStripMenuItem_Click(object sender, EventArgs e)
    {
      OpenFile();
    }
    
    private void saveToolStripMenuItem_Click(object sender, EventArgs e)
    {
      SaveFile();
    }

    private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
    {
      if (m_windowMgr.HasEditor)
        m_windowMgr.ActiveEditor.SaveFileAs();
    }

    #endregion    

    #region Script menu handlers
    
    private void refreshToolStripMenuItem_Click(object sender, EventArgs e) 
    {
      StartScript();
    }

    private void refreshlocalToolStripMenuItem_Click(object sender, EventArgs e)
    {
      StartCurrentScript();
    }

    private void abortScriptToolStripMenuItem_Click(object sender, EventArgs e)
    {
      AbortScript();
    }

    private void restartEngineToolStripMenuItem_Click(object sender, EventArgs e)
    {
      RestartEngine();
    }
    
    #endregion

    #region Window menu handlers
    private void outputToolStripMenuItem_Click(object sender, EventArgs e)
    {
      m_outputContent.Show(dockPanel);
    }

    private void editorToolStripMenuItem_Click(object sender, EventArgs e)
    {
      if (m_windowMgr.HasEditor)
        m_windowMgr.ActiveEditor.Show(dockPanel);
    }

    private void errorsToolStripMenuItem_Click(object sender, EventArgs e)
    {
      m_errorContent.Show(dockPanel);
    }

    private void saveSettingsToolStripMenuItem_Click(object sender, EventArgs e)
    {
      m_settings.DockBottom = dockPanel.DockBottomPortion;
      m_settings.DockLeft = dockPanel.DockLeftPortion;
      m_settings.DockRight = dockPanel.DockRightPortion;
      m_settings.DockTop = dockPanel.DockTopPortion;

      m_settings.Save();
      MessageBox.Show(this, "Settings saved", "Retoolkit");
    }

    private void clearSettingsToolStripMenuItem_Click(object sender, EventArgs e)
    {
      m_settings.Reset();
      m_settings.Save();
      ApplySettings();
    }

    private void closeAllToolStripMenuItem_Click(object sender, EventArgs e)
    {
      m_windowMgr.CloseAll();
    }

    #endregion

    #region Methods

    private void NewEditor()
    {
      m_windowMgr.NewEditorWindow();
    }

    private void OpenFile()
    {
      openFileDialog.ShowDialog(this);

      foreach (var fileName in openFileDialog.FileNames)
        m_windowMgr.AddEditorWindow(fileName, false);
    }

    private void SaveFile()
    {
      if (m_windowMgr.HasEditor)
        m_windowMgr.ActiveEditor.SaveFile();
    }

    private void StartScript()
    {
      m_errorContent.ClearErrors();
      m_outputContent.Clear();
      m_outputContent.Show(dockPanel);

      if (m_windowMgr.HasEditor)
        m_windowMgr.ActiveEditor.Show(dockPanel);

      m_scriptMgr.Execute();
    }

    private void AbortScript()
    {
      m_outputContent.AppendText("Stopping script\n");
      tsLabelStatus.Text = "Aborting";
      m_scriptMgr.InterruptScript();
    }

    private void StartCurrentScript()
    {

    }

    private void RestartEngine()
    {
      m_scriptMgr.RestartEngine();
    }
    
    private void ApplySettings()
    {
      var formSize = this.Size;
      formSize.Width = m_settings.FormWidth;
      formSize.Height = m_settings.FormHeight;
      this.Size = formSize;

      var formLoc = this.Location;
      formLoc.X = m_settings.FormX;
      formLoc.Y = m_settings.FormY;
      this.Location = formLoc;
    }
    
    
    #endregion

    #region Toolstrip handlers
    
    private void newToolStripButton_Click(object sender, EventArgs e)
    {
      NewEditor();
    }

    private void openToolStripButton_Click(object sender, EventArgs e)
    {
      OpenFile();
    }

    private void saveToolStripButton_Click(object sender, EventArgs e)
    {
      SaveFile();
    }

    private void helpToolStripButton_Click(object sender, EventArgs e)
    {
    }

    private void tsBtnStart_Click(object sender, EventArgs e)
    {
      StartScript();
    }

    private void tsBtnStop_Click(object sender, EventArgs e)
    {
      AbortScript();
    }

    private void tsBtnRestart_Click(object sender, EventArgs e)
    {
      RestartEngine();
    }
    
    #endregion
  }
}
