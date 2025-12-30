using oxs.Configuration;
using Simplic.OxS.Flow.Meta;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace oxs.Commands.Flow;

/// <summary>
/// Command for pushing flow node definitions from a .NET assembly to the OXS platform.
/// </summary>
public class FlowPushNodeDefCommand : Command<FlowPushNodeDefSettings>
{
    /// <summary>
    /// Executes the flow node definition push command with the specified settings.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="settings">The settings for pushing node definitions.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An exit code indicating success (0) or failure (1).</returns>
    public override int Execute(CommandContext context, FlowPushNodeDefSettings settings, CancellationToken cancellationToken)
    {
        return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously executes the flow node definition push process, analyzing the assembly and uploading definitions to the API.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="settings">The settings for pushing node definitions.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation with an exit code indicating success (0) or failure (1).</returns>
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

/// <summary>
/// Represents package information for a flow node.
/// </summary>
public class NodePackage
{
    /// <summary>
    /// Gets or sets the name of the package.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the version of the package.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the assembly name containing the node.
    /// </summary>
    public string? AssemblyName { get; set; }

    /// <summary>
    /// Gets or sets the class name of the node.
    /// </summary>
    public string? ClassName { get; set; }
}

/// <summary>
/// Represents a data input pin definition for a flow node.
/// </summary>
public class DataInPinDefinition
{
    /// <summary>
    /// Gets or sets the name of the data input pin.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the data type of the input pin.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this pin is required.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this pin can accept null values.
    /// </summary>
    public bool CanBeNull { get; set; }
}

/// <summary>
/// Represents a data output pin definition for a flow node.
/// </summary>
public class DataOutPinDefinition
{
    /// <summary>
    /// Gets or sets the name of the data output pin.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the data type of the output pin.
    /// </summary>
    public string? Type { get; set; }
}

/// <summary>
/// Represents a flow output pin definition for a flow node.
/// </summary>
public class FlowOutPinDefinition
{
    /// <summary>
    /// Gets or sets the name of the flow output pin.
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// Represents a custom data input pin template definition.
/// </summary>
public class CustomDataInPinTemplateDefinition
{
    /// <summary>
    /// Gets or sets the data type for the custom input pin template.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the custom input pin is required.
    /// </summary>
    public bool Required { get; set; }
}

/// <summary>
/// Represents a custom flow output pin template definition.
/// </summary>
public class CustomFlowOutPinTemplateDefinition
{
    /// <summary>
    /// Gets or sets the name of the custom flow output pin template.
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// Represents a complete flow node definition with all its properties and pin configurations.
/// </summary>
public class NodeDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier of the node.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the type of the node.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the display name of the node.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the display key for localization.
    /// </summary>
    public string? DisplayKey { get; set; }

    /// <summary>
    /// Gets or sets the description of the node.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the markdown documentation for the node.
    /// </summary>
    public string? Markdown { get; set; }

    /// <summary>
    /// Gets or sets the target platform or execution environment.
    /// </summary>
    public string Target { get; set; }

    /// <summary>
    /// Gets or sets the custom data input pin template configuration.
    /// </summary>
    public CustomDataInPinTemplateDefinition? CustomDataInPinTemplate { get; set; }

    /// <summary>
    /// Gets or sets the custom flow output pin template configuration.
    /// </summary>
    public CustomFlowOutPinTemplateDefinition? CustomFlowOutPinTemplate { get; set; }

    /// <summary>
    /// Gets or sets the array of data input pin definitions.
    /// </summary>
    public DataInPinDefinition[]? DataInPins { get; set; }

    /// <summary>
    /// Gets or sets the array of data output pin definitions.
    /// </summary>
    public DataOutPinDefinition[]? DataOutPins { get; set; }

    /// <summary>
    /// Gets or sets the array of flow output pin definitions.
    /// </summary>
    public FlowOutPinDefinition[]? FlowOutPins { get; set; }

    /// <summary>
    /// Gets or sets the package information for the node.
    /// </summary>
    public NodePackage? Package { get; set; }

    /// <summary>
    /// Gets or sets the event name associated with the node.
    /// </summary>
    public string? EventName { get; set; }
}

/// <summary>
/// Represents a request to create a new node definition via POST API call.
/// </summary>
public class PostNodeDefinitionRequest
{
    /// <summary>
    /// Gets or sets the unique identifier of the node.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the type of the node.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the event name associated with the node.
    /// </summary>
    public string? EventName { get; set; }

    /// <summary>
    /// Gets or sets the display name of the node.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the display key for localization.
    /// </summary>
    public string? DisplayKey { get; set; }

    /// <summary>
    /// Gets or sets the description of the node.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the markdown documentation for the node.
    /// </summary>
    public string? Markdown { get; set; }

    /// <summary>
    /// Gets or sets the target platform or execution environment.
    /// </summary>
    public string Target { get; set; }

    /// <summary>
    /// Gets or sets the custom data input pin template configuration.
    /// </summary>
    public CustomDataInPinTemplateDefinition? CustomDataInPinTemplate { get; set; }

    /// <summary>
    /// Gets or sets the custom flow output pin template configuration.
    /// </summary>
    public CustomFlowOutPinTemplateDefinition? CustomFlowOutPinTemplate { get; set; }

    /// <summary>
    /// Gets or sets the array of data input pin definitions.
    /// </summary>
    public DataInPinDefinition[]? DataInPins { get; set; }

    /// <summary>
    /// Gets or sets the array of data output pin definitions.
    /// </summary>
    public DataOutPinDefinition[]? DataOutPins { get; set; }

    /// <summary>
    /// Gets or sets the array of flow output pin definitions.
    /// </summary>
    public FlowOutPinDefinition[]? FlowOutPins { get; set; }

    /// <summary>
    /// Gets or sets the package information for the node.
    /// </summary>
    public NodePackage? Package { get; set; }

    /// <summary>
    /// Creates a PostNodeDefinitionRequest from a NodeDefinition object.
    /// </summary>
    /// <param name="def">The node definition to convert.</param>
    /// <returns>A new PostNodeDefinitionRequest instance.</returns>
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

/// <summary>
/// Represents a request to update an existing node definition via PUT API call.
/// </summary>
public class PutNodeDefinitionRequest : PostNodeDefinitionRequest
{
    /// <summary>
    /// Creates a PutNodeDefinitionRequest from a NodeDefinition object.
    /// </summary>
    /// <param name="def">The node definition to convert.</param>
    /// <returns>A new PutNodeDefinitionRequest instance.</returns>
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

/// <summary>
/// Provides functionality to analyze .NET assemblies and extract flow node definitions based on attributes.
/// </summary>
public class NodeDefinitionAnalyzer
{
    /// <summary>
    /// Analyzes the specified assembly and extracts all flow node definitions.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly file to analyze.</param>
    /// <returns>A list of node definitions found in the assembly.</returns>
    /// <exception cref="Exception">Thrown when the assembly cannot be loaded or analyzed.</exception>
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

    /// <summary>
    /// Attempts to retrieve embedded markdown documentation for a specific class from the assembly's manifest resources.
    /// </summary>
    /// <param name="assembly">The assembly to search for embedded resources.</param>
    /// <param name="className">The name of the class to find documentation for.</param>
    /// <returns>The markdown content if found; otherwise, null.</returns>
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
