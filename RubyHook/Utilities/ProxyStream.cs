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

using System.IO;

namespace Retoolkit.Utilities
{
  public delegate int ReadStream(byte[] buffer, int offset, int count);
  public delegate void WriteStream(byte[] buffer, int offset, int count);

  internal sealed class ProxyStream : MemoryStream
  {
    #region Fields
    WriteStream m_writer;
    ReadStream m_reader;
#endregion

    #region Properties
    public WriteStream Writer
    {
      get { return m_writer; }
      set { m_writer = value; }
    }

    public ReadStream Reader
    {
      get { return m_reader; }
      set { m_reader = value; }
    }
    #endregion

    public ProxyStream() : this(null, null)
    {
    }

    public ProxyStream(WriteStream writer, ReadStream reader)
    {
      m_writer = writer ?? base.Write;
      m_reader = reader ?? base.Read;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      return m_reader(buffer, offset, count);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      m_writer(buffer, offset, count);
    }
  }
}
