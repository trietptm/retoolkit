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

#include "StdAfx.h"
#include "GenAssemblerEx.h"

using namespace AsmJit;

namespace GenHook
{
  AssemblerEx::AssemblerEx(void) : m_base(0)
  {
  }

  AssemblerEx::~AssemblerEx(void)
  {
  }

  // patched version of AsmJit::Assembler::relocCode
  // will now relocate code based upon given base 
  // address
  void AssemblerEx::relocCode( void* _dst )
  {
    SysUInt base = (m_base == 0) ? (SysUInt)_dst : m_base;

    // Copy code
    UInt8* dst = reinterpret_cast<UInt8*>(_dst);
    memcpy(dst, _buffer.data(), codeSize());

    // Reloc
    SysInt i, len = _relocData.length();

    for (i = 0; i < len; i++)
    {
      const RelocData& r = _relocData[i];
      SysInt val;

      // Be sure that reloc data structure is correct
      ASMJIT_ASSERT((SysInt)(r.offset + r.size) <= codeSize());

      switch(r.type)
      {
      case RelocData::ABSOLUTE_TO_ABSOLUTE:
        val = (SysInt)(r.address);
        break;

      case RelocData::RELATIVE_TO_ABSOLUTE:
        val = (SysInt)(base + r.destination);
        break;

      case RelocData::ABSOLUTE_TO_RELATIVE:
      case RelocData::ABSOLUTE_TO_RELATIVE_TRAMPOLINE:
        val = (SysInt)(r.address) - (SysInt)(base + r.offset + r.size);
        break;

      default:
        ASMJIT_ASSERT(0);
      }

      switch(r.size)
      {
      case 4:
        *reinterpret_cast<Int32*>(dst + r.offset) = (Int32)val;
        break;

      case 8:
        *reinterpret_cast<Int64*>(dst + r.offset) = (Int64)val;
        break;

      default:
        ASMJIT_ASSERT(0);
      }
    }
  }

  void* AssemblerEx::make(MemoryManager* memoryManager, UInt32 allocType)
  {
    void* memRet = NULL;
    if (codeSize() > 0) 
    {
      MemoryManager* memmgr = memoryManager;
      memmgr = (memmgr == NULL) ? MemoryManager::global() : memmgr;
      memRet = memmgr->alloc(codeSize(), allocType);
      if (memRet != NULL) 
        relocCode(memRet);
    }
    return memRet;
  }

  void* AssemblerEx::rmake( HANDLE target, void* destination )
  {
    int size = codeSize();
    if (NULL == destination) {
      destination = VirtualAllocEx (
        target,
        NULL, 
        size, 
        MEM_COMMIT | MEM_RESERVE, 
        PAGE_EXECUTE_READWRITE
        );
    }
    setBaseAddress((SysUInt)destination);
    void* code = make();
    WriteProcessMemory(target, destination, code, size, NULL);
    MemoryManager::global()->free(code);
    return destination;
  }

  void AssemblerEx::embedFarAddr( SysUInt addr )
  {
    RelocData r = {0};
    r.type = RelocData::ABSOLUTE_TO_RELATIVE;
    r.address = (void*)addr;
    r.size = sizeof(SysUInt);
    r.offset = codeSize();
    _relocData.append(r);
    _emitInt32(0);
  }

  void AssemblerEx::setBaseAddress( SysUInt addr )
  {
    m_base = addr;
  }

  void AssemblerEx::clear()
  {
    m_base = 0;
    Assembler::clear();
  }

  //////////////////////////////////////////////////////////////////////////////
  // An Assembler class which can copy and relocate existing x86 instructions
  //////////////////////////////////////////////////////////////////////////////

  CopierAssembler::CopierAssembler( void* baseAddr ) : m_offset(0), m_base(baseAddr), m_error(false)
  {
  }

  int get_branch_dest( x86_insn_t& inst )
  {
    x86_op_t* op;
    if (op = x86_get_branch_target(&inst)) {
      if (op->datatype == op_byte)
        return (inst.addr + inst.size) + op->data.relative_near;
      else
        return (inst.addr + inst.size) + op->data.relative_far;
    } else {
      return 0;
    }
  }


  void CopierAssembler::consume()
  {
    if (m_error) return;

    x86_insn_t inst;

    m_offset += x86_disasm(((unsigned char*)m_base)+m_offset, 20, ((uint32_t)m_base) + m_offset, 0, &inst);
    if (x86_insn_is_valid(&inst) == false) {
      m_error = true;
      return;
    }

    Assembler::ensureSpace();
    if (inst.group == insn_controlflow) {
      uint32_t dest = get_branch_dest(inst);
      if (inst.bytes[0] == 0xE8 || inst.bytes[0] == 0xE9){            
        Assembler::_emitByte(inst.bytes[0]);
      } else {
        Assembler::_emitByte(0x0F);
        if (inst.bytes[0] != 0x0F)
          Assembler::_emitByte(inst.bytes[0] + 0x10);
        else
          Assembler::_emitByte(inst.bytes[1]);
      }
      AssemblerEx::embedFarAddr(dest);
    } else {
      Assembler::data(inst.bytes, inst.size);
    }

//     char buf[256];
//     memset(buf, 0, sizeof(buf));
//     x86_format_insn(&inst, buf, sizeof(buf), native_syntax);
//     m_last = buf;

    x86_oplist_free(&inst);
  }

  void* CopierAssembler::getBase()
  {
    return m_base;
  }

  void CopierAssembler::clear()
  {
    m_error = false;
    m_base = 0;
    m_offset = 0;
    AssemblerEx::clear();
  }

  std::string CopierAssembler::getLastInst()
  {
    return "";
  }
} // namespace GenHook
