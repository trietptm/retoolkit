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
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using IronRuby.Runtime;
using IronRuby.Builtins;
using System.Text.RegularExpressions;

namespace Retoolkit.Scripting
{
  #region Enums
  public enum ErrorMessageFormat
  {
    Default = 0,
    MessageOnly = Default,
    Warning,
    Error
  }
  #endregion

  #region Interfaces
  internal interface IErrorInfoProvider
  {
    bool HasExtendedInfo { get; }
    int? Line { get; }
    int? Column { get; }
    string Path { get; }
    string Message { get; }
    ErrorMessageFormat Format { get; }
  }
  #endregion

  internal class RubyErrorProvider : ErrorListener, IErrorInfoProvider
  {
    #region Fields
    private bool m_hasInfo = false;
    private int? m_line = null;
    private int? m_col = null;
    private string m_message;
    private string m_path;
    private ErrorMessageFormat m_format = ErrorMessageFormat.Default;
    #endregion

    #region Static methods
    public static RubyErrorProvider AsErrorSink()
    {
      return new RubyErrorProvider();
    }

    public static RubyErrorProvider ParseException(Exception ex)
    {
      return new RubyErrorProvider(ex);
    }

    public static RubyErrorProvider FromMessageAndBacktrace(MutableString message, RubyArray backtrace)
    {
      var rep = new RubyErrorProvider();
      rep.ParseFromBacktrace(message, backtrace);
      return rep;
    }
    #endregion

    #region Constructors
    public RubyErrorProvider()
    {
    }
    
    public RubyErrorProvider(Exception ex)
    {
      _ParseException(ex);
    }
    #endregion

    #region Overridden ErrorListener methods
    public override void ErrorReported(ScriptSource source, string message, SourceSpan span, int errorCode, Severity severity)
    {
      m_hasInfo = true;
      m_message = message;
      m_path = source.Path;
      m_line = span.Start.Line;
      m_col = span.Start.Column;

      switch (severity)
      {
        case Severity.Error:
        case Severity.FatalError:
          m_format = ErrorMessageFormat.Error;
          break;
        case Severity.Ignore:
        case Severity.Warning:
          m_format = ErrorMessageFormat.Warning;
          break;
      }
    }
    #endregion

    #region IErrorInfoProvider Members

    public bool HasExtendedInfo
    {
      get { return m_hasInfo; }
    }

    public int? Line
    {
      get 
      {
        return (m_hasInfo) ? m_line : null;
      }
    }

    public int? Column
    {
      get 
      {
        return (m_hasInfo) ? m_col : null;
      }
    }

    public string Message
    {
      get { return m_message; }
    }

    public ErrorMessageFormat Format
    {
      get { return m_format; }
    }

    public string Path
    {
      get { return m_path; }
    }

    #endregion

    #region Methods
    
    private void _ParseException(Exception ex)
    {
      var red = RubyExceptionData.GetInstance(ex);
      var trace = red.Backtrace;

      ParseFromBacktrace((MutableString)red.Message, trace);
    } 

    private void ParseFromBacktrace(MutableString message, RubyArray backtrace)
    {
      m_message = message.ToString();

      var trace = backtrace.Cast<MutableString>().Select((line) => line.ToString().Trim());
      var rubyPathLine = 
        from line in trace
        let match = Regex.Match(line, @"(.*\.rb):(\d+)(:in\s+(.*))?$")
        let groupCnt = match.Groups.Count
        where match.Success
        let innerLoc = match.Groups[4].Value
        select new
        {
          FilePath = match.Groups[1].Value,
          FileLine = int.Parse(match.Groups[2].Value),
          InnerLoc = String.IsNullOrEmpty(innerLoc) ? "<top>" : innerLoc
        };

      var infoObj = rubyPathLine.FirstOrDefault();
      if (infoObj != null)
      {
        m_hasInfo = true;
        m_format = ErrorMessageFormat.Error;
        m_message = String.Format("{0} (in {1})", m_message, infoObj.InnerLoc);
        m_path = infoObj.FilePath;
        m_line = infoObj.FileLine;
      }
    }
    
    #endregion
  }
}
