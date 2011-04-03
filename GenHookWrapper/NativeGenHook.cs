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
  public struct X86Registers
  {
    public IntPtr Edi;
    public IntPtr Esi;
    public IntPtr Ebp;
    public IntPtr Esp;
    public IntPtr Ebx;
    public IntPtr Edx;
    public IntPtr Ecx;
    public IntPtr Eax;
  }

  [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
  public delegate bool HookCallback(IntPtr handle, ref X86Registers regs, ref UInt32 flags);

  public static class GenHook
  {
    // hook functions
    [DllImport("GenHook.dll", CallingConvention = CallingConvention.Cdecl)]
    extern static public IntPtr GenCreateHook(IntPtr address, HookCallback callback, int stackBytes);
    
    [DllImport("GenHook.dll", CallingConvention = CallingConvention.Cdecl)]
    extern static public IntPtr GenEnableHook(IntPtr handle);
    
    [DllImport("GenHook.dll", CallingConvention = CallingConvention.Cdecl)]
    extern static public IntPtr GenDisableHook(IntPtr handle);

    [DllImport("GenHook.dll", CallingConvention = CallingConvention.Cdecl)]
    extern static public IntPtr GenGetFuncPtr(IntPtr handle);
    
    [DllImport("GenHook.dll", CallingConvention = CallingConvention.Cdecl)]
    extern static public IntPtr GenFreeHook(IntPtr handle);

    // injection 
    [DllImport("GenHook.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    extern static public IntPtr GenClrInject(IntPtr target, string dllPath, string appBase, string privatePath, string userData);

    [DllImport("GenHook.dll", CallingConvention = CallingConvention.Cdecl)]
    extern static public UInt32 GenSyncInjection(IntPtr target);
  }
}
