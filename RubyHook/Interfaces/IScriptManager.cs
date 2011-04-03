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
using System.Linq;
using System.Text;
using Retoolkit.Scripting;
using Microsoft.Scripting.Hosting;

namespace Retoolkit.Interfaces
{
  public interface IScriptManager
  {
    string MainScript { get; }
    ScriptEngine Engine { get; }
    
    void Execute();
    void ExecuteScript(string fileName);
    void InterruptScript();
    void RestartEngine();

    event EventHandler CompileStarting;
    event EventHandler CompileFinished;
    event EventHandler CompileInterrupted;
    event EventHandler ScriptEngineRestarting;
    event EventHandler ScriptEngineRestarted;
    event EventHandler<ScriptOutputEventArgs> ScriptOutput;
    event EventHandler<ScriptErrorEventArgs> ScriptError;
  }

  #region Event args
  public class ScriptOutputEventArgs : EventArgs
  {
    public string Message { get; set; }
  }

  public class ScriptErrorEventArgs : EventArgs
  {
    public string Message { get; set; }
    public string Path { get; set; }
    public int ErrorCode { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public ErrorMessageFormat Type { get; set; }
  }
  #endregion
}
