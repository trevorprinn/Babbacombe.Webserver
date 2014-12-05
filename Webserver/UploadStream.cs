#region Licence
/*
    Babbacombe.Webserver
    https://github.com/trevorprinn/Babbacombe.Webserver
    Copyright © 2014 Babbacombe Computers Ltd.

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301
    USA
 */
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Babbacombe.Webserver {

    /// <summary>
    /// The stream passed to a FileUploaded event handler containing the file contents.
    /// </summary>
    internal class UploadStream : Stream {
        private TextBinaryReader _reader;
        private string _delimiter;

        public UploadStream(TextBinaryReader reader, string delimiter) {
            _reader = reader;
            _delimiter = delimiter;
        }

        public override bool CanRead { get { return true; } }

        public override bool CanSeek { get { return false; } }

        public override bool CanWrite { get { return false; } }

        /// <summary>
        /// Reads to the end of the stream (so the position in the TextBinaryReader is at 
        /// the start of the next data item.
        /// </summary>
        public override void Flush() {
            byte[] buf = null;
            while (!EndOfStream) {
                if (buf == null) buf = new byte[4096];
                Read(buf, 0, 4096);
            }
        }

        public override long Length {
            get { throw new NotImplementedException(); }
        }

        public override long Position {
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            if (EndOfStream) return 0;
            bool delimiterReached;
            int read = _reader.ReadBinary(buffer, offset, count, _delimiter, out delimiterReached);
            if (read == 0 || delimiterReached) EndOfStream = true;
            return read;
        }

        public bool EndOfStream { get; private set; }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotImplementedException();
        }

        public override void SetLength(long value) {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotImplementedException();
        }
    }
}
