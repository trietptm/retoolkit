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
#include "ClrInject.h"
#include "GenService.h"
#include "GenAssemblerEx.h"
#include "GenInject.h"

using namespace AsmJit;
using namespace GenHook;
using namespace boost::filesystem;
using namespace std;

HANDLE JitMakeAndRun( AssemblerEx &a, HANDLE targetProc );
void JitCallKernelFunc( Assembler &a, const char* funcName );
void JitSetupBootStrap(Assembler& a, const char* initFunc);

GENHOOK_API HANDLE GenClrInject ( 
    HANDLE targetProc, 
    const wchar_t* dllPath, 
    const wchar_t* appBase, 
    const wchar_t* privatePath, 
    const wchar_t* userData 
)
{
  AssemblerEx a;

  // push [dir_of_GenHook_dll]      |
  // call SetDllDirectoryW          |
  // push [path_to_GenHook.dll]     |
  // call LoadLibraryW              | BootStrapper
  // push "ldr_clr_dll"  (ascii)    |
  // push eax                       |
  // call GetProcAddress            |  
  
  JitSetupBootStrap(a, "ldr_clr_dll"); 

  // GenHook.dll loaded in remote process
  // EAX contains loader function pointer
  
  // -------------------
  // push userdata      |
  // push private path  |
  // push appbase       | call to clr_loader
  // push dllpath       |
  // call eax           |
  // add esp, 0x10      |
  // ret 4              |

  Label* codeBegin = a.newLabel();
  a.jmp(codeBegin);
  Label* paramUserData = asm_wstring(a, userData ? userData : L"");
  Label* paramPrivatePath = asm_wstring(a, privatePath);
  Label* paramAppBase = asm_wstring(a, appBase);
  Label* paramDllPath = asm_wstring(a, dllPath);
  a.bind(codeBegin);
  a.lea(ecx, dword_ptr(paramUserData));
  a.push(ecx);
  a.lea(ecx, dword_ptr(paramPrivatePath));
  a.push(ecx);
  a.lea(ecx, dword_ptr(paramAppBase));
  a.push(ecx);
  a.lea(ecx, dword_ptr(paramDllPath));
  a.push(ecx);
  a.call(eax);
  a.add(esp, 0x10);
  a.ret(4);  

  HANDLE remoteThread = JitMakeAndRun(a, targetProc);
  a.free();
  return remoteThread;
}

GENHOOK_API DWORD GenNativeInject( HANDLE targetProc, const wchar_t* dllPath, const wchar_t* userData )
{
  // setup bootstrap
  AssemblerEx a;
  JitSetupBootStrap(a, "ldr_native_dll"); 
  
  // call loader function
  Label* codeBegin = a.newLabel();
  a.jmp(codeBegin);
  Label* paramUserData = asm_wstring(a, userData ? userData : L"");
  Label* paramDllPath = asm_wstring(a, dllPath);
  a.bind(codeBegin);
  a.lea(ecx, dword_ptr(paramUserData));
  a.push(ecx);
  a.lea(ecx, dword_ptr(paramDllPath));
  a.push(ecx);
  a.call(eax);
  a.add(esp, 0x08);
  a.ret(4);

  HANDLE remoteThread = JitMakeAndRun(a, targetProc);
  return GenSyncInjection(remoteThread);
}

GENHOOK_API DWORD GenSyncInjection( HANDLE remoteThread )
{
  DWORD ecode = 0;
  WaitForSingleObject(remoteThread, INFINITE);
  GetExitCodeThread(remoteThread, &ecode);
  return ecode;
}

////////////////////////////////////////////
// inter-process dll loading functions

extern "C"
GENHOOK_API void * ldr_native_dll(const wchar_t* dllPath, 
                                  const wchar_t* userData) 
{
  HMODULE libHandle = LoadLibrary(dllPath);
  if (NULL == libHandle)
  {
    wchar_t msg[400] = { 0 };
    wsprintf(msg, L"Could not load %s", PathFilename(dllPath).c_str());
    MessageBox(NULL, msg, L"Load error", MB_ICONERROR);
  }
  return libHandle;
}

extern "C"
GENHOOK_API void * ldr_clr_dll(wchar_t* dllPath, 
                               wchar_t* appBase, 
                               wchar_t* privatePath, 
                               wchar_t* userData)
{
  void* retVal = StartClrBootstrap(dllPath, userData, appBase, privatePath);
  return retVal;
}

///////////////////////////////////////////////////
// Auxilliary Jit functions

HANDLE JitMakeAndRun( AssemblerEx &a, HANDLE targetProc ) 
{
  LPTHREAD_START_ROUTINE rcode = (LPTHREAD_START_ROUTINE)a.rmake(targetProc);
  return CreateRemoteThread(targetProc, NULL, 0, rcode, NULL, 0, NULL);
}

void JitCallKernelFunc( Assembler &a, const char* funcName ) 
{
  HMODULE hKernel32 = GetModuleHandle(L"kernel32.dll");
  a.call((void*)GetProcAddress(hKernel32, funcName));
}

void JitSetupBootStrap(Assembler& a, const char* initFunc)
{
  // skip over data section
  // a.int3();  
  Label* L_CodeBegin = a.newLabel();
  a.jmp(L_CodeBegin);

  // begin data section
  wstring dllName         = PathBasename(PathFromModule(dllInstance));
  wstring localDirPath    = PathGetLocal();
  Label* paramSetDllDir   = asm_wstring(a, localDirPath);
  Label* paramLoadLibrary = asm_wstring(a, dllName);
  Label* paramInitFunc    = asm_string(a, initFunc);

  // begin code section
  a.bind(L_CodeBegin);
  a.lea(ecx, dword_ptr(paramSetDllDir));
  a.push(ecx);
  JitCallKernelFunc(a, "SetDllDirectoryW");

  a.lea(ecx, dword_ptr(paramLoadLibrary));
  a.push(ecx);
  JitCallKernelFunc(a, "LoadLibraryW");

  a.lea(ecx, dword_ptr(paramInitFunc));
  a.push(ecx);
  a.push(eax);
  JitCallKernelFunc(a, "GetProcAddress");

  return;
}

