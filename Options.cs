using System.Reflection;
using CommandLine;
using CommandLine.Text;

namespace Tcad
{
    internal sealed class Options : CommandLineOptionsBase
    {
        [Option("u", "url", Required = true,
            HelpText = "Url to TeamCity instance.")]
        public string Url { get; set; }

        [Option("b", "build", Required = true,
            HelpText = "The build to download artifacts from.")]
        public string BuildType { get; set; }

        [Option("i", "build-id", DefaultValue = "lastFinished",
            HelpText = "The specific build to download (default: lastFinished).")]
        public string BuildId { get; set; }

        [Option("o", "outdir",
            HelpText = "Directory to download files to (default: current).")]
        public string OutDir { get; set; }

        [Option("g", "glob", DefaultValue = "*.*",
            HelpText = "Only download files matching glob expression.")]
        public string Glob { get; set; }

        [Option("d", "dry-run",
            HelpText = "Do not download any artifacts.")]
        public bool Dry { get; set; }

        [Option("f", "flatten", DefaultValue = false,
            HelpText = "Flatten the artifact folder structure.")]
        public bool FlattenArtifactFolders { get; set; }

        [Option("v", "verbose",
            HelpText = "Print every handled artifact.")]
        public bool Verbose { get; set; }

        [Option(null, "hidden", DefaultValue = false,
            HelpText = "Include TeamCity generated hidden artifacts.")]
        public bool IncludeHidden { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            Assembly assembly = typeof(Options).Assembly;
            var helpText = new HelpText
                {
                    Heading = new HeadingInfo(
                        assembly.GetName().Name,
                        assembly.GetName().Version.ToString())
                };
            helpText.AddOptions(this);
            return helpText;
        }
    }
}
