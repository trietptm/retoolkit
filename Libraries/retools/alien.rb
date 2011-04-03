# Retoolkit - Scripting-based reverse engineering toolkit for Windows OS'es
# Copyright (C) 2010  James Leskovar
# The full license is available at http://www.gnu.org/licenses/gpl.txt

include System::Collections::Generic
include NativeGenHook

module Retools
  module Alien
    class ::String
      include System::Runtime::InteropServices  
      def marshal_native()
        Marshal::StringToHGlobalAnsi(self)
      end
      
      def marshal_free(ptr)
        ss = Marshal::PtrToStringAnsi(ptr, self.size)
        Marshal::FreeHGlobal(ptr)
        self[0, self.size] = ss
      end
    end
  
    class ::Bignum
      def marshal_native()
        self.to_intptr
      end
    end
  
    class ::Fixnum  
      def marshal_native()
        self.to_intptr
      end
    end
  
    #SLOW! Only for prototyping purposes only
    # Fixing: use ILGenerator, use calli instruction  
    def self.invoke(addr, *args)
      has_regs = args.last.instance_of? Hash
      regs = args.last if has_regs
      
      # handle stack arguments
      stkargs = has_regs ? args[0..-2] : args
      largs = List.of(System::IntPtr).new
      ptrvars = {}
      stkargs.each { |arg|
        argptr = arg.marshal_native
        largs << argptr    
        ptrvars[argptr] = arg
      }
      
      # handle register arguments
      if has_regs then
        rs = X86Registers.new
        regs.keys.each { |reg|
          rsym = "#{reg}="
          argptr = regs[reg].marshal_native
          rs.send rsym, argptr
        }
        regs = List.of(X86Registers).new
        regs << rs
        regs = regs.to_array
      end
      
      # do invoke
      eax, edx = NativeInvoke.GenNativeInvoke(
        addr.to_intptr,
        regs, 
        largs.size, 
        largs.to_array, 
        0
      )
  
      # free intptrs
      ptrvars.keys.each { |ptr|    
        var = ptrvars[ptr]
        if var.respond_to? :marshal_free then
          var.marshal_free(ptr)
        end     
      }
      
      return eax, edx
    end
  end
end