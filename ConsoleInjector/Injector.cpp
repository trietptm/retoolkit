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

#include <iostream>
#include <vector>
#include <algorithm>
#include <boost/filesystem.hpp>
#include "Injector.h"

using namespace std;
namespace fs = boost::filesystem;

int Injector::_setDebugPriv()
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

void Injector::monitor()
{
  // set private path if we need to
  if (m_clrPrivateBin.empty())
  {
    m_clrPrivateBin.append(L"References;");
    fs::wpath dllPath(m_targetDll);
    wstring dllFileName = dllPath.replace_extension(L"").filename();
    m_clrPrivateBin.append(dllFileName + L"/References;");
    m_clrPrivateBin.append(dllFileName);
  }

  // check if we should force inject
  if (!m_programPath.empty())
  {
    STARTUPINFO si;
    PROCESS_INFORMATION pi;

    ZeroMemory( &si, sizeof(si) );
    si.cb = sizeof(si);
    ZeroMemory( &pi, sizeof(pi) );

    CreateProcess(m_programPath.c_str(),
                   NULL,
                   NULL,
                   NULL,
                   FALSE,
                   CREATE_SUSPENDED,
                   NULL,
                   NULL,
                   &si,
                   &pi);

    HANDLE rThread = GenClrInject (
      pi.hProcess, 
      m_targetDll.c_str(),
      m_appBase.c_str(),
      m_clrPrivateBin.c_str(),
      m_userParam.c_str()
    );

    GenSyncInjection(rThread);
    // HACK: Sleep for x amount of time, until we know injected DLL has fully initialzied
    Sleep(m_programDelay);

    ResumeThread(pi.hThread);
  }
  else
  { // run injection loop
    do 
    {
      wcout << "Watching for process" << endl;

      DWORD pid = NOT_FOUND;
      do 
      {
        Sleep(m_delay);
        for (vector<Monitor*>::size_type i=0; i<m_monitors.size(); ++i)
        {
          pid = m_monitors[i]->GetProcess();
          if (pid != NOT_FOUND)
          {
            vector<DWORD>::iterator it;
            it = find(m_injected.begin(), m_injected.end(), pid);
            if (it != m_injected.end())
              pid = NOT_FOUND;
            else
              break;
          }
        }
      } while (pid == NOT_FOUND);

      wcout << "Injecting...  " << flush;

      HANDLE procHandle = NULL;
      procHandle = OpenProcess(PROCESS_ALL_ACCESS, FALSE, pid);
      GenClrInject (
        procHandle, 
        m_targetDll.c_str(),
        m_appBase.c_str(),
        m_clrPrivateBin.c_str(),
        m_userParam.c_str()
        );
      m_injected.push_back(pid);
      CloseHandle(procHandle);

      wcout << "Done!" << endl;

    } while (m_waitFlag);
  } // m_programPath.empty()
}

Injector::Injector() : 
m_targetDll(L""),
m_waitFlag(false),
m_delay(100)
{
  if (_setDebugPriv() == 0)
  {
    throw runtime_error("Must be administrator to run");
  }
}

Injector::~Injector()
{
  for (vector<Monitor*>::size_type i=0; i<m_monitors.size(); ++i)
    delete m_monitors[i];
}

bool Injector::isProcessRunning( DWORD pid )
{
  HANDLE procHandle = OpenProcess(SYNCHRONIZE, FALSE, pid);
  bool isRunning = WaitForSingleObject(procHandle, 0) == WAIT_TIMEOUT;
  CloseHandle(procHandle);
  return isRunning;
}

void Injector::addWindowMonitor( std::wstring wname )
{
  WindowMonitor* wm = new WindowMonitor(wname);
  m_monitors.push_back(wm);
}

void Injector::addClassMonitor( std::wstring cname )
{
  ClassMonitor* cm = new ClassMonitor(cname);
  m_monitors.push_back(cm);
}

void Injector::addProcMonitor( std::wstring pname )
{
  ProcMonitor* wm = new ProcMonitor(pname);
  m_monitors.push_back(wm);
}

void Injector::addPidMonitor( DWORD pid )
{
  PidMonitor* pm = new PidMonitor(pid);
  m_monitors.push_back(pm);
}

void Injector::setWaitFlag()
{
  m_waitFlag = true;
}

void Injector::setDelay( int delay )
{
  m_delay = (delay < MIN_DELAY_TIME) ? MIN_DELAY_TIME : delay;
}

void Injector::setAppBase( wstring clrAppBase )
{
  m_appBase = fs::system_complete(clrAppBase).string();
}

void Injector::setPrivateBin( wstring clrPrivateBin )
{
  m_clrPrivateBin = clrPrivateBin;
}

void Injector::setUserParam( wstring userParam )
{
  m_userParam = userParam;
}

void Injector::setDllName( std::wstring dllName )
{
  m_targetDll = fs::system_complete(dllName).string();
}

void Injector::setProgramPath( wstring program )
{
  m_programPath = program;
}

void Injector::setProgramDelay( int delay )
{
  m_programDelay = delay;
}