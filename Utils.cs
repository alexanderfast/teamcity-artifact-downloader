using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace tcad
{
    public static class Utils
    {
        /// <summary>
        /// Download file to memory and return content as a string.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string DownloadFile(WebRequest request)
        {
            using (var stream = new MemoryStream())
            {
                // write to stream 
                DownloadFile(request, stream);
                // Jump to the start position of the stream
                stream.Seek(0, SeekOrigin.Begin);

                var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Download file from TeamCity to local file.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="localFilename"></param>
        public static void DownloadFile(
            WebRequest request,
            string localFilename)
        {
            Stream localStream;

            // Create the local file
            if (File.Exists(localFilename))
            {
                localStream = File.Open(
                    localFilename, FileMode.Truncate);
            }
            else
            {
                localStream = File.Create(localFilename);
            }

            DownloadFile(request, localStream);
            localStream.Close();
        }

        /// <summary>
        /// From http://www.codeguru.com/columns/dotnettips/article.php/c7005/Downloading-Files-with-the-WebRequest-and-WebResponse-Classes.htm
        /// </summary>
        /// <param name="request"></param>
        /// <param name="localStream"></param>
        /// <returns></returns>
        public static int DownloadFile(
            WebRequest request,
            Stream localStream)
        {
            // Function will return the number of bytes processed
            // to the caller. Initialize to 0 here.
            int bytesProcessed = 0;

            // Assign values to these objects here so that they can
            // be referenced in the finally block
            Stream remoteStream = null;
            WebResponse response = null;

            // Use a try/catch/finally block as both the WebRequest and Stream
            // classes throw exceptions upon error
            try
            {
                // Create a request for the specified remote file name
                //WebRequest request = WebRequest.Create(remoteFilename);
                if (request != null)
                {
                    // Send the request to the server and retrieve the
                    // WebResponse object 
                    response = request.GetResponse();
                    if (response != null)
                    {
                        //Console.WriteLine(string.Format("Download: {0}", request.RequestUri));

                        // Once the WebResponse object has been retrieved,
                        // get the stream object associated with the response's data
                        remoteStream = response.GetResponseStream();

                        if (remoteStream != null)
                        {
                            // Allocate a 1k buffer
                            var buffer = new byte[1024];
                            int bytesRead;

                            // Simple do/while loop to read from stream until
                            // no bytes are returned
                            do
                            {
                                // Read data (up to 1k) from the stream
                                bytesRead = remoteStream.Read(buffer, 0, buffer.Length);

                                // Write the data to the local file
                                localStream.Write(buffer, 0, bytesRead);

                                // Increment total bytes processed
                                bytesProcessed += bytesRead;
                            } while (bytesRead > 0);
                        }
                    }
                }

                //var sr = new StreamReader(localStream);
                //var myStr = sr.ReadToEnd();
                //return myStr;
            }
            finally
            {
                // Close the response and streams objects here 
                // to make sure they're closed even if an exception
                // is thrown at some point
                if (response != null)
                {
                    response.Close();
                }
                if (remoteStream != null)
                {
                    remoteStream.Close();
                }
            }

            // Return total bytes processed to caller.
            return bytesProcessed;
        }

        /// <summary>
        /// Compares the string against a given pattern.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="wildcard">The wildcard, where "*" means any sequence of characters, and "?" means any single character.</param>
        /// <returns><c>true</c> if the string matches the given pattern; otherwise <c>false</c>.</returns>
        public static bool Glob(this string str, string wildcard)
        {
            return new Regex(
                "^" + Regex.Escape(wildcard).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
                .IsMatch(str);
        }
    }
}
