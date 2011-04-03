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

#ifndef STDAFX_H
#define STDAFX_H

#define WIN32_LEAN_AND_MEAN

// Window libs
#include <windows.h>
#include <Psapi.h>
#include <mscoree.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <string>
#include <vector>

// Library imports
#pragma comment(lib, "mscoree.lib")

// Vendor libs
#include <boost/filesystem.hpp>

// Site libs
#include "libdis.h"
#include "AsmJit.h"

#define OFFSET_OF(struct, field) ((SysUInt) ((const UInt8*) &((const struct*)0x1)->field) - 1)

extern HMODULE dllInstance;
extern ICLRRuntimeHost * pClrHost;

#endif // STDAFX_H
