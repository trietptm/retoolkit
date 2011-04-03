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
using Retoolkit.Utilities;
using Retoolkit.Interfaces;

namespace Retoolkit.Scripting
{
  #region Interfaces
  interface ILibraryPathResolver
  {
    string[] Paths { get; }
  }
  #endregion
  
  #region Classes
  class LibraryPathFile : ILibraryPathResolver
  {
    #region Fields
    IPathResolver m_pathProvider;
    string m_libFileName;
    #endregion

    #region Constructor
    public LibraryPathFile(IPathResolver pathProvider, string fileName)
    {
      m_pathProvider = pathProvider;
      m_libFileName = fileName;
    }
    #endregion

    #region ILibraryPathResolver Members

    public string[] Paths
    {
      get
      {
        var paths = new List<string>();

        // add appbase
        var appBase = AppDomain.CurrentDomain.BaseDirectory;
        paths.Add(m_pathProvider.Resolve(appBase));

        // add private bin paths
        var binPaths = AppDomain.CurrentDomain.SetupInformation.PrivateBinPath;
        if (!String.IsNullOrEmpty(binPaths))
        {
          var fixedPaths = from path in binPaths.Split(';')
                           select m_pathProvider.ResolveBase(appBase, path);
          paths.AddRange(fixedPaths);
        }

        // add paths from path.txt
        var libFilePath = m_pathProvider.Resolve(m_libFileName);
        if (File.Exists(libFilePath))
        {
          var pathLines = from p in File.ReadAllLines(libFilePath)
                          select m_pathProvider.Resolve(p);
          paths.AddRange(pathLines);
        }

        return paths.Distinct().ToArray();
      }
    }

    #endregion
  }
  #endregion
}
