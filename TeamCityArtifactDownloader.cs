using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using tcad;

namespace Tcad
{
    internal class TeamCityArtifactDownloader
    {
        private readonly Options options;
        private string outdir;
        private UriBuilder uriBuilder;

        public TeamCityArtifactDownloader(Options options)
        {
            this.options = options;
        }

        /// <summary>
        /// Do magic.
        /// </summary>
        /// <returns>Exit code.</returns>
        public int Run()
        {
            // enforce trailing separator for simplicity
            outdir = options.OutDir ?? Directory.GetCurrentDirectory();
            if (!outdir.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                outdir += Path.DirectorySeparatorChar;
            }

            // if no protocol is specified the uribuilder gets confused
            uriBuilder = new UriBuilder(options.Url.Contains("://")
                ? options.Url
                : "http://" + options.Url);

            // if credentials are not supplied prompt user
            if (!EnsureCredentials())
            {
                return 0;
            }

            try
            {
                // go through every artifact
                foreach (string fileName in GetArtifacts())
                {
                    if (fileName.StartsWith(".teamcity") &&
                        !options.IncludeHidden)
                    {
                        continue;
                    }

                    // filter on glob
                    if (string.IsNullOrEmpty(options.Glob) ||
                        fileName.Glob(options.Glob))
                    {
                        DownloadArtifact(fileName);
                    }
                }
            }
            catch (WebException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
            return 0;
        }

        /// <summary>
        /// Prompts user for username and password.
        /// </summary>
        /// <returns>
        /// False if input operation was aborted, True otherwise
        /// </returns>
        private bool EnsureCredentials()
        {
            if (string.IsNullOrEmpty(uriBuilder.UserName))
            {
                string input = GetInput("username");
                if (input == null)
                {
                    return false;
                }
                uriBuilder.UserName = input;
            }
            if (string.IsNullOrEmpty(uriBuilder.Password))
            {
                string input = GetInput("password");
                if (input == null)
                {
                    return false;
                }
                uriBuilder.Password = input;
            }
            return true;
        }

        private static string GetInput(string what)
        {
            string s = null;
            while (string.IsNullOrEmpty(s))
            {
                Console.Write(what + ": ");
                s = Console.ReadLine();
                if (s == null)
                {
                    break;
                }
                s = s.Trim();
            }
            return s;
        }

        /// <summary>
        /// Download a single artifact, respecting output options.
        /// </summary>
        /// <param name="artifactPath">The full path of the artifact.</param>
        public void DownloadArtifact(string artifactPath)
        {
            string localPath = outdir;

            // teamcity uses forward slashes in paths
            if (artifactPath.Contains("/"))
            {
                //localPath += options.Recursive
                //    ? artifactPath.Replace('/', Path.DirectorySeparatorChar)
                //    : artifactPath.Split('/').Last();
                artifactPath = artifactPath.Replace(
                    '/', Path.DirectorySeparatorChar);
            }
            localPath += options.FlattenArtifactFolders
                ? artifactPath.Split(Path.DirectorySeparatorChar).Last()
                : artifactPath;

            // make sure output directory exists
            string directory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(directory))
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }

            // download the file
            if (!options.Dry)
            {
                string filePath = CreateFileRequest(artifactPath);
                HttpWebRequest request = CreateWebRequest(filePath);
                Utils.DownloadFile(request, localPath);
            }
            if (options.Verbose)
            {
                Console.WriteLine(artifactPath);
            }
        }

        /// <summary>
        /// Returns the full path of each artifact, including file extension.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetArtifacts()
        {
            var list = new List<string>();
            using (var stream = new MemoryStream())
            {
                string path = CreateFileRequest("teamcity-ivy.xml");
                HttpWebRequest request = CreateWebRequest(path);
                Utils.DownloadFile(request, stream);
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new XmlTextReader(stream);
                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element ||
                        reader.Name != "artifact")
                    {
                        continue;
                    }
                    string name = reader.GetAttribute("name");
                    string ext = reader.GetAttribute("ext");
                    list.Add(string.Format("{0}.{1}", name, ext));
                }
            }
            return list;
        }

        private string CreateFileRequest(string file)
        {
            uriBuilder.Path = string.Format(
                "httpAuth/repository/download/{0}/{1}/{2}",
                options.BuildType,
                options.BuildId,
                file);
            return uriBuilder.ToString();
        }

        private string CreateAuthInfo()
        {
            string authInfo = uriBuilder.UserName + ":" + uriBuilder.Password;
            authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
            return authInfo;
        }

        private HttpWebRequest CreateWebRequest(string uri)
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            //request.Proxy = null;
            string authInfo = CreateAuthInfo();
            request.Headers["Authorization"] = "Basic " + authInfo;
            return request;
        }
    }
}
