using Spectre.Console;
using Spectre.Console.Cli;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using oxs.Configuration;
using Simplic.OxS.Flow.Meta;
using System.Diagnostics;

namespace oxs.Commands.Flow
{
    public class FlowPushNodeDefCommand : Command<FlowPushNodeDefSettings>
    {
        public override int Execute(CommandContext context, FlowPushNodeDefSettings settings, CancellationToken cancellationToken)
        {
            return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
        }

        private async Task<int> ExecuteAsync(CommandContext context, FlowPushNodeDefSettings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(settings.DllPath))
            {
                AnsiConsole.MarkupLine("[red]DLL path required. Use -p|--path <Path-to-dll>[/]");
                return 1;
            }

            if (!File.Exists(settings.DllPath))
            {
                AnsiConsole.MarkupLine($"[red]File not found:[/] {settings.DllPath}");
                return 1;
            }

            var configManager = new ConfigurationManager();
            var config = await configManager.LoadConfigurationAsync(settings.Section);
            if (config == null)
            {
                AnsiConsole.MarkupLine($"[red]Configuration not found for section '{settings.Section}'. Run 'oxs configure env'.[/]");
                return 1;
            }

            var apiBase = config.Api == "prod" ? "https://oxs.simplic.io/flow-api/v1" :
                          config.Api == "staging" ? "https://dev-oxs.simplic.io/flow-api/v1" : null;
            if (apiBase == null)
            {
                AnsiConsole.MarkupLine($"[red]Unknown API environment: {config.Api}[/]");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(config.Token))
            {
                AnsiConsole.MarkupLine("[red]Missing bearer token in configuration. Run 'oxs configure env'.[/]");
                return 1;
            }

            AnsiConsole.MarkupLine($"[blue]Analyzing assembly:[/] [yellow]{Path.GetFullPath(settings.DllPath)}[/]");

            var analyzer = new NodeDefinitionAnalyzer();
            List<NodeDefinition> defs;
            try
            {
                defs = analyzer.AnalyzeAssembly(settings.DllPath);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to analyze assembly:[/] {ex.Message}");
                return 1;
            }

            if (defs.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No flow node definitions found.[/]");
                return 0;
            }

            AnsiConsole.MarkupLine($"[blue]Found[/] [green]{defs.Count}[/] [blue]node definition(s). Pushing to API...[/]");

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.Token);
            http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var success = 0;
            foreach (var def in defs)
            {
                var request = PostNodeDefinitionRequest.FromDefinition(def);
                var json = JsonSerializer.Serialize(request, jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await http.PostAsync($"{apiBase}/NodeDefinition", content);

                if (response.IsSuccessStatusCode)
                {
                    success++;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Conflict || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var putReq = PutNodeDefinitionRequest.FromDefinition(def);
                    var putJson = JsonSerializer.Serialize(putReq, jsonOptions);
                    var putContent = new StringContent(putJson, System.Text.Encoding.UTF8, "application/json");
                    var putResp = await http.PutAsync($"{apiBase}/NodeDefinition/{def.Id}", putContent);
                    if (putResp.IsSuccessStatusCode)
                        success++;
                }
            }

            if (success == defs.Count)
            {
                AnsiConsole.MarkupLine($"[green]✓ Pushed {success}/{defs.Count} node definitions[/]");
                return 0;
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]Pushed {success}/{defs.Count} node definitions[/]");
                return 1;
            }
        }
    }

    public class NodePackage
    {
        public string? Name { get; set; }
        public string? Version { get; set; }
        public string? AssemblyName { get; set; }
        public string? ClassName { get; set; }
    }

    public class DataInPinDefinition { public string? Name { get; set; } public string? Type { get; set; } public bool Required { get; set; } public bool CanBeNull { get; set; } }
    public class DataOutPinDefinition { public string? Name { get; set; } public string? Type { get; set; } }
    public class FlowOutPinDefinition { public string? Name { get; set; } }
    public class CustomDataInPinTemplateDefinition { public string? Type { get; set; } public bool Required { get; set; } }
    public class CustomFlowOutPinTemplateDefinition { public string? Name { get; set; } }

    public class NodeDefinition
    {
        public string? Id { get; set; }
        public string Type { get; set; }
        public string? DisplayName { get; set; }
        public string? DisplayKey { get; set; }
        public string? Description { get; set; }
        public string? Markdown { get; set; }
        public string Target { get; set; }
        public CustomDataInPinTemplateDefinition? CustomDataInPinTemplate { get; set; }
        public CustomFlowOutPinTemplateDefinition? CustomFlowOutPinTemplate { get; set; }
        public DataInPinDefinition[]? DataInPins { get; set; }
        public DataOutPinDefinition[]? DataOutPins { get; set; }
        public FlowOutPinDefinition[]? FlowOutPins { get; set; }
        public NodePackage? Package { get; set; }
        public string? EventName { get; set; }
    }

    public class PostNodeDefinitionRequest
    {
        public string? Id { get; set; }
        public string Type { get; set; }
        public string? EventName { get; set; }
        public string? DisplayName { get; set; }
        public string? DisplayKey { get; set; }
        public string? Description { get; set; }
        public string? Markdown { get; set; }
        public string Target { get; set; }
        public CustomDataInPinTemplateDefinition? CustomDataInPinTemplate { get; set; }
        public CustomFlowOutPinTemplateDefinition? CustomFlowOutPinTemplate { get; set; }
        public DataInPinDefinition[]? DataInPins { get; set; }
        public DataOutPinDefinition[]? DataOutPins { get; set; }
        public FlowOutPinDefinition[]? FlowOutPins { get; set; }
        public NodePackage? Package { get; set; }

        public static PostNodeDefinitionRequest FromDefinition(NodeDefinition def) => new PostNodeDefinitionRequest
        {
            Id = def.Id,
            Type = def.Type,
            EventName = def.EventName,
            DisplayName = def.DisplayName,
            DisplayKey = def.DisplayKey,
            Description = def.Description,
            Markdown = def.Markdown,
            Target = def.Target,
            CustomDataInPinTemplate = def.CustomDataInPinTemplate,
            CustomFlowOutPinTemplate = def.CustomFlowOutPinTemplate,
            DataInPins = def.DataInPins,
            DataOutPins = def.DataOutPins,
            FlowOutPins = def.FlowOutPins,
            Package = def.Package
        };
    }

    public class PutNodeDefinitionRequest : PostNodeDefinitionRequest
    {
        public static PutNodeDefinitionRequest FromDefinition(NodeDefinition def)
        {
            var req = new PutNodeDefinitionRequest();
            var baseReq = PostNodeDefinitionRequest.FromDefinition(def);
            req.Id = baseReq.Id;
            req.Type = baseReq.Type;
            req.EventName = baseReq.EventName;
            req.DisplayName = baseReq.DisplayName;
            req.DisplayKey = baseReq.DisplayKey;
            req.Description = baseReq.Description;
            req.Markdown = baseReq.Markdown;
            req.Target = baseReq.Target;
            req.CustomDataInPinTemplate = baseReq.CustomDataInPinTemplate;
            req.CustomFlowOutPinTemplate = baseReq.CustomFlowOutPinTemplate;
            req.DataInPins = baseReq.DataInPins;
            req.DataOutPins = baseReq.DataOutPins;
            req.FlowOutPins = baseReq.FlowOutPins;
            req.Package = baseReq.Package;
            return req;
        }
    }

    public class NodeDefinitionAnalyzer
    {
        public List<NodeDefinition> AnalyzeAssembly(string assemblyPath)
        {
            var list = new List<NodeDefinition>();
            var ctx = new AssemblyLoadContext("FlowNodeDef", isCollectible: true);
            Assembly asm;
            try
            {
                asm = ctx.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load assembly: {ex.Message}");
            }

            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null)!.Cast<Type>().ToArray(); }

            foreach (var t in types)
            {
                if (t == null || t.IsAbstract || t.IsInterface || t.IsGenericTypeDefinition)
                    continue;

                var meta = t.GetCustomAttribute<FlowNodeMetaAttribute>();
                if (meta == null) continue;

                string id = meta.Id ?? t.FullName ?? t.Name;
                var nodeType = meta.Type;
                var target = meta.Target;
                string? eventName = meta.EventName;

                var desc = t.GetCustomAttribute<FlowNodeDescriptionMetaAttribute>();
                var displayName = desc?.DisplayName;
                var displayKey = desc?.DisplayKey;
                var description = desc?.Description;

                var pkg = new NodePackage
                {
                    Name = asm.GetName().Name,
                    Version = "latest",
                    AssemblyName = asm.GetName().Name,
                    ClassName = t.Name
                };

                // Pin attributes
                var dataInPins = t.GetCustomAttributes<FlowNodeDataInPinMetaAttribute>(true)
                    .Select(a => new DataInPinDefinition
                    {
                        Name = a.Name,
                        Type = a.Type,
                        Required = a.Required,
                        CanBeNull = a.Nullable
                    }).ToArray();

                var dataOutPins = t.GetCustomAttributes<FlowNodeDataOutPinMetaAttribute>(true)
                    .Select(a => new DataOutPinDefinition
                    {
                        Name = a.Name,
                        Type = a.Type
                    }).ToArray();

                var flowOutPins = t.GetCustomAttributes<FlowNodeOutPinMetaAttribute>(true)
                    .Select(a => new FlowOutPinDefinition
                    {
                        Name = a.Name
                    }).ToArray();

                CustomDataInPinTemplateDefinition? customIn = null;
                var customInAttr = t.GetCustomAttribute<FlowNodeCustomDataInPinTemplateMetaAttribute>(true);
                if (customInAttr != null)
                {
                    customIn = new CustomDataInPinTemplateDefinition
                    {
                        Type = customInAttr.Type,
                        Required = customInAttr.Required
                    };
                }

                CustomFlowOutPinTemplateDefinition? customOut = null;
                var customOutAttr = t.GetCustomAttribute<FlowNodeCustomOutPinTemplateMetaAttribute>(true);
                if (customOutAttr != null)
                {
                    customOut = new CustomFlowOutPinTemplateDefinition
                    {
                        Name = customOutAttr.Name
                    };
                }

                // Try to get embedded markdown content, fallback to generated markdown
                var embeddedMarkdown = GetEmbeddedMarkdown(asm, t.Name);
                var markdown = !string.IsNullOrWhiteSpace(embeddedMarkdown) 
                    ? embeddedMarkdown 
                    : $"# {displayName ?? t.Name}\n\nNode ID: `{id}`\nType: `{t.FullName}`";

                var def = new NodeDefinition
                {
                    Id = id,
                    Type = nodeType,
                    DisplayName = displayName,
                    DisplayKey = displayKey,
                    Description = description,
                    Markdown = markdown,
                    Target = target,
                    CustomDataInPinTemplate = customIn,
                    CustomFlowOutPinTemplate = customOut,
                    DataInPins = dataInPins,
                    DataOutPins = dataOutPins,
                    FlowOutPins = flowOutPins,
                    Package = pkg,
                    EventName = eventName
                };

                list.Add(def);
            }

            try { ctx.Unload(); } catch { }
            return list;
        }

        private string? GetEmbeddedMarkdown(Assembly assembly, string className)
        {
            try
            {
                var resourceName = $"{className}.md";
                var resourceNames = assembly.GetManifestResourceNames();
                
                // Look for exact match first
                var exactMatch = resourceNames.FirstOrDefault(name => name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));
                if (exactMatch != null)
                {
                    using var stream = assembly.GetManifestResourceStream(exactMatch);
                    if (stream != null)
                    {
                        using var reader = new StreamReader(stream);
                        return reader.ReadToEnd();
                    }
                }

                // Look for any .md file that contains the class name
                var partialMatch = resourceNames.FirstOrDefault(name => 
                    name.Contains(className, StringComparison.OrdinalIgnoreCase) && 
                    name.EndsWith(".md", StringComparison.OrdinalIgnoreCase));
                
                if (partialMatch != null)
                {
                    using var stream = assembly.GetManifestResourceStream(partialMatch);
                    if (stream != null)
                    {
                        using var reader = new StreamReader(stream);
                        return reader.ReadToEnd();
                    }
                }
            }
            catch
            {
                // Silently ignore any errors when trying to read embedded resources
            }

            return null;
        }
    }
}
