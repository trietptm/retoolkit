require 'utils'
include System
include System::Runtime

module Retools

class TypedData
end

class Pointer
  Marshal = InteropServices::Marshal  
  attr_accessor :base_addr
  
  def initialize(base_addr)
    @base_addr = base_addr.to_intptr
  end
  
  def self.[](addr)
    self.new(addr)
  end
  
  ###############################
  # offset mutation
  ###############################
  def offset(n)
    self.class.new(@base_addr + n)
  end
  
  def +(offs)
    offset(offs)
  end
  
  def -(offs)
    offset(-offs)
  end
  
  ###############################
  # array methods
  ###############################
  def [](n)
    raise 'bad array length' if n <= 0
    MemoryArray.new(self, n)
  end
  
  ###############################
  # writer methods
  ###############################
  def byte=(n)
    Marshal.write_byte(@base_addr, n)
  end
  
  def int16=(n)
    Marshal.write_int16(@base_addr, n)
  end
  
  def int32=(n)
    Marshal.write_int32(@base_addr, n)
  end
  
  def int_ptr=(n)
    Marshal.write_int_ptr(@base_addr, n)
  end
  
  def int64=(n)
    Marshal.write_int64(@base_addr, n)
  end
  
  ###############################
  # reader methods
  ###############################
  def byte()
    Marshal.read_byte(@base_addr)
  end
  
  def int16()
    Marshal.read_int16(@base_addr)
  end
  
  def int32()
    Marshal.read_int32(@base_addr)
  end
  
  def int_ptr()
    Marshal.read_int_ptr(@base_addr)
  end
  
  def int64()
    Marshal.read_int64(@base_addr)
  end
end

class MemoryArray
  Marshal = InteropServices::Marshal  
  
  attr_accessor :base_ptr, :length
  def initialize(ptr, n)
    @base_ptr = ptr
    @length = n
  end
  
  def byte()
    dest = System::Array.of(Byte).new(@length)
    Marshal.copy(@base_ptr.base_addr, dest, 0, @length)
    dest
  end
  
  def int16()
    dest = System::Array.of(Int16).new(@length)
    Marshal.copy(@base_ptr.base_addr, dest, 0, @length)
    dest
  end
  
  def int32()
    dest = System::Array.of(Int32).new(@length)
    Marshal.copy(@base_ptr.base_addr, dest, 0, @length)
    dest
  end
  
  def int_ptr()
    dest = System::Array.of(IntPtr).new(@length)
    Marshal.copy(@base_ptr.base_addr, dest, 0, @length)
    dest
  end
  
  def int64()
    dest = System::Array.of(Int64).new(@length)
    Marshal.copy(@base_ptr.base_addr, dest, 0, @length)
    dest
  end  
end

end
