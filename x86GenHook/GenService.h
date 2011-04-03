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

#ifndef GENSERVICE_H
#define GENSERVICE_H

#include <string>
#include "AsmJit.h"
#include "GenConfig.h"

///////////////////////////////////
// Utility functions
/////////////////////////////////////////////////////////

std::wstring PathParentDir( std::wstring p );
std::wstring PathFromModule( HMODULE module );
std::wstring PathBasename( std::wstring path );
std::wstring PathFilename( std::wstring path );
std::wstring PathGetLocal();
std::wstring PathResolve(std::wstring p1, std::wstring basePath = L"");

std::wstring AuxAsciiToUni( const char* ascii_str );
std::string  AuxUniToAscii( const wchar_t* ucode_str );
bool         AuxIsClrImage( std::wstring path );


//////////////////////////
// Assembler JIT helpers
////////////////////////////////////////////////////
inline AsmJit::Label* asm_string(AsmJit::Assembler& a, const std::string& data) 
{
  a.align(4);
  AsmJit::Label* lbl = a.newLabel();
  a.bind(lbl);
  a.data(data.c_str(), data.length()); a.db(0);
  return lbl;
}

inline AsmJit::Label* asm_wstring(AsmJit::Assembler& a, const std::wstring& data) 
{
  a.align(4);
  AsmJit::Label* lbl = a.newLabel();
  a.bind(lbl);
  a.data(data.c_str(), data.length() * 2); a.dw(0);
  return lbl;
}

#endif // GENSERVICE_H
