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

#ifndef INJECTOR_H
#define INJECTOR_H

#include <Windows.h>
#include <iostream>
#include <vector>
#include <string>
#include "GenInject.h"
#include "Monitor.h"

const int MIN_DELAY_TIME = 100;

class Injector
{
  bool m_waitFlag;
  int m_delay;
  std::vector<Monitor*> m_monitors;
  std::vector<DWORD> m_injected;
  std::wstring m_targetDll;
  std::wstring m_appBase;
  std::wstring m_clrPrivateBin;
  std::wstring m_userParam;  
  std::wstring m_programPath;
  int m_programDelay;
private:
  int _setDebugPriv();

public:
  Injector();
  virtual ~Injector();

  bool isProcessRunning(DWORD pid);

  void monitor();

  void addWindowMonitor(std::wstring wname);
  void addClassMonitor(std::wstring cname);
  void addProcMonitor(std::wstring pname);
  void addPidMonitor(DWORD pid);

  void setWaitFlag();
  void setDelay( int delay );

  void setAppBase( std::wstring clrAppBase );
  void setPrivateBin( std::wstring clrPrivateBin );
  void setUserParam( std::wstring scriptName );
  void setDllName( std::wstring dllName );
  void setProgramPath( std::wstring program );
  void setProgramDelay( int delay );
};

#endif // INJECTOR_H