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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using IronRuby.Builtins;

namespace Retoolkit.Utilities
{
  public static class Helpers
  {
    #region Keyboard utils
    public static bool IsAlt(this KeyEventArgs args, Keys code)
    {
      return (args.Alt && args.KeyCode == code);
    }

    public static bool IsCtrl(this KeyEventArgs args, Keys code)
    {
      return (args.Control && args.KeyCode == code);
    }

    public static bool IsCtrlShift(this KeyEventArgs args, Keys code)
    {
      return (args.Control && args.Shift && args.KeyCode == code);
    }
    #endregion

    #region Path utils
    public static string GetAsmDirectory()
    {
      var fullPath = Assembly.GetExecutingAssembly().Location;
      return Path.GetDirectoryName(fullPath);
    }

    public static string NormalizePath(string p1)
    {
      string pp = p1.Trim();
      pp = pp.Replace('\\', '/');
      if (pp.EndsWith("/")) 
        pp = pp.Remove(pp.Length - 1);
      return pp;
    }

    public static bool PathsEqual(string p1, string p2)
    {
      return (String.Compare(NormalizePath(p1), NormalizePath(p2), true) == 0);
    }

    #endregion

    #region Memory utils

    public static unsafe IntPtr GetBytePtr(MutableString str)
    {
      IntPtr pp;
      fixed (byte* bp = str.ToByteArray())
      {
        pp = (IntPtr)bp;
      }
      return pp;
    }

    public static unsafe int Read8(IntPtr ptr)
    {
      return unchecked(*((sbyte*)ptr));
    }

    public static unsafe int Read16(IntPtr ptr)
    {
      return unchecked(*((short*)ptr));
    }

    public static unsafe int Read32(IntPtr ptr)
    {
      return unchecked(*((int*)ptr));
    }

    public static unsafe uint ReadU8(IntPtr ptr)
    {
      return unchecked(*((byte*)ptr));
    }

    public static unsafe uint ReadU16(IntPtr ptr)
    {
      return unchecked(*((ushort*)ptr));
    }

    public static unsafe uint ReadU32(IntPtr ptr)
    {
      return unchecked(*((uint*)ptr));
    }

    #endregion

    #region Integer conversions
    public static int ToInt32(UInt32 u)
    {
      return unchecked((int)u);
    }

    public static UInt32 ToUInt32(int i)
    {
      return unchecked((UInt32)i);
    }
    #endregion

    #region Form utils

    #endregion
  }
}
