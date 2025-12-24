using oxs.Clients;
using oxs.Commands.Configure;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace oxs;

public static class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp();

        app.Configure(config =>
        {
            RegisterConfigure.RegisterCommands(config);

            // config.AddBranch<ConfigureSettings>("package", add =>
            // {
            //     add.AddCommand<ConfigureEnvCommand>("new");
            //     add.AddCommand<AddReferenceCommand>("pack");
            //     add.AddCommand<AddReferenceCommand>("puch");
            // });
        });

        return app.Run(args);
    }
}


