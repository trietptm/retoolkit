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
#include "MSCorEE.h"
#include "GenService.h"

using std::wstring;

wstring FormParameter( wchar_t* dllPath, 
                       wchar_t* appBase, 
                       wchar_t* privatePath, 
                       wchar_t* userData );

void* StartClrBootstrap( wchar_t* dllPath, 
                         wchar_t* userData, 
                         wchar_t* appBase, 
                         wchar_t* privatePath )
{
  // Bind to the CLR runtime..
  ICLRRuntimeHost *pClrHost = NULL;
  HRESULT clrResult = CorBindToRuntimeEx(
    NULL, L"wks", 0, CLSID_CLRRuntimeHost,
    IID_ICLRRuntimeHost, (PVOID*)&pClrHost);

  // Push the big START button shown above
  clrResult = pClrHost->Start();

  // Create parameters to pass into Bootstrap assembly
  // Bootstrapper is responsible for setting up the
  // apppath and privatebin parameters for the AppDomain
  wstring outParam = FormParameter(dllPath, appBase, privatePath, userData);

  // Get path to Bootstrap assembly
  wstring pathBootstrap = PathResolve(L"BootStrapper.dll");

  // Okay, the CLR is up and running in this (previously native) process.
  // Now call a method on our managed C# class library.
  DWORD retCode = 0;
  clrResult = pClrHost->ExecuteInDefaultAppDomain ( 
      pathBootstrap.c_str(),
      L"BootStrapper.Main", 
      L"Initialize",
      outParam.c_str(), 
      &retCode 
  );

  return (void*)retCode;
}

wstring FormParameter(wchar_t* dllPath,
                      wchar_t* appBase, 
                      wchar_t* privatePath,
                      wchar_t* userData ) 
{
  wstring outParam;
  outParam.append(dllPath);
  outParam.append(L"|");
  
  outParam.append(appBase);
  outParam.append(L"|");
  
  outParam.append(privatePath);
  outParam.append(L"|");
  
  outParam.append(userData);

  return outParam;
}
