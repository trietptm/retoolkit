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
using System.Runtime.InteropServices;

namespace NativeGenHook
{
  public static class NativeInvoke
  {
    /* extern "C" GENHOOK_API void GenNativeInvoke( DWORD address,
                                             Registers_t* regs,
                                             int stackArgSize, 
                                             DWORD* stackArgs, 
                                             DWORD storedEsp, 
                                             DWORD* outEax, 
                                             DWORD* outEdx );
     */

    [DllImport("GenHook.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GenNativeInvoke(IntPtr address, X86Registers[] regs, int argCount, IntPtr[] args, IntPtr unused, out IntPtr eax, out IntPtr edx);
  }
}
