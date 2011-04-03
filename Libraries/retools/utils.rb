# Retoolkit - Scripting-based reverse engineering toolkit for Windows OS'es
# Copyright (C) 2010  James Leskovar
# The full license is available at http://www.gnu.org/licenses/gpl.txt

#############################################
# Miscellanious
def notifyError(e)
  $onError.invoke(e.message, e.backtrace)
end

################################################
# Thread enhancements
class Thread
  class << self
    alias_method :old_start, :start
    alias_method :old_new, :new
    
    def new(*args, &block)
      old_new(*args) {
        begin
          block.call(*args)
        rescue
          notifyError($!)
        end
      }
    end
    
    def start(*args, &block)
      old_start {
        begin
          block.call(*args)
        rescue
          notifyError($!)
        end  
      }
    end
  end
end  

################################################
# Compatibility fixes for IronRuby 0.9.2
# * Adds ability to sort hashes with symbols (1.9.1)
class Hash  
  def sort
    skeys = self.keys
    Array.new(skeys.size) { |k|
      key = skeys[k]
      val = self[key]
      [key , val]
    }
  end
end


########################################
# Extension method for class String:
#  - adds to_intptr for string
#    NB: may be moved around by GCtor in some cases
#        eg. making a p/invoke call or calli MSIL into
#            native code flags objects for GC
#        Make sure to keep a reference to the string
#        available, and use only within block scope
#        eg. stk = StackArgs[r]; t = 'test'; stk[1].ptr = t
class String
  def to_intptr
    Retoolkit::Utilities::Helpers.GetBytePtr(self)
  end
end


#########################################
# Extension methods for simplifying marshalling
# * adds #to_i32 and #to_u32 to Bignum and Fixnum
# * adds #to_intptr to Bignum and Fixnum
# * adds #+, #- offset methods for System::IntPtr
# * adds #to_i32 for System::IntPtr
class Bignum
  def to_i32
    val = self.ToUInt32
    Retoolkit::Utilities::Helpers.ToInt32(val)    
  end
  
  def to_u32
    self.ToUInt32
  end
  
  def to_intptr
    self.to_i32.to_intptr
  end
end

class Fixnum
  def to_i32
    self
  end
  
  def to_u32
    Retoolkit::Utilities::Helpers.ToUInt32(self)
  end
  
  def to_intptr
    System::IntPtr.new self
  end
end

class System::IntPtr
  def to_i32
    self.to_int32
  end
  
  def to_intptr
    self
  end
  
  def +(offs)
    addr = self.to_int32 + offs
    addr.to_intptr
  end
  
  def -(offs)
    addr = self.to_int32 - offs
    addr.to_intptr
  end
end
