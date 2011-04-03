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

using System.Linq;
using System.Reflection;
using Microsoft.Scripting.Ast;

namespace Retoolkit.Utilities
{
  public delegate int LateBoundMethod(object[] arguments);

  public static class DelegateFactory
  {
    public static LateBoundMethod Create(MethodInfo method)
    {
      var argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
      var paramExpression = CreateParameterExpressions(method, argumentsParameter);
      /*
       * <paramExpression> = [ (T0)arguments[0], (T1)arguments[1], ...]
       */

      var call = Expression.Call(method, paramExpression);
      /*
       * <call> = method(<paramExpression>)
       */

      var lambda = Expression.Lambda<LateBoundMethod>(
        call,
        argumentsParameter
      );
      /*
       * int LambdaFuncN(object[] args)
       * {
       *  return (int)<call>;
       * }
       */

      return lambda.Compile();
    }

    private static Expression[] CreateParameterExpressions(MethodInfo method, Expression paramArray)
    {
      return method.GetParameters().Select((parameter, index) =>
        Expression.Convert(
          Expression.ArrayIndex(paramArray, Expression.Constant(index)), // from
          parameter.ParameterType                                        // to
        )
      ).ToArray();
    }
  }
}
