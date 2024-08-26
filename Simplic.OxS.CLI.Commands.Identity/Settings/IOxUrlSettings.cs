using Spectre.Console.Cli;
using System.ComponentModel;

namespace Simplic.OxS.CLI.Identity.Settings
{
    public interface IOxUrlSettings
    {
        [CommandOption("-u|--uri <SERVER>")]
        [Description("URI of Ox Server instance")]
        public Uri? Uri { get; init; }

        public HttpClient? HttpClient { get; set; }
    }
}
