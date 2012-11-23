using CommandLine;

namespace Tcad
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var options = new Options();
            if (CommandLineParser.Default.ParseArguments(args, options))
            {
                return new TeamCityArtifactDownloader(options).Run();
            }
            return -1;
        }
    }
}
