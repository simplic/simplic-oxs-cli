using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Unity;

namespace Simplic.OxS.CLI.Core
{
    internal class CommandInterceptor(IUnityContainer container, Dictionary<Type, List<Type>> modules) : ICommandInterceptor
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };

        /// <summary>
        /// Execute all modules required by the command
        /// </summary>
        /// <param name="context"></param>
        /// <param name="settings"></param>
        void ICommandInterceptor.Intercept(CommandContext context, CommandSettings settings)
        {
            var injectedSettings = (IInjectedSettings)settings;
            var profiles = injectedSettings.Profiles;
            foreach (var profile in profiles ?? [])
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

        private static void ApplyProfile(string name, CommandSettings settings)
        {
            Dictionary<string, JsonObject>? profile;
            try
            {
                using var file = File.OpenRead(GetProfilePath(name));
                profile = JsonSerializer.Deserialize<Dictionary<string, JsonObject>>(file) ?? [];
            }
            catch (FileNotFoundException)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]Profile [yellow]{name}[/] does not exist[/]");
                throw new CancelCommandException();
            }

            var interfaces = settings.GetType().GetInterfaces();
            foreach (var type in interfaces)
            {
                if (!profile.TryGetValue(type.FullName ?? "?Unknown", out var value))
                    continue;
                foreach (var pair in value)
                {
                    var property = type.GetProperty(pair.Key);
                    if (property == null)
                        continue;

                    var setting = JsonSerializer.Deserialize(pair.Value, property.PropertyType);
                    property.SetValue(settings, setting);
                }
            }
        }

        private static void StoreProfile(string name, CommandSettings settings, bool add)
        {
            Dictionary<string, JsonObject>? profile = null;
            var path = GetProfilePath(name);
            var added = false;
            if (add && File.Exists(path))
            {
                using var fileRead = File.OpenRead(path);
                profile = JsonSerializer.Deserialize<Dictionary<string, JsonObject>>(fileRead);
                added = true;
            }
            profile ??= [];

            var interfaces = settings.GetType().GetInterfaces();
            foreach (var type in interfaces)
            {
                // Do not save profile settings
                if (type == typeof(IInjectedSettings))
                    continue;

                var typeName = type.FullName ?? "?Unknown";
                if (!profile.TryGetValue(typeName, out var value))
                {
                    value = [];
                    profile.Add(typeName, value);
                }

                foreach (var property in type.GetProperties())
                {
                    var isArgument = property.GetCustomAttributes(false)
                        .Any(a => a.GetType() == typeof(CommandArgumentAttribute)
                               || a.GetType() == typeof(CommandOptionAttribute));
                    if (!isArgument)
                        continue;

                    var setting = property.GetValue(settings);
                    if (setting != null)
                        value[property.Name] = JsonSerializer.SerializeToNode(setting);
                }
            }

            using var fileWrite = File.Create(path);
            JsonSerializer.Serialize(fileWrite, profile, JsonOptions);

            if (added)
                AnsiConsole.MarkupLineInterpolated($"[red]Added to profile [yellow]{name}[/][/]");
            else
                AnsiConsole.MarkupLineInterpolated($"[red]Profile [yellow]{name}[/] written[/]");

            throw new CancelCommandException();
        }

        private static string GetProfilePath(string name)
        {
            var userData = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // https://stackoverflow.com/a/847251
            var invalid = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            var regex = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalid);
            var newName = Regex.Replace(name, regex, "_") + ".json";

            var directory = Path.Join(userData, ".simplic", "simplic-oxs-cli", "profiles");
            Directory.CreateDirectory(directory);

            return Path.Join(directory, newName);
        }
    }
}
