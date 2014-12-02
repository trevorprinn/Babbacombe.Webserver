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
        /// The client is uploading one or more files. Called with an url resembling uploader?c=Uploader&m=Upload
        /// Writes the files to a folder in the user's desktop.
        /// </summary>
        public void Upload() {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Uploaded");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            var manager = new MultipartManager(Session);
            manager.FileUploaded += (object s, MultipartManager.FileUploadedEventArgs e) => {
                string fname = Path.Combine(folder, e.Info.Filename);
                using (var f = new FileStream(fname, FileMode.Create)) {
                    e.Contents.CopyTo(f);
                }
                LogFile.Log("Uploaded File: {0}", e.Info.Filename);
            };
            manager.DataReceived += (object s, MultipartManager.DataReceivedEventArgs e) => {
                LogFile.Log("Upload: {0} = {1}", e.Name, e.Value);
            };
            manager.Process();
            
            // Redisplay the upload page again.
            var url = Session.Context.Request.Url;
            Session.Redirect(Session.TopUrl);
        }
    }
}
