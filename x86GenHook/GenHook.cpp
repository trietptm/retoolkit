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

// x86GenHook.cpp : Defines the exported functions for the DLL application.
//
#include "stdafx.h"
#include "GenAssemblerEx.h"
#include "GenService.h"
#include "GenHook.h"

#include <iostream>

using namespace std;
using namespace AsmJit;
using namespace GenHook;

int HookCallbackTrampoline(GenHook_t* hook, Registers_t regs, unsigned long flags) 
{
  return ((HookCallback)hook->callback)(hook, &regs, &flags);
}

GENHOOK_API GenHook_t * GenCreateHook( void* address, HookCallback callback, int stackBytes /*= 0*/ )
{
  // create hook
  GenHook_t* hook = new GenHook_t;
  memset(hook, 0, sizeof(GenHook_t));
  InitializeCriticalSection(&hook->cs);
  hook->address = address;
  hook->callback = callback;
  hook->stackBytes = stackBytes;

  // save old instructions (relocations handled automatically)
  CopierAssembler saved(address);
  while (saved.codeSize() < 5)
    saved.consume();
  hook->patch_length = saved.codeSize();
  saved.jmp((void*)((UInt32)address + hook->patch_length));
  hook->exit = saved.make();

  // save original bytes
  memcpy(hook->original, address, saved.codeSize());

  // create prelude stub
  AssemblerEx a;
  Label L_Ret;
  a.pushfd();
  a.pushad();
  a.add(dword_ptr(esp,12),4);
  a.push((UInt32)hook);
  a.call(HookCallbackTrampoline);
  a.add(esp, 4);
  a.test(eax, eax);
  a.popad();
  a.jz(&L_Ret);
  a.popfd();
  a.jmp(hook->exit);
  a.bind(&L_Ret); 
  a.popfd();
  a.ret(hook->stackBytes);
  hook->prelude = a.make();
  a.free();

  // patch address to jmp to prelude
  a.jmp(hook->prelude);
  for (int i=0; i<hook->patch_length-5; ++i)
    a.nop();
  a.rmake(GetCurrentProcess(), hook->address);
  hook->enabled = true;

  // save patched bytes
  memcpy(hook->patched, hook->address, hook->patch_length);

  return hook;
}

GENHOOK_API int GenEnableHook( GenHook_t* hook )
{
  EnterCriticalSection(&hook->cs);
  if (!hook->enabled)
  {
    WriteProcessMemory(GetCurrentProcess(), hook->address, hook->patched, hook->patch_length, NULL);
    hook->enabled = true;
  }
  LeaveCriticalSection(&hook->cs);
  return 1;
}

GENHOOK_API int GenDisableHook( GenHook_t* hook )
{
  EnterCriticalSection(&hook->cs);
  if (hook->enabled)
  {
    WriteProcessMemory(GetCurrentProcess(), hook->address, hook->original, hook->patch_length, NULL);
    hook->enabled = false;
  }
  LeaveCriticalSection(&hook->cs);
  return 1;
}

GENHOOK_API int GenFreeHook( GenHook_t* hook )
{
  GenDisableHook(hook);

  MemoryManager* mmgr = MemoryManager::global();

  void* prelude = hook->prelude;
  if (prelude) 
    mmgr->free(prelude);

  void* exit = hook->exit;
  if (exit) 
    mmgr->free(exit);

  DeleteCriticalSection(&hook->cs);

  delete hook;

  return 1;
}

GENHOOK_API void* GenGetFuncPtr(GenHook_t* hook)
{
  return hook->exit;
}
