namespace Retoolkit.Gui.Controls
{
  partial class ErrorContentBox
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
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ErrorContentBox));
      this.errorListBox = new System.Windows.Forms.ListBox();
      this.SuspendLayout();
      // 
      // errorListBox
      // 
      this.errorListBox.Dock = System.Windows.Forms.DockStyle.Fill;
      this.errorListBox.FormattingEnabled = true;
      this.errorListBox.Location = new System.Drawing.Point(0, 0);
      this.errorListBox.Name = "errorListBox";
      this.errorListBox.Size = new System.Drawing.Size(292, 264);
      this.errorListBox.TabIndex = 0;
      this.errorListBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnKeyDown);
      // 
      // ErrorContentBox
      // 
      this.AutoScroll = true;
      this.ClientSize = new System.Drawing.Size(292, 273);
      this.Controls.Add(this.errorListBox);
      this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
      this.Name = "ErrorContentBox";
      this.Text = "Errors";
      this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnClosing);
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.ListBox errorListBox;
  }
}
