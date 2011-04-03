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
#include <Windows.h>

HMODULE dllInstance = NULL;
ICLRRuntimeHost *pClrHost = NULL;

extern "C"
void __declspec(dllexport) exported_func1(int a)
{ }

DWORD WINAPI thread_func(LPVOID v)
{
  while (true)
  {
    exported_func1(42);
  }
  return NULL;
}

extern "C"
void __declspec(dllexport) thread_test()
{
  CreateThread(NULL, 0, thread_func, NULL, 0, NULL);
}

BOOL WINAPI DllMain(_In_ void * _HDllHandle, _In_ unsigned _Reason, _In_opt_ void * _Reserved)
{
    if (_Reason == DLL_PROCESS_ATTACH)
        dllInstance = (HMODULE)_HDllHandle;
    return TRUE;
}
