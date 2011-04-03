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

#include <Windows.h>
#include "Utils.h"

int SetDebugPriv()
{
  HANDLE TokenHandle;
  LUID lpLuid;
  TOKEN_PRIVILEGES NewState;

  if(!OpenProcessToken(GetCurrentProcess(), TOKEN_ALL_ACCESS, &TokenHandle))
  {
    //failed
    return 0;
  }

  if(!LookupPrivilegeValue(NULL, L"SeDebugPrivilege" , &lpLuid))
  {
    //failed
    CloseHandle(TokenHandle);
    return 0;
  }

  NewState.PrivilegeCount = 1;
  NewState.Privileges[0].Luid = lpLuid;
  NewState.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

  if(!AdjustTokenPrivileges(TokenHandle, FALSE, &NewState, sizeof(NewState), NULL, NULL))
  {
    //failed
    CloseHandle(TokenHandle);
    return 0;
  }

  CloseHandle(TokenHandle);
  return 1;
}

