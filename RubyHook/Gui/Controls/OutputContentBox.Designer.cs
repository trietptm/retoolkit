namespace Retoolkit.Gui.Controls
{
  partial class OutputContentBox
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
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OutputContentBox));
      this.scintilla = new ScintillaNet.Scintilla();
      ((System.ComponentModel.ISupportInitialize)(this.scintilla)).BeginInit();
      this.SuspendLayout();
      // 
      // scintilla
      // 
      this.scintilla.Dock = System.Windows.Forms.DockStyle.Fill;
      this.scintilla.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.scintilla.LineWrap.Mode = ScintillaNet.WrapMode.Word;
      this.scintilla.LineWrap.VisualFlags = ScintillaNet.WrapVisualFlag.Start;
      this.scintilla.Location = new System.Drawing.Point(0, 0);
      this.scintilla.Margins.Margin1.Width = 0;
      this.scintilla.MatchBraces = false;
      this.scintilla.Name = "scintilla";
      this.scintilla.Scrolling.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
      this.scintilla.Size = new System.Drawing.Size(292, 273);
      this.scintilla.TabIndex = 0;
      this.scintilla.UseFont = true;
      // 
      // OutputContentBox
      // 
      this.ClientSize = new System.Drawing.Size(292, 273);
      this.Controls.Add(this.scintilla);
      this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
      this.Name = "OutputContentBox";
      this.Text = "Output";
      this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnClosing);
      ((System.ComponentModel.ISupportInitialize)(this.scintilla)).EndInit();
      this.ResumeLayout(false);

    }

    #endregion

    private ScintillaNet.Scintilla scintilla;
  }
}
