using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;

using Babbacombe.Webserver;

namespace Babbacombe.WebSampleApp.Uploader {
    class HttpSession : Babbacombe.Webserver.HttpSession {
    }

    class Uploader : HttpRequestHandler {

        /// <summary>
        /// The client is uploading a file. Called with an url resembling Uploader?c=Uploader&m=Upload&file=filename.
        /// Writes the file to the user's desktop.
        /// </summary>
        public void Upload() {
            var formData = MultipartForm.FromStream(GetPostStream(), Encoding.UTF8);
            if (formData.Fields.ContainsKey("filename") && formData.Fields["filename"] is PostedFile) {
                var f = (PostedFile)formData.Fields["filename"];
                using (var s = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Uploaded " + f.FileName), FileMode.CreateNew)) {
                    f.FileContents.CopyTo(s);
                }
            }

            //using (var m = new MemoryStream()) {
            //    GetPostStream().CopyTo(m);
            //    m.Seek(0, SeekOrigin.Begin);
            //    var l = (int)m.Length;
            //    var buf = new byte[l];
            //    m.Read(buf, 0, l);
            //    File.WriteAllBytes(@"C:\Users\Trev\Desktop\upload.dat", buf);
            //}
            
            var url = Session.Context.Request.Url;
            Session.Redirect(new UriBuilder(url.Scheme, url.Host, url.Port, url.AbsolutePath).Uri);
        }
    }
}
