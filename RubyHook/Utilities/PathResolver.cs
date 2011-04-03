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
using System.Text;
using System.IO;
using Retoolkit.Interfaces;

namespace Retoolkit.Utilities
{
  public class DefaultPathResolver : IPathResolver
  {
    #region IPathProvider Members

    public DefaultPathResolver(string rootDir)
    {
      BaseDirectory = rootDir;
    }

    public string BaseDirectory
    {
      get;
      set;
    }

    public string Resolve(string absFileName)
    {
      return ResolveBase(BaseDirectory, absFileName);
    }

    public string ResolveBase(string basePath, string fileName)
    {
      return Path.GetFullPath(Path.Combine(basePath, fileName)).Replace('\\', '/');
    }

    #endregion
  }
}
