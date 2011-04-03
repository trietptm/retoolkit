# Retoolkit - Scripting-based reverse engineering toolkit for Windows OS'es
# Copyright (C) 2010  James Leskovar
# The full license is available at http://www.gnu.org/licenses/gpl.txt

require 'utils'

module Retools

  module Hooker
    Struct.new('HookInfo', :addr, :handle, :callback, :args, :tramp)
    HookAddressMap = {} unless defined? HookAddressMap
    
    class << self
    
      def onFinish()
        p 'Cleaning hooks'
        self.freeall 
      end
    
      def freeall()
        HookAddressMap.keys.each { |k|
          hookStruct = HookAddressMap[k]
          p "Unhooking #{hookStruct.addr.to_i32.to_s(16)}"
          hdl = hookStruct.handle
          NativeGenHook::GenHook.GenFreeHook(hdl)
        }
        HookAddressMap = {}
      end
      
      def args_to_addr(*args)
        addr = 0 
        stack = 0
        
        if (args.first.kind_of? String) then
          addr = Win32.GetAddress(args[0], args[1])
        elsif args.first.respond_to? :to_intptr then
          addr = args.first.to_intptr
        else
          raise 'Couldn\'t get hook address'
        end
        
        if (args.last.kind_of? Hash) then
          stack = args.last[:stack] or 0
        end       
        
        return addr.to_intptr, stack
      end
      
      def hook_obj(handle)
        HookAddressMap[handle]
      end
    
      def hook(*args, &block)
        addr, stk = args_to_addr(*args)
        
        if (HookAddressMap.has_key?(addr)) then
          raise "Hook already exists at #{addr}"
        end
        
        callback = NativeGenHook::HookCallback.new { |a,b,c|
          retVal = true
          begin  
            retVal = block.call(a,b,c)
          rescue
            notifyError($!)
          end  
          retVal
        }
        
        handle = NativeGenHook::GenHook.GenCreateHook(
          addr, 
          callback, 
          stk
        )
        
        # create hook struct
        hk = Struct::HookInfo.new(addr, handle, callback)
        hk.tramp = NativeGenHook::GenHook.GenGetFuncPtr(handle)
        HookAddressMap[addr] = hk
        hk
      end    
    end
  end
  
end
