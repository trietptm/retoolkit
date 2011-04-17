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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using IronRuby;
using Microsoft.Scripting.Hosting;
using Retoolkit.Utilities;
using Microsoft.Scripting;
using IronRuby.Runtime;
using IronRuby.Builtins;
using Retoolkit.Interfaces;
using Retoolkit.Properties;
using Microsoft.Scripting.Hosting.Providers;

namespace Retoolkit.Scripting
{
  #region Classes
  public class IronScriptManager : IScriptManager
  {
    #region Fields

    private Settings m_settings;

    private ProxyStream m_outputStream = new ProxyStream();
    private ProxyStream m_errorStream = new ProxyStream();
    private ScriptEngine m_engine;
    private IPathResolver m_pathResolver;
    private string m_mainScript;
    
    // Script thread synchronization  
    private Thread m_scriptThread;
    private volatile bool m_runningScript = false;
    private object m_recompileLock = new object();

    // Script objects
    private ScriptSource m_scriptSrc;

    #endregion

    #region Properties
    public ScriptEngine Engine
    {
      get
      {
        return m_engine;
      }
    }
    #endregion

    #region Constructors
    
    public IronScriptManager(string mainScript, IPathResolver pathResolver, Settings settings)
    {
      m_settings = settings;
      m_pathResolver = pathResolver;
      m_mainScript = m_pathResolver.Resolve(mainScript);
      
      // Create script file if it doesn't exist
      if (!File.Exists(m_mainScript))
      {
        var stream = File.CreateText(m_mainScript);
        stream.Dispose();
      }

      // Create ruby engine
      m_engine = CreateAndInitEngine();
    }
    
    #endregion

    #region EventHandlers

    public event EventHandler CompileStarting;
    public event EventHandler CompileFinished;
    public event EventHandler CompileInterrupted;
    public event EventHandler ScriptEngineRestarted; 
    public event EventHandler<ScriptOutputEventArgs> ScriptOutput;
    public event EventHandler<ScriptErrorEventArgs> ScriptError;

    private void OnCompileStarting()
    {
      if (CompileStarting != null)
        CompileStarting(this, new EventArgs());
    }

    private void OnCompileFinished()
    {
      if (CompileFinished != null)
        CompileFinished(this, new EventArgs());
    }

    private void OnCompileInterrupted()
    {
      if (CompileInterrupted != null)
        CompileInterrupted(this, new EventArgs());
    }

    private void OnScriptOutput(string message)
    {
      if (ScriptOutput != null)
      {
        var args = new ScriptOutputEventArgs { Message = message };
        ScriptOutput(this, args);
      }
    }

    private void OnScriptError(string message, string path, int? line, int? column, ErrorMessageFormat format)
    {
      if (ScriptError != null)
      {
        var args = new ScriptErrorEventArgs 
        { 
          Message = message,
          Path = path,
          Line = (line == null) ? -1 : (int)line,
          Column = (column == null) ? -1 : (int)column,
          Type = format
        };
        ScriptError(this, args);
      }
    }
    #endregion

    #region IScriptManager Members

    public string MainScript
    {
      get { return m_mainScript; }
    }

    public void Execute()
    {
      lock (m_recompileLock)
      {
        if (m_runningScript)
          return;

        m_scriptThread = new Thread(ScriptThreadProc);
        m_scriptThread.Name = "ScriptThread";
        OnCompileStarting();
        m_scriptThread.Start();
      }
    }

    public void ExecuteScript(string filePath)
    {
    }

    public void InterruptScript()
    {
      lock (m_recompileLock)
      {
        if (m_runningScript)
        {
          m_scriptThread.Abort();
          OnCompileInterrupted();
        }
      }
    }

    #endregion

    #region ScriptThreadProc

    private void ScriptThreadProc()
    {
      ScriptScope scope = null;
      try
      {
        m_runningScript = true;

        var rep = RubyErrorProvider.AsErrorSink();
        var compiledCode = m_scriptSrc.Compile(rep);
        if (compiledCode != null)
        {
          scope = compiledCode.DefaultScope;
          compiledCode.Execute();
          ScriptHelper.SpinLoop(0.5, scope);
        }
        else
          TriggerErrorEvent(rep);
      }
      catch (ThreadAbortException)
      { // We don't want to do anything special about thread aborts,
        // since these are usually invoked by the user
      }
      catch (Exception ex)
      {
        TriggerErrorEvent(RubyErrorProvider.ParseException(ex));
      }
      finally
      {
        // only execute if no syntax errors
        if (scope != null)
        { 
          ScriptHelper.SendMethodToModules("onFinish", scope);
          ScriptHelper.ExecuteTopMethod("onFinish", scope);
        }
        m_runningScript = false;
        OnCompileFinished();
      }
    }

    #endregion
    
    #region Methods

    private void DefaultExceptionHandler(Exception ex)
    {
      OnScriptError(GetExceptionMessage(ex), "", -1, -1, ErrorMessageFormat.MessageOnly);
    }

    private void TriggerErrorEvent(RubyErrorProvider rep)
    {
      OnScriptError(rep.Message, rep.Path, rep.Line, rep.Column, rep.Format);
    }

    private void TriggerErrorEventFromRuby(MutableString message, RubyArray backtrace)
    {
      TriggerErrorEvent(RubyErrorProvider.FromMessageAndBacktrace(message, backtrace));
    }
    
    private string GetExceptionMessage(Exception e)
    {
      var eo = m_engine.GetService<ExceptionOperations>();
      var msg = eo.FormatException(e);
      return msg;
    }

    private ScriptEngine CreateAndInitEngine()
    {
      var engine = Ruby.CreateEngine();
      engine.Runtime.LoadAssembly(Assembly.GetExecutingAssembly());

      // set library/assembly search paths
      // (used by require and load_assembly)
      var libPathProvider = new LibraryPathFile(m_pathResolver, @"path.txt");
      engine.SetSearchPaths(libPathProvider.Paths);

      // Expose global objects
      var ctx = HostingHelpers.GetLanguageContext(engine) as RubyContext;
      ctx.DefineReadOnlyGlobalVariable(
        "onError",
        new Action<MutableString, RubyArray>(TriggerErrorEventFromRuby) // #onError(message, backtrace)
      );
            
      // HACK: needed to set $0 == <main script filename>
      ctx.DefineGlobalVariable("0", Path.GetFileName(MainScript));
      ctx.DefineReadOnlyGlobalVariable("pr", m_pathResolver);

      // Create initial script source
      m_scriptSrc = engine.CreateScriptSourceFromFile(
        MainScript, 
        Encoding.UTF8, 
        SourceCodeKind.File
      );

      // Set new IO streams
      RedirectIOStreams(engine);
      return engine;
    }

    private void RedirectIOStreams(ScriptEngine engine)
    {
      m_outputStream.Writer = (buf, offs, cnt) =>
      {
        var msg = Encoding.UTF8.GetString(buf, offs, cnt);
        OnScriptOutput(msg);
      };

      m_errorStream.Writer = (buf, offs, cnt) =>
      {
        var msg = Encoding.UTF8.GetString(buf, offs, cnt);
        OnScriptError(msg, "", null, null, ErrorMessageFormat.MessageOnly);
      };

      engine.Runtime.IO.SetOutput(m_outputStream, new StreamWriter(m_outputStream));
      engine.Runtime.IO.SetErrorOutput(m_errorStream, new StreamWriter(m_errorStream));
    }

    public void RestartEngine()
    {
      if (!m_runningScript)
      {
        if (ScriptEngineRestarting != null)
        {
          ScriptEngineRestarting(this, new EventArgs());
        }

        var context = HostingHelpers.GetLanguageContext(m_engine);
        context.Shutdown();
        m_engine = CreateAndInitEngine();

        if (ScriptEngineRestarted != null)
        {
          ScriptEngineRestarted(this, new EventArgs());
        }
      }
    }

    #endregion

    #region IScriptManager Members

    public event EventHandler ScriptEngineRestarting;

    #endregion
  }

  #endregion
}
