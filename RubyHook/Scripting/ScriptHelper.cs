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
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;
using Retoolkit.Interfaces;
using IronRuby.Builtins;
using IronRuby.Runtime;

namespace Retoolkit.Scripting
{
  internal sealed class ScriptHelper
  {
    private static string RecompileSnippet = @"
      ObjectSpace.each_object(Module) {{ |obj| 
        begin
          obj.send :{0} if obj.respond_to? :{0} 
        rescue
          $onError.invoke($!.message, $!.backtrace)
        end
      }}";

    private static string SpinLoopSnippet = @"
    while true do
      sleep({0})
    end
    ";

    public static void SpinLoop(double sleepRate, ScriptScope scope)
    {
      var snippet = String.Format(SpinLoopSnippet, sleepRate);
      var source = scope.Engine.CreateScriptSourceFromString(snippet);
      source.Execute(scope);
    }

    public static void SendMethodToModules(string methodName, ScriptScope scope)
    {
      var engine = scope.Engine;
      var snippet = String.Format(RecompileSnippet, methodName);
      var source = engine.CreateScriptSourceFromString(snippet, SourceCodeKind.Statements);
      source.Execute(scope);
    }

    public static void ExecuteTopMethod(string methName, ScriptScope scope)
    {
      Action act;
      if (scope.TryGetVariable<Action>(methName, out act))
      {
        act.Invoke();
      }
    }
  }
}
