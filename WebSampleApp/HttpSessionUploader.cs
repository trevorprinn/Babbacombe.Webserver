using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;

using Babbacombe.Logger;
using Babbacombe.Webserver;

namespace Babbacombe.WebSampleApp.Uploader {

    // This class doesn't really do anything except define the namespace for the request handler.
    // When the default page for the Uploader application is requested it sends index.html automatically.
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
            // Create a multipart manager to handle the uploads in the posted stream
            var manager = new MultipartManager(Session);
            // Respond to each file by writing it out to the Uploads folder.
            manager.FileUploaded += (object s, MultipartManager.FileUploadedEventArgs e) => {
                string fname = Path.Combine(folder, e.Info.Filename);
                using (var f = new FileStream(fname, FileMode.Create)) {
                    e.Contents.CopyTo(f);
                }
                LogFile.Log("Uploaded File: {0}", e.Info.Filename);
            };
            // Just log any other data received.
            manager.DataReceived += (object s, MultipartManager.DataReceivedEventArgs e) => {
                LogFile.Log("Upload: {0} = {1}", e.Name, e.Value);
            };
            // Start processing the posted stream (NB easy to forget to call this).
            manager.Process();
            
            // Redisplay the upload page again.
            Session.Redirect(Session.TopUrl);
        }
    }
}
