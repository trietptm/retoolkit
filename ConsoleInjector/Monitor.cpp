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

#include "Monitor.h"
#include <tchar.h>
#include <Psapi.h>
#pragma comment(lib, "psapi.lib")

using std::wstring;

wstring lowerstr(wstring s)
{
  for (wstring::size_type i=0; i<s.size(); ++i)
    s[i] = _totlower(s[i]);
  return s;
}

ProcMonitor::ProcMonitor( std::wstring procName ) : m_procName(lowerstr(procName))
{
}

DWORD ProcMonitor::GetProcess()
{
  DWORD pids[100] = { 0 };
  DWORD count = 0;
  DWORD targetPid = NOT_FOUND;
  EnumProcesses(pids, sizeof(pids), &count);

  count /= sizeof(DWORD);
  for (DWORD i = 0; i < count; ++i)
  {
    wchar_t procName[1024] = { 0 };
    HANDLE procHandle = OpenProcess(PROCESS_ALL_ACCESS, FALSE, pids[i]);
    GetModuleBaseName(procHandle, NULL, procName, sizeof(procName));
    CloseHandle(procHandle);
    
    if (m_procName == lowerstr(procName))
    {
      targetPid = pids[i];
      break;
    }
  }
  
  return targetPid;
}

ClassMonitor::ClassMonitor( std::wstring cname ) : m_className(cname)
{

}

DWORD ClassMonitor::GetProcess()
{
  HWND targetWin = FindWindow(m_className.c_str(), NULL);
  DWORD pid = NOT_FOUND;
  GetWindowThreadProcessId(targetWin, &pid);
  return pid;
}

DWORD PidMonitor::GetProcess()
{
  return m_pid;
}

PidMonitor::PidMonitor( DWORD pid ) : m_pid(pid)
{

}

WindowMonitor::WindowMonitor( std::wstring wname ) : m_windowName(wname)
{

}

DWORD WindowMonitor::GetProcess()
{
  HWND targetWin = FindWindow(NULL, m_windowName.c_str());
  DWORD pid = NOT_FOUND;
  GetWindowThreadProcessId(targetWin, &pid);
  return pid;
}