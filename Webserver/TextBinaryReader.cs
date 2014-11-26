﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Babbacombe.Webserver {

    /// <summary>
    /// Used for reading a stream that contains a mixture of text and binary.
    /// Contains methods to read text (like a StreamReader) but can then switch
    /// to transferring via a buffer until a text delimiter is reached.
    /// </summary>
    internal class TextBinaryReader : IDisposable {
        private BufferedStream _stream;
        public bool EndOfStream { get; private set; }
        private Queue<int> _pushBackBuffer = new Queue<int>();

        public TextBinaryReader(Stream stream) {
            if (!stream.CanRead) throw new ArgumentException("TextBinaryReader must be able to read the stream");
            _stream = new BufferedStream(stream);
        }

        public TextBinaryReader(BufferedStream stream) {
            if (!stream.CanRead) throw new ArgumentException("TextBinaryReader must be able to read the stream");
            _stream = stream;
        }

        /// <summary>
        /// Reads and returns a line from the stream, not including the line delimiter.
        /// </summary>
        /// <returns>null at the end of the stream.</returns>
        public string ReadLine() {
            if (EndOfStream) return null;
            var buf = new StringBuilder();
            int ch = _stream.ReadByte();
            while (ch >= 0 && ch != '\n') {
                buf.Append((char)ch);
            }
            if (ch < 0) EndOfStream = true;
            while (buf.Length > 0 && buf[buf.Length - 1] == '\r') buf.Length--;
            return buf.ToString();
        }

        /// <summary>
        /// Copies up to count bytes from the stream into the buffer. Stops when
        /// the delimiter is reached (does not return it).
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="delimiter">
        /// The delimiter at which the read will stop (not including an initial \n or \r\n).
        /// </param>
        /// <param name="delimiterReached">
        /// True if the position in the stream has reached (and passed) the delimiter.
        /// </param>
        /// <returns>
        /// 0 if the end of the stream has been reached.
        /// </returns>
        public int ReadBinary(byte[] buffer, int offset, int count, string delimiter, out bool delimiterReached) {
            if (string.IsNullOrEmpty(delimiter)) throw new ArgumentException("Delimiter cannot be null or empty");
            if (delimiter.Contains('\r') || delimiter.Contains('\n')) throw new ArgumentException(@"Delimiter cannot contain \r or \n");

            delimiterReached = false;
            if (EndOfStream) return 0;
            
            List<int> delimiterBuffer = new List<int>();
            int delimCount = 0; // Count of how many delimiter characters have been read and matched (not inc \r\n)
            int bytesRead = 0;
            while (bytesRead < count && !delimiterReached) {
                int ch = getNextByte();
                if (ch < 0) {
                    EndOfStream = true;
                    if (delimiterBuffer.Any()) {
                        // Send any delimiter data that has been held aside.
                        int cnt = Math.Min(delimiterBuffer.Count, count - bytesRead);
                        delimiterBuffer.ConvertAll(c => (byte)c).CopyTo(0, buffer, offset + bytesRead, cnt);
                        delimiterBuffer.RemoveRange(0, cnt);
                        return bytesRead + cnt;                        
                    }
                }
                if (!_pushBackBuffer.Any() && ch == '\n' || ch == '\r') {
                    if (delimCount == 0) {
                        // Possibly at or near start of delimiter
                        delimiterBuffer.Add(ch);
                    } else {
                        // End of line reached without matching the delimiter
                        foreach (int c in delimiterBuffer) _pushBackBuffer.Enqueue(c);
                        delimiterBuffer.Clear();
                        _pushBackBuffer.Enqueue(ch);
                        delimCount = 0;
                    }
                    continue;
                }
                if (delimiterBuffer.Any()) {
                    delimiterBuffer.Add(ch);
                    if (ch == delimiter[delimCount]) {
                        delimCount++;
                        if (delimCount == delimiter.Length) {
                            // Found the delimiter
                            delimiterReached = true;
                        }
                    } else {
                        // Doesn't match the delimiter - push what has been saved back on for normal processing
                        foreach (int c in delimiterBuffer) _pushBackBuffer.Enqueue(c);
                        delimiterBuffer.Clear();
                        _pushBackBuffer.Enqueue(ch);
                        delimCount = 0;
                    }
                } else {
                    buffer[offset + bytesRead++] = (byte)ch;
                }
            }
            return bytesRead;
        }

        private int getNextByte() {
            if (_pushBackBuffer.Any()) return _pushBackBuffer.Dequeue();
            return _stream.ReadByte();
        }

        public void Dispose() {
            if (_stream != null) {
                _stream.Dispose();
                _stream = null;
            }
        }
    }
}