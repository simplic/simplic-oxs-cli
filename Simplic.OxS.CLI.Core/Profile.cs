using Spectre.Console.Cli;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Simplic.OxS.CLI.Core
{
    public class Profile
    {
        public void Add(CommandSettings settings)
        {
            var interfaces = settings.GetType().GetInterfaces();
            foreach (var type in interfaces)
            {
                // Do not save profile settings
                if (type == typeof(IInjectedSettings))
                    continue;

                var typeName = type.FullName ?? "?Unknown";
                if (!Data.TryGetValue(typeName, out var value))
                {
                    value = [];
                    Data.Add(typeName, value);
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
        }

        public void Apply(CommandSettings settings)
        {
            var interfaces = settings.GetType().GetInterfaces();
            foreach (var type in interfaces)
            {
                if (!Data.TryGetValue(type.FullName ?? "?Unknown", out var value))
                    continue;
                foreach (var pair in value)
                {
                    var property = type.GetProperty(pair.Key);
                    if (property == null)
                        continue;

                    // Don't override current values
                    if (property.GetValue(settings) != null)
                        continue;

                    var setting = JsonSerializer.Deserialize(pair.Value, property.PropertyType);
                    property.SetValue(settings, setting);
                }
            }
        }

        public IDictionary<string, JsonObject> Data { get; set; } = new Dictionary<string, JsonObject>();
    }
}
