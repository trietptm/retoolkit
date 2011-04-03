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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace BootStrapper
{
  public class Main
  {
    #region Constants
    public const string APATH_FULL_PATH_NAME = "ASM_PATH";
    public const string APATH_DIR_NAME = "ASM_DIRECTORY";
    public const string PARAMETER_NAME = "USER_DATA";
    public const string INIT_METHOD = "Initialize";
    #endregion

    /// <summary>
    /// Invoked from native CLR bootstrapper
    /// </summary>
    /// <param name="nativeParams">
    /// Compacted arguments from CLR bootstrapper in the form of 
    /// path|appbase|private_binpath|user_data
    /// </param>
    /// <returns>Return code for native CLR bootstrapper</returns>
    static int Initialize(string nativeParams)
    {
      // obtain parameters in the form: 
      var tokens = nativeParams.Split('|');

      // setup appdomain to use given ApplicationBase and PrivateBinPath
      var ads = new AppDomainSetup();
      ads.ApplicationBase = tokens[1];
      ads.PrivateBinPath = tokens[2];
      var asmPath = tokens[0];
      var userData = tokens[3];
      var adName = String.Format (
        "AppDomain_{0}", 
        Path.GetFileNameWithoutExtension(asmPath)
      );
      var appDomain = AppDomain.CreateDomain(adName, null, ads);
      
      // Set some parameters which will be global to the new appdomain
      appDomain.SetData(APATH_FULL_PATH_NAME, asmPath);
      appDomain.SetData(APATH_DIR_NAME, Path.GetDirectoryName(asmPath));
      appDomain.SetData(PARAMETER_NAME, userData);
      appDomain.DoCallBack(LoadAssemblyInAppDomain);

      return 0;
    }

    static void LoadAssemblyInAppDomain()
    {
      try
      {
        // Load target assembly into newly created appdomain
        var appDom = AppDomain.CurrentDomain;
        var fullAsmPath = (string)appDom.GetData(APATH_FULL_PATH_NAME);

        // launch new STA thread to invoke the initializer
        string param = (string)appDom.GetData(PARAMETER_NAME);
        var appDomThread = new Thread(() =>
        {
          appDom.ExecuteAssembly(fullAsmPath);
        });
        appDomThread.SetApartmentState(ApartmentState.STA);
        appDomThread.Start();
      }
      catch (Exception ex)
      {
        MessageBox.Show(
          ex.Message,
          "Error",
          MessageBoxButtons.OK,
          MessageBoxIcon.Error
        );
      }
    }
  }
}
