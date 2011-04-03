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

using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using Retoolkit.Scripting;
using System;
using Retoolkit.Utilities;
using Retoolkit.Properties;
using Retoolkit.Gui.Forms;

namespace Retoolkit
{
  public class Program
  {
    [STAThread]
    static void Main(string[] args)
    {
      // Get settings object
      var settings = Settings.Default;

      // Get principle script name from current process
      var process = Process.GetCurrentProcess();
      var processName = process.MainModule.ModuleName;
      var mainScript = Path.ChangeExtension(processName, ".rb");

      // Creating path resolver
      var pathProvider = new DefaultPathResolver(Helpers.GetAsmDirectory());

      // Create script manager
      var scriptManager = new IronScriptManager(mainScript, pathProvider, settings);

      // Create main form
      var editor = new MainForm(pathProvider, scriptManager, settings);
      editor.Text = string.Format(
        "Retoolkit - {0} ({1})",
        processName,
        process.Id
      );

      // Hit it
      Application.EnableVisualStyles();
      Application.Run(editor);
    }
  }
}
