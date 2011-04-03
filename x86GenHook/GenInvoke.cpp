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

#include "stdafx.h"
#include "GenInvoke.h"

using namespace AsmJit;

// Heavy-weight metainvokation. Do not use.
GENHOOK_API void GenNativeInvoke( DWORD address, 
                                  Registers_t* regs, 
                                  int argCount, 
                                  DWORD* stackArgs,
                                  DWORD storedEsp,
                                  DWORD* outEax, 
                                  DWORD* outEdx ) { __asm {
  mov storedEsp, esp
  mov ecx, argCount
  test ecx, ecx
  jz handleRegArgs
  lea ecx, [ecx * 4]
  sub esp, ecx
  mov edi, esp
  mov esi, stackArgs
  rep movsb

handleRegArgs:
  mov eax, regs
  test eax, eax
  jz makecall

  // save ESP and EBP
  mov [eax]Registers_t.ESP, esp
  mov [eax]Registers_t.EBP, ebp

  // do register shuffle
  mov esp, regs
  popad

  // restore EBP, ESP
  // bring esp back to regs struct
  sub esp, 0x20
  mov ebp, [esp]Registers_t.EBP
  mov esp, [esp]Registers_t.ESP

makecall:
  call address
  mov esp, storedEsp
  mov ecx, outEax;
  mov [ecx], eax
  mov ecx, outEdx;
  mov [ecx], edx

}}
