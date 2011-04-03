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
using System.IO;
using System.Windows.Forms;
using Retoolkit.Interfaces;
using ScintillaNet;
using WeifenLuo.WinFormsUI.Docking;
using System.Text;

namespace Retoolkit.Gui.Controls
{
  public partial class EditorContentBox : DockContent
  {
    #region Static methods
    private static int m_newFileCount = 1;
    private static readonly Encoding m_defaultEnc = new UTF8Encoding(false);
    private static string MakeGenericFileName()
    {      
      return String.Format("Untitled {0}", m_newFileCount++);
    }

    private static readonly string CONFIG_XML_REL_PATH = @"resources\ScintillaNET.xml";

    private static Dictionary<string, string> EXT_LANG_MAP = new Dictionary<string, string>
    {
      {".rb", "ruby"},
      {".xml", "xml"},
      {".xsd", "xml"}
    };

    #endregion

    #region Events
    
    #endregion

    #region Fields
    private string m_baseTitle;
    private string m_filePath;
    private IPathResolver m_pathResolver;
    bool m_ignoreUpdate = false;
    #endregion

    #region Properties
    public SaveFileDialog SaveFileDialog 
    { 
      get { return saveFileDialog; } 
    }
    
    public Scintilla Editor 
    { 
      get { return scintilla; } 
    }

    public string FilePath
    {
      get { return m_filePath; }
    }

    public bool FileLoaded
    {
      get;
      private set;
    }
    #endregion

    #region Constructors

    public EditorContentBox(IPathResolver pathResolver)
    {
      m_pathResolver = pathResolver;
      m_baseTitle = MakeGenericFileName();
      
      InitializeComponent();
      
      // scintilla control settings
      var confMgr = scintilla.ConfigurationManager;
      confMgr.CustomLocation = m_pathResolver.Resolve(CONFIG_XML_REL_PATH);
      confMgr.Language = "default";
    }
    #endregion

    #region Form event handlers
    
    private void EditorFormOnLoad(object sender, EventArgs e)
    {
      UpdateTitle();
      saveFileDialog.InitialDirectory = m_pathResolver.BaseDirectory;
    }

    private void EditorFormOnClosing(object sender, FormClosingEventArgs e)
    {
      // Notify if closing document has modifications
      if (scintilla.Modified)
      {
        var dr = MessageBox.Show (
          "The file has been modified. Do you wish to save?",
          "Save file",
          MessageBoxButtons.YesNoCancel,
          MessageBoxIcon.Question
        );

        if (dr == DialogResult.Yes)
          e.Cancel = !this.SaveFile();
        else if (dr == DialogResult.Cancel)
        {
          e.Cancel = true;
          return;
        }
      }
    }

    #endregion

    #region Scintilla event handlers
    private void ScintillaOnTextChanged(object sender, EventArgs e)
    {
      if (m_ignoreUpdate)
      {
        m_ignoreUpdate = false;
        return;
      }
      
      scintilla.Modified = true;
      UpdateTitle();
    }

    #endregion

    #region Methods
    
    public bool LoadFile(string fileName, bool shouldCreate)
    {
      // Load file
      if (File.Exists(fileName))
      {
        scintilla.Text = File.ReadAllText(fileName, m_defaultEnc);
        // Needed to prevent invalid dirty flag when loading files
        m_ignoreUpdate = true;
      }
      else if (shouldCreate)
      {
        File.OpenWrite(fileName).Close();
      }
      else // !Exists and !create
      {
        throw new FileNotFoundException("Could not find file " + fileName);
      }
      
      // Do standard operations for new loaded newfile
      OnNewFileName(fileName);

      return true;
    }

    public bool SaveFile()
    {
      if (!FileLoaded)
        return SaveFileAs();
      
      WriteFile(m_filePath);      
      return true;
    }

    public bool SaveFileAs()
    {
      saveFileDialog.ShowDialog();
      if (!String.IsNullOrEmpty(saveFileDialog.FileName))
      {
        WriteFile(saveFileDialog.FileName);
        return true;
      }
      else
        return false;
    }

    public void ReloadConfig()
    {
      scintilla.ConfigurationManager.CustomLocation = m_pathResolver.Resolve(CONFIG_XML_REL_PATH);
      if (!String.IsNullOrEmpty(m_filePath))
      {
        string ext = Path.GetExtension(m_filePath);
        if (EXT_LANG_MAP.ContainsKey(ext))
          scintilla.ConfigurationManager.Language = EXT_LANG_MAP[ext];
      }
      else
        scintilla.ConfigurationManager.Configure();
    }

    private void WriteFile(string fileName)
    {
      File.WriteAllText(fileName, scintilla.Text, m_defaultEnc);
      OnNewFileName(fileName);
    }

    private void OnNewFileName(string fileName)
    {
      m_filePath = fileName;
      FileLoaded = true;
      m_baseTitle = Path.GetFileName(m_filePath);
      scintilla.Modified = false;
      ReloadConfig();
      UpdateTitle();
    }

    private void UpdateTitle()
    {
      string title = m_baseTitle;
      if (scintilla.Modified)
        title += " *";
      this.Text = title;
    }
    
    #endregion

  }

  public class CaretChangedEventArgs : EventArgs
  {
    public int Line { get; set; }
    public int Column { get; set; }
    public Scintilla Editor { get; set; }
  }
}
