namespace Retoolkit.Gui.Controls
{
  partial class EditorContentBox
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditorContentBox));
      this.scintilla = new ScintillaNet.Scintilla();
      this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
      ((System.ComponentModel.ISupportInitialize)(this.scintilla)).BeginInit();
      this.SuspendLayout();
      // 
      // scintilla
      // 
      this.scintilla.AllowDrop = true;
      this.scintilla.Caret.CurrentLineBackgroundColor = System.Drawing.Color.Lavender;
      this.scintilla.Caret.HighlightCurrentLine = true;
      this.scintilla.Dock = System.Windows.Forms.DockStyle.Fill;
      this.scintilla.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.scintilla.Indentation.IndentWidth = 2;
      this.scintilla.Indentation.SmartIndentType = ScintillaNet.SmartIndent.Simple;
      this.scintilla.Indentation.TabIndents = false;
      this.scintilla.Indentation.TabWidth = 2;
      this.scintilla.Indentation.UseTabs = false;
      this.scintilla.IsBraceMatching = true;
      this.scintilla.Location = new System.Drawing.Point(0, 0);
      this.scintilla.Margins.Margin0.Width = 28;
      this.scintilla.Margins.Margin2.Width = 12;
      this.scintilla.Name = "scintilla";
      this.scintilla.Size = new System.Drawing.Size(279, 233);
      this.scintilla.Styles.BraceBad.FontName = "Verdana";
      this.scintilla.Styles.BraceLight.FontName = "Verdana";
      this.scintilla.Styles.ControlChar.FontName = "Verdana";
      this.scintilla.Styles.Default.FontName = "Verdana";
      this.scintilla.Styles.IndentGuide.FontName = "Verdana";
      this.scintilla.Styles.LastPredefined.FontName = "Verdana";
      this.scintilla.Styles.LineNumber.FontName = "Verdana";
      this.scintilla.Styles.Max.FontName = "Verdana";
      this.scintilla.TabIndex = 0;
      this.scintilla.TextChanged += new System.EventHandler<System.EventArgs>(this.ScintillaOnTextChanged);
      // 
      // EditorContentBox
      // 
      this.ClientSize = new System.Drawing.Size(279, 233);
      this.Controls.Add(this.scintilla);
      this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
      this.Name = "EditorContentBox";
      this.Load += new System.EventHandler(this.EditorFormOnLoad);
      this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.EditorFormOnClosing);
      ((System.ComponentModel.ISupportInitialize)(this.scintilla)).EndInit();
      this.ResumeLayout(false);

    }

    #endregion

    private ScintillaNet.Scintilla scintilla;
    private System.Windows.Forms.SaveFileDialog saveFileDialog;
  }
}
