# Retoolkit - Scripting-based reverse engineering toolkit for Windows OS'es
# Copyright (C) 2010  James Leskovar
# The full license is available at http://www.gnu.org/licenses/gpl.txt

include Retoolkit::Utilities

module Retools
  class Memory
    attr_accessor :base_addr
    CLRMarshal = System::Runtime::InteropServices::Marshal unless defined? CLRMarshal
    
    def self.[](ptr)
      addr = ptr.to_intptr
      Memory.new(addr)
    end
    
    def copy(size)
      dst = ' ' * size
      0.upto(size - 1) { |n|
        mem = self.class.new(@base_addr + n)
        dst[n] = mem.byte
      }
      dst
    end
    
    ##############################
    # peek/poke methods
    
    def mem()
      self.class.new(self.ptr)
    end
    
    def u8()
      val = CLRMarshal.ReadByte(@base_addr)
      
    end
    
    def u8=(v)
    end
    
    def s8()
    end
    
    def s8=(v)
    end
    
    def u16()
    end
    
    def u16=(v)
    end
    
    def s16()
    end
    
    def s16=(v)
    end
    
    def u32()
    end
    
    def u32=(v)
    end
    
    def s32()
    end
    
    def s32=(v)
    end
    
    def u64()
    end
    
    def u64=(v)
    end
    
    def s64()
    end
    
    def s64=(v)
    end
    
    def f32()
    end
    
    def f32=(v)
    end
    
    def f64()
    end
    
    def f64=(v)
    end
    
    def dword()
      val = CLRMarshal.ReadInt32(@base_addr)
      Integer(Helpers.ToUInt32(val))
    end
    
    def dword=(val)
      CLRMarshal.WriteInt32(@base_addr, val.to_i32)
      Integer(Helpers.ToUInt32(val))
    end
    
    def word()
      val = CLRMarshal.ReadInt16(@base_addr)
      Integer(Helpers.ToUInt32(val))
    end
    
    def word=(val)
      CLRMarshal.WriteInt16(@base_addr, val.to_i32)
      Integer(Helpers.ToUInt32(val))
    end
    
    def byte()
      val = CLRMarshal.ReadByte(@base_addr)
      Integer(Helpers.ToUInt32(val))
    end
    
    def byte=(val)
      CLRMarshal.WriteByte(@base_addr, val.to_i32)
      Integer(Helpers.ToUInt32(val))
    end
    
    def ascii()
      CLRMarshal.PtrToStringAnsi(self.ptr)
    end
    
    def ptr()
      CLRMarshal.ReadIntPtr(@base_addr)
    end
    
    def ptr=(val)
      CLRMarshal.WriteIntPtr(@base_addr, val.to_intptr)
      val.to_intptr
    end
    
    ##############################
    # iteration
    def each_b(n, &block)    
    end
    
    def each_b2z(&block)
    end
    
    def each_w(n, &block)
    end
    
    def each_w2z(&block)
    end
    
    def each_d(n, &block)
    end
    
    def each_d2z(&block)
    end
    
    ##############################
    # offset methods
    def +(offs)
      addr = @base_addr + offs
      self.class.new(addr)
    end
    
    def -(offs)
      addr = @base_addr + (-offs)
      self.class.new(addr)
    end
    
    def [](offs)
      self.class[@base_addr + offs]
    end
    
    ##############################
    # standard
      
    def to_s
      self.inspect
    end
    
    def inspect
      "Memory: <#{self.to_s.upcase}>"
    end
    
    private :initialize
    
    def initialize(base_addr)
      @base_addr = base_addr
    end
    
  end

  class StackArgs

    def self.[](regs)
      addr = 0
      if (regs.respond_to? :value) then
        regs = regs.value
      end
      StackArgs.new(regs.esp)
    end

    def initialize(esp)    
      @base_mem = Memory[esp]
    end
    
    def arg(n)
      @base_mem[n*4]
    end
    
    def [](n)
      self.arg(n)
    end
    
    def retaddr()
      arg(0).dword
    end
  end
end