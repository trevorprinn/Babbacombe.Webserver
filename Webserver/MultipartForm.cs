using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Babbacombe.Webserver {
    // Code found at http://multipartparser.codeplex.com/discussions/210676

    public abstract class PostedField {
        public string ContentType { get; set; }
        public string Name { get; set; }
    }

    public class PostedValue : PostedField {
        public string Value { get; set; }

        public PostedValue(Dictionary<string, string> properties, string contents) {
            ContentType = "text/plain";
            Name = properties["name"];
            Value = contents;
        }
    }

    public class PostedFile : PostedField {
        public string FileName { get; set; }
        public Stream FileContents { get; set; }

        public PostedFile(Dictionary<string, string> properties, byte[] contents) {
            ContentType = properties["Content-Type"];
            FileName = properties["filename"];
            Name = properties["name"];

            FileContents = new MemoryStream(contents);
        }
    }

    public class MultipartForm {
        public Dictionary<string, PostedField> Fields { get; private set; }

        private MultipartForm(IEnumerable<string> fields, byte[] data, byte[] dataStartMarker, byte[] dataEndMarker) {
            Fields = new Dictionary<string, PostedField>();

            ParseFields(fields, data, dataStartMarker, dataEndMarker);
        }

        private void ParseFields(IEnumerable<string> fields, byte[] data, byte[] dataStartMarker, byte[] dataEndMarker) {
            int dataStartIndex = (IndexOf(data, dataStartMarker, 0) > -1)
                                     ? IndexOf(data, dataStartMarker, 0) + dataStartMarker.Length
                                     : -1;
            int dataLength = (dataStartIndex > -1) ? IndexOf(data, dataEndMarker, dataStartIndex) - dataStartIndex : -1;

            foreach (var field in fields) {
                Dictionary<string, string> properties = ParseProperties(field);
                string contents = Regex.Match(field, @"(?<=\r\n\r\n)(.*?)(?=\r\n)").Value;
                byte[] rawData = null;

                if (dataLength > -1) {
                    rawData = new byte[dataLength];
                    Buffer.BlockCopy(data, dataStartIndex, rawData, 0, dataLength);
                }

                if (properties.ContainsKey("filename")) {
                    Fields.Add(properties["name"], new PostedFile(properties, rawData));
                } else {
                    Fields.Add(properties["name"], new PostedValue(properties, contents));
                }

                if (dataStartIndex > -1) {
                    dataStartIndex = IndexOf(data, dataStartMarker, dataStartIndex + dataLength);
                    if (dataStartIndex > -1) {
                        dataStartIndex += dataStartMarker.Length;
                        dataLength = IndexOf(data, dataEndMarker, dataStartIndex) - dataStartIndex;
                    }
                }
            }
        }

        private static Dictionary<string, string> ParseProperties(string field) {
            Match propertiesMatch = Regex.Match(field.Substring(0, field.IndexOf("\r\n\r\n") + "\r\n\r\n".Length),
                                                  @"(?:.*)\r\n((?<Key>[^\:=]+)(?:[\=\:])(?:[\s]*)(?<Value>[^;\s\r]+)(?:[;\s]*))+\r\n\r\n(?:.*)");

            var parsed = propertiesMatch.Groups["Key"].Captures.Cast<Capture>().Select((c, i) => new { c, i })
                .Join(propertiesMatch.Groups["Value"].Captures.Cast<Capture>().Select((c, i) => new { c, i }), key => key.i, value => value.i,
                    (key, value) => new KeyValuePair<string, string>(key.c.Value, value.c.Value))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.StartsWith("\"") ? kvp.Value.Substring(1, kvp.Value.Length - 2) : kvp.Value);

            return parsed;
        }

        public static MultipartForm FromBytes(byte[] data, Encoding encoding) {
            // Copy to a string for header parsing
            string content = encoding.GetString(data);

            // The first line should contain the delimiter            
            string delimiter = content.Substring(0, content.IndexOf("\r\n"));
            MatchCollection matches = Regex.Matches(content, delimiter);
            List<string> fields = new List<string>();

            foreach (Match match in matches) {
                if (match.NextMatch() != null && !string.IsNullOrEmpty(match.NextMatch().Value)) {
                    string fieldData = content.Substring(match.Index + delimiter.Length,
                                                         match.NextMatch().Index - match.Index - delimiter.Length);

                    fields.Add(fieldData);
                }

            }

            return new MultipartForm(fields, data, encoding.GetBytes("\r\n\r\n"), encoding.GetBytes("\r\n" + delimiter));
        }

        public static MultipartForm FromStream(Stream stream, Encoding encoding) {
            // Read the stream into a byte array
            byte[] data = ToByteArray(stream);

            return FromBytes(data, encoding);
        }

        private static int IndexOf(byte[] searchWithin, byte[] serachFor, int startIndex) {
            int index = 0;
            int startPos = Array.IndexOf(searchWithin, serachFor[0], startIndex);

            if (startPos != -1) {
                while ((startPos + index) < searchWithin.Length) {
                    if (searchWithin[startPos + index] == serachFor[index]) {
                        index++;
                        if (index == serachFor.Length) {
                            return startPos;
                        }
                    } else {
                        startPos = Array.IndexOf<byte>(searchWithin, serachFor[0], startPos + index);
                        if (startPos == -1) {
                            return -1;
                        }
                        index = 0;
                    }
                }
            }

            return -1;
        }

        private static byte[] ToByteArray(Stream stream) {
            byte[] buffer = new byte[32768];
            using (MemoryStream ms = new MemoryStream()) {
                while (true) {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        return ms.ToArray();
                    ms.Write(buffer, 0, read);
                }
            }
        }
    }
}