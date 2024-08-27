using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics;
using Unity;

namespace Simplic.OxS.CLI.Core
{
    internal class CommandInterceptor(IUnityContainer container, Dictionary<Type, List<Type>> modules) : ICommandInterceptor
    {
        private readonly ProfileManager profileManager = container.Resolve<ProfileManager>();

        /// <summary>
        /// Execute all modules required by the command
        /// </summary>
        /// <param name="context"></param>
        /// <param name="settings"></param>
        void ICommandInterceptor.Intercept(CommandContext context, CommandSettings settings)
        {
            var injectedSettings = (IInjectedSettings)settings;
            var profiles = injectedSettings.Profiles ?? [];
            var defaultProfile = profileManager.GetDefaultProfile();
            if (defaultProfile != null)
                profiles = [defaultProfile, .. profiles];
            foreach (var profile in profiles)
                ApplyProfile(profile, settings);

            if (injectedSettings.AddProfile != null)
                StoreProfile(injectedSettings.AddProfile, settings, true);
            if (injectedSettings.StoreProfile != null)
                StoreProfile(injectedSettings.StoreProfile, settings, false);

            var data = (CommandData)context.Data!;
            var alreadyInvoked = new HashSet<Type>();
            foreach (var module in data.RequiredModules)
                InvokeModule(module, settings, alreadyInvoked);
        }

        private void InvokeModule(Type type, CommandSettings settings, HashSet<Type> alreadyInvoked)
        {
            // Prevent modules from being executed multiple times
            if (alreadyInvoked.Contains(type))
                return;

            var required = modules[type];
            foreach (var module in required)
                InvokeModule(module, settings, alreadyInvoked);
            var instance = container.Resolve(type);
            var method = instance.GetType().GetMethod("Execute");
            var task = (Task)method!.Invoke(instance, [settings])!;
            task.GetAwaiter().GetResult();
        }

        private void ApplyProfile(string name, CommandSettings settings)
        {
            var profile = profileManager.Load(name);
            if (profile == null)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]Profile [yellow]{name}[/] does not exist[/]");
                throw new CancelCommandException();
            }

            profile.Apply(settings);
            AnsiConsole.MarkupLineInterpolated($"[gray]Profile [yellow]{name}[/] applied[/]");
        }

        private void StoreProfile(string name, CommandSettings settings, bool add)
        {
            Profile? profile = null;
            if (add)
                profile = profileManager.Load(name);
            var added = profile != null;
            profile ??= new();

            profile.Add(settings);
            profileManager.Save(name, profile);

            if (added)
                AnsiConsole.MarkupLineInterpolated($"[green]Added to profile [yellow]{name}[/][/]");
            else
                AnsiConsole.MarkupLineInterpolated($"[green]Profile [yellow]{name}[/] written[/]");

            // Don't execute the command when writing a profile
            throw new CancelCommandException();
        }
    }
}
