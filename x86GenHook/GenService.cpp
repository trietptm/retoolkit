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
#include "GenService.h"
using namespace std;
using namespace boost::filesystem;



wstring PathResolve(wstring p1, wstring basePath/* = ""*/)
{
  wpath fullPath(p1);
  if (!fullPath.is_complete()) 
  {
    if (basePath.empty()) 
      basePath = PathGetLocal();    

    wpath local_path(basePath);
    fullPath = complete(fullPath, local_path);
  }
  return fullPath.string();
}

bool AuxIsClrImage( wstring path )
{
  const int VER_LENGTH = 256;
  wchar_t versionBuffer[VER_LENGTH];
  DWORD outLength;
  return (0 == GetFileVersion(path.c_str(), versionBuffer, VER_LENGTH, &outLength));
}

wstring PathParentDir(wstring p) 
{
  wpath _path(p);
  return _path.parent_path().string();
}

wstring PathBasename( wstring p ) 
{
  wpath _path(p);
  wstring base_name = (_path.replace_extension(L"").filename());
  return base_name;
}

wstring PathFilename( wstring p ) 
{
  wpath _path(p);
  wstring base_name = (_path.filename());
  return base_name;
}

wstring PathFromModule(HMODULE module) 
{
  wchar_t buf[MAX_PATH];
  GetModuleFileName(module, buf, MAX_PATH);
  return buf;
}

wstring PathGetLocal()
{
  return PathParentDir(PathFromModule(dllInstance));
}

string AuxUniToAscii(const wchar_t* ucode_str) 
{
  int len = wcslen(ucode_str) + 1;
  char* buf = new char[len];
  memset(buf, 0, len);
  WideCharToMultiByte(CP_ACP, 0, ucode_str, -1, buf, len, NULL, NULL);
  string r = buf;
  delete [] buf;
  return r;
}

wstring AuxAsciiToUni( const char* ascii_str ) 
{
  int len = strlen(ascii_str) * 2 + 2;
  wchar_t* buf = new wchar_t[len];
  memset(buf, 0, len);
  MultiByteToWideChar(CP_ACP, 0, ascii_str, -1, buf, len);
  wstring r = buf;
  delete [] buf;
  return r;
}
