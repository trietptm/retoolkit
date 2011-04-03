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

#ifndef GENHOOK_H
#define GENHOOK_H

#include <Windows.h>
#include <string>
#include "GenConfig.h"

struct Registers_t;
struct GenHook_t;

typedef int(*HookCallback)(GenHook_t*, Registers_t*, unsigned long*);

extern "C" GENHOOK_API GenHook_t * GenCreateHook(void* address, HookCallback callback, int stackBytes = 0);
extern "C" GENHOOK_API int GenEnableHook(GenHook_t* hook);
extern "C" GENHOOK_API int GenDisableHook(GenHook_t* hook);
extern "C" GENHOOK_API int GenFreeHook(GenHook_t* hook);
extern "C" GENHOOK_API void* GenGetFuncPtr(GenHook_t* hook);

struct Registers_t
{    
  void*   EDI;
  void*   ESI;
  void*   EBP;
  void**  ESP;
  void*   EBX;
  void*   EDX;
  void*   ECX;
  void*   EAX;
};

struct GenHook_t
{
  // VA of patch
  void* address;
  // Prelude to callback
  void* prelude;
  // Pointer to code containing patched instructions
  void* exit;
  // Callback function
  void* callback;
  // Patch length
  int patch_length;
  // Number of stack bytes needed to ret safely
  int stackBytes;

  // Is hook enabled?
  bool enabled;

  // Critical section for operations
  CRITICAL_SECTION cs;

  // Original overwritten bytes
  unsigned char original[32];
  // Patched bytes
  unsigned char patched[32];
};

#endif // GENHOOK_H

