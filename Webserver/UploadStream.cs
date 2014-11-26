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

        public override void Flush() { }

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
