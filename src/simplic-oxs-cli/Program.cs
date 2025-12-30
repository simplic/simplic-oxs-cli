using oxs.Clients;
using oxs.Commands.Configure;
using oxs.Commands.Project;
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
            RegisterProject.RegisterCommands(config);
        });

        return app.Run(args);
    }
}


