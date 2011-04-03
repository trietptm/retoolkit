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
using System.Reflection.Emit;
using System.Reflection;
using System.Runtime.InteropServices;
using IronRuby.Builtins;
using Microsoft.Scripting.Ast;
using Microsoft.Scripting.Math;

namespace Retoolkit.Utilities
{
  public class FreeApi
  {
    #region Fields
    int m_address;
    string m_paramString;
    string m_retString;
    string m_callConv;
    LateBoundMethod m_invoker;
    #endregion

    public FreeApi(int address, string paramString, string retString, params string[] callConv)
    {
      m_address = address;
      m_paramString = paramString.Trim();
      m_retString = retString.Trim();
      m_callConv = callConv.Length > 0 ? callConv[0].Trim() : "s";

      Relocate(m_address);
    }

    public int Call(params object[] p)
    {
      for (int i = 0; i < p.Length; ++i)
      {
        p[i] = MarshalArg(p[i]);
      }

      return m_invoker(p);
    }

    public void Relocate(int address)
    {
      m_address = address;
      m_invoker = DelegateFactory.Create(MakeMethod(m_address, m_paramString, m_retString, m_callConv));
    }

    #region Methods

    private DynamicMethod MakeMethod(int address, string paramString, string retString, string callConv)
    {
      // setup parameter types
      List<Type> paramList = new List<Type>();
      foreach (var c in paramString) paramList.Add(GetParamType(c));
      var paramTypeAry = paramList.ToArray();
      Type retType = typeof(int);

      // craete dynamic method
      var dynMeth = new DynamicMethod(
        "DynaMeth",
        MethodAttributes.Public | MethodAttributes.Static,
        CallingConventions.Standard,
        retType,              // return type
        paramTypeAry,         // params
        typeof(void).Module,  // owner
        true
      );

      // generate IL code
      var il = dynMeth.GetILGenerator();

      // load arguments
      for (int i = 0; i < paramTypeAry.Length; ++i)
        il.Emit(OpCodes.Ldarg, i);

      // do call
      il.Emit(OpCodes.Ldc_I4, address);
      il.EmitCalli(OpCodes.Calli, GetCC(callConv[0]), retType, paramTypeAry);

      // return
      il.Emit(OpCodes.Ret);

      return dynMeth;
    }

    private object MarshalArg(object p)
    {
      if (p is int) { return p; }
      else if (p is BigInteger) 
      {
        var bigInt = p as BigInteger;
        if (bigInt.GetBitCount() <= 32)
        {
          UInt32 val = bigInt.ToUInt32();
          return unchecked((int)val);
        }          
      }
      else if (p is MutableString)
        return ((MutableString)p).ToByteArray(); 
      
      throw new ArgumentException("Couldn't marshal argument");
    }

    private Type GetParamType(char paramType)
    {
      switch (paramType)
      {
        case 'i':
        case 'I':
        case 'l':
        case 'L':
          return typeof(int);
        case 'p':
        case 'P':
          return typeof(byte[]);
        default:
          throw new ArgumentException("Invalid param type");
      }
    }

    private CallingConvention GetCC(char c)
    {
      switch (Char.ToLower(c))
      {
        case 'c':
          return CallingConvention.Cdecl;
        case 's': 
          return CallingConvention.StdCall;
        case 't': 
          return CallingConvention.ThisCall;
        default: 
          throw new ArgumentException(
            "Unknown convention. Accepted values are 's', 't', or 'c'"
          );
      }
    }

    #endregion
  }
}
