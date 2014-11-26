using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Babbacombe.Webserver {

    /// <summary>
    /// Manages posted submission of a form with enc-type="multipart/form-data" (commonly
    /// used for file uploads).
    /// For each uploaded file a FileUploaded event is triggered that
    /// contains the header information sent by the client, and a stream containing the data.
    /// </summary>
    public class MultipartManager {
        private HttpSession _session;
        private List<DataItem> _dataItems = new List<DataItem>();

        public class DataItem {
            public string Name { get; private set; }
            public string Value { get; private set; }
            private DataItem() { }
            public DataItem(string name, string value) {
                Name = name;
                Value = value;
            }
        }

        /// <summary>
        /// Information about the current file.
        /// </summary>
        public class FileInfo {
            public IDictionary<string, string> Items { get; private set; }
            private FileInfo() { }
            internal FileInfo(IDictionary<string, string> items) {
                Items = items;
            }
            public string ContentType { get { return Items.ContainsKey("Content-Type") ? Items["Content-Type"] : null; } }
            public string Filename { get { return Items.ContainsKey("filename") ? Items["filename"] : null; } }
            public string Name { get { return Items.ContainsKey("name") ? Items["name"] : null; } }
        }

        public class FileUploadedEventArgs : EventArgs {
            public FileInfo Info { get; private set; }
            public Stream Contents { get; private set; }
            private FileUploadedEventArgs() { }
            internal FileUploadedEventArgs(FileInfo info, Stream contents) {
                Info = info;
                Contents = contents;
            }
        }

        /// <summary>
        /// Raised for each file that is being uploaded.
        /// </summary>
        public event EventHandler<FileUploadedEventArgs> FileUploaded;

        public class DataReceivedEventArgs : EventArgs {
            public IDictionary<string, string> Items { get; private set; }
            public string Name { get; private set; }
            public string Value { get; private set; }
            private DataReceivedEventArgs() { }
            internal DataReceivedEventArgs(IDictionary<string, string> items, string name, string value) {
                Items = items;
                Name = name;
                Value = value;
            }
        }

        /// <summary>
        /// Raised for each data item in the form that is not a filename (or a file upload with no files,
        /// in which case the Value is null). The Name/Value data is also available from the DataItems
        /// property.
        /// </summary>
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        public MultipartManager(HttpSession session) {
            _session = session;
        }

        /// <summary>
        /// The data items (excluding uploaded files) that have been received.
        /// </summary>
        public IEnumerable<DataItem> DataItems { get { return _dataItems; } }

        protected virtual void OnFileUploaded(FileInfo info, Stream contents) {
            if (FileUploaded != null) FileUploaded(this, new FileUploadedEventArgs(info, contents));
        }

        protected virtual void OnDataReceived(IDictionary<string, string> items, string name, string data) {
            if (DataReceived != null) DataReceived(this, new DataReceivedEventArgs(items, name, data));
        }

        /// <summary>
        /// Processes the multipart data posted from the client, raising events as
        /// files and data items are reached.
        /// </summary>
        public void Process() {
            using (var reader = new TextBinaryReader(_session.Context.Request.InputStream)) {
                string delimiter = reader.ReadLine();
                do {
                    var headers = readHeaders(reader);
                    if (headers == null) break;
                    var headerData = parseHeaders(headers);
                    if (headerData.ContainsKey("filename")) {
                        var info = new FileInfo(headerData);
                        using (var uploadStream = new UploadStream(reader, delimiter)) {
                            OnFileUploaded(info, uploadStream);
                            uploadStream.Flush();
                        }
                    } else {
                        string name = headerData.ContainsKey("name") ? headerData["name"] : null;
                        string value;
                        using (var m = new MemoryStream()) 
                        using (var d = new UploadStream(reader, delimiter)) {
                            d.CopyTo(m);
                            m.Seek(0, SeekOrigin.Begin);
                            value = new string(Encoding.UTF8.GetChars(m.ToArray()));
                        }
                        _dataItems.Add(new DataItem(name, value));
                        OnDataReceived(headerData, name, value);
                    }
                    reader.ReadLine();
                } while (true);
            }
        }

        private string readHeaders(TextBinaryReader reader) {
            StringBuilder headers = new StringBuilder();
            string line = reader.ReadLine();
            while (!string.IsNullOrEmpty(line)) {
                headers.AppendLine(line);
                line = reader.ReadLine();
            }
            if (line == null) return null;
            return headers.ToString();
        }

        private IDictionary<string, string> parseHeaders(string headers) {
            Match propertiesMatch = Regex.Match(headers,
                @"((?<Key>[^\:=]+)(?:[\=\:])(?:[\s]*)(?<Value>([^"";\s\r]+)|(""[^""]+""))(?:[;\s]*))+");

            var parsed = propertiesMatch.Groups["Key"].Captures.Cast<Capture>().Select((c, i) => new { c, i })
                .Join(propertiesMatch.Groups["Value"].Captures.Cast<Capture>().Select((c, i) => new { c, i }), key => key.i, value => value.i,
                    (key, value) => new KeyValuePair<string, string>(key.c.Value, value.c.Value))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.StartsWith("\"") ? kvp.Value.Substring(1, kvp.Value.Length - 2) : kvp.Value);

            return parsed;
        }
    }
}
