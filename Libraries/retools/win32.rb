# Retoolkit - Scripting-based reverse engineering toolkit for Windows OS'es
# Copyright (C) 2010  James Leskovar
# The full license is available at http://www.gnu.org/licenses/gpl.txt

require 'Win32API'
 
module Retools
  module Win32 

    PAGE_EXECUTE_READWRITE = 0x40 unless defined? PAGE_EXECUTE_READWRITE

    class << self
      def GetProcAddress(hMod, szExport)
        gpa = Win32API.new('kernel32', 'GetProcAddress', 'LP', 'L')
        gpa.call(hMod, szExport)
      end
      
      def GetModuleHandle(szModName)
        gmh = Win32API.new('kernel32', 'GetModuleHandleA', 'P', 'L')
        gmh.call(szModName)
      end
      
      def GetAddress(szModName, szExport)
        GetProcAddress(GetModuleHandle(szModName), szExport)
      end
      
      def VirtualProtectEx(hProc, lpAddr, dwSize, flNewProt)
        oldProt = ' ' * 4
        vpe = Win32API.new('kernel32', 'VirtualProtectEx', 'LLLLP', 'L')
        success = vpe.call(hProc, lpAddr.to_i32, dwSize, flNewProt, oldProt)
        oldProt = oldProt.unpack('i')
        return success, oldProt
      end
      
      def GetCurrentProcess()
        gcp = Win32API.new('kernel32', 'GetCurrentProcess', '', 'L')
        gcp.call()
      end
    end
    
  end
end
