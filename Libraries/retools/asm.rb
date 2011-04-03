# Retoolkit - Scripting-based reverse engineering toolkit for Windows OS'es
# Copyright (C) 2010  James Leskovar
# The full license is available at http://www.gnu.org/licenses/gpl.txt

require 'Retools'
require 'metasm'
include Metasm

module Retools
  
  class AsmFunc
    @@asmFuncs = [] unless defined? @@asmFuncs
    @@cpu = Ia32.new
    @addr = System::IntPtr.Zero
    @size = 0
    @freeAPI = nil
    
    def self.onFinish
      @@asmFuncs.each { |asmFunc|
        asmFunc.dispose
      }
      @@asmFuncs.clear
    end
    
    def initialize(cc, sig, asm)
      sc = Shellcode.new(@@cpu)
      sc.parse(asm)
      sc.assemble
      @size = sc.encoded.virtsize
      @addr = alloc_mem(@size)
          
      hProc = Win32.GetCurrentProcess
      Win32.VirtualProtectEx(hProc, @addr, @size, 0x40)
      
      sc.base_addr = @addr.to_i32
      encodedX86 = sc.encode_string
      codePtr = Memory[@addr]
      0.upto(@size-1) { |i|
        codePtr[i].byte = encodedX86[i]
      }
    
      @freeAPI = Retoolkit::Utilities::FreeApi.new(@addr.to_i32, sig, 'L', cc)
      
      @@asmFuncs << self
    end
    
    def call(*args)
      @freeAPI.call(*args)
    end
    
    def dispose
      System::Runtime::InteropServices::Marshal.FreeHGlobal(@addr)
      p "Disposing asm function #{@addr.to_i32.to_s(16)}"
    end
    
  private
    def alloc_mem(size)
      mem = System::Runtime::InteropServices::Marshal.AllocHGlobal(size)      
      mem
    end
end

end