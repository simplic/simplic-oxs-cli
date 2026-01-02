using oxs.Configuration;
using oxs.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text;
using System.Text.Json;

namespace oxs.Commands.Service;

/// <summary>
/// Command for downloading service definitions from the OXS API and saving them to a local directory.
/// </summary>
public class ServiceGetDefinitionCommand : Command<ServiceGetDefinitionSettings>
{
    /// <summary>
    /// Executes the service get-definition command with the specified settings.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="settings">The command settings.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An exit code indicating success (0) or failure (1).</returns>
    public override int Execute(CommandContext context, ServiceGetDefinitionSettings settings, CancellationToken cancellationToken)
    {
        return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously executes the service definition download process.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="settings">The command settings.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation with an exit code.</returns>
    private async Task<int> ExecuteAsync(CommandContext context, ServiceGetDefinitionSettings settings, CancellationToken cancellationToken)
    {
        // Validate endpoint
        if (string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            AnsiConsole.MarkupLine("[red]Endpoint is required. Use -e or --endpoint to specify the service endpoint.[/]");
            return 1;
        }

        // Validate version
        if (string.IsNullOrWhiteSpace(settings.Version))
        {
            AnsiConsole.MarkupLine("[red]Version is required. Use -v or --version to specify the service version.[/]");
            return 1;
        }

        var serviceName = $"{settings.Endpoint}-{settings.Version}";
        AnsiConsole.MarkupLine($"[blue]Downloading service definition for:[/] [yellow]{serviceName}[/]");
        AnsiConsole.MarkupLine($"[blue]Using configuration section:[/] [yellow]{settings.Section}[/]");

        // Construct the endpoint URL: <endpoint>-api/<version>/ServiceDefinition
        var endpoint = $"{settings.Endpoint}-api/{settings.Version}/ServiceDefinition";

        AnsiConsole.MarkupLine($"[blue]Fetching from:[/] [yellow]{endpoint}[/]");

        // Use HttpService to make the request
        using var httpService = new HttpService();
        var result = await httpService.ExecuteRequestAsync(
            "get",
            endpoint,
            settings.Section);

        if (!result.Success)
        {
            AnsiConsole.MarkupLine($"[red]Failed to download service definition: {result.ErrorMessage}[/]");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(result.ResponseBody))
        {
            AnsiConsole.MarkupLine("[red]Received empty response from server[/]");
            return 1;
        }

        // Parse the response
        ServiceDefinition? serviceDefinition;
        try
        {
            serviceDefinition = JsonSerializer.Deserialize<ServiceDefinition>(result.ResponseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (serviceDefinition == null)
            {
                AnsiConsole.MarkupLine("[red]Failed to parse service definition[/]");
                return 1;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to parse service definition: {ex.Message}[/]");
            return 1;
        }

        // Create output directory: <output>/<endpoint-version>
        var outputPath = Path.Combine(
            Path.GetFullPath(settings.OutputDirectory),
            serviceName);

        try
        {
            Directory.CreateDirectory(outputPath);
            AnsiConsole.MarkupLine($"[green]Created directory:[/] [yellow]{outputPath}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to create directory {outputPath}: {ex.Message}[/]");
            return 1;
        }

        // Save the complete service definition as JSON
        var serviceDefPath = Path.Combine(outputPath, "service-definition.json");
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var jsonContent = JsonSerializer.Serialize(serviceDefinition, jsonOptions);
            await File.WriteAllTextAsync(serviceDefPath, jsonContent, cancellationToken);
            AnsiConsole.MarkupLine($"[green]?[/] Saved service definition: [yellow]{serviceDefPath}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to save service definition: {ex.Message}[/]");
            return 1;
        }

        // Save gRPC proto files if available
        if (serviceDefinition.GrpcDefinitions != null && serviceDefinition.GrpcDefinitions.Count > 0)
        {
            AnsiConsole.MarkupLine($"[blue]Found {serviceDefinition.GrpcDefinitions.Count} gRPC definition(s)[/]");

            foreach (var grpcDef in serviceDefinition.GrpcDefinitions)
            {
                if (string.IsNullOrWhiteSpace(grpcDef.ProtoFile))
                    continue;

                try
                {
                    // Decode the base64-encoded proto file
                    var protoContent = Encoding.UTF8.GetString(Convert.FromBase64String(grpcDef.ProtoFile));

                    // Create a filename based on the package name
                    var protoFileName = $"{grpcDef.Package}.proto";
                    var protoFilePath = Path.Combine(outputPath, protoFileName);

                    await File.WriteAllTextAsync(protoFilePath, protoContent, cancellationToken);
                    AnsiConsole.MarkupLine($"[green]?[/] Saved proto file: [yellow]{protoFilePath}[/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning: Failed to save proto file for package {grpcDef.Package}: {ex.Message}[/]");
                }
            }
        }

        // Save GraphQL schema if available
        if (!string.IsNullOrWhiteSpace(serviceDefinition.GraphQLSchema))
        {
            try
            {
                var graphqlPath = Path.Combine(outputPath, "schema.graphql");
                await File.WriteAllTextAsync(graphqlPath, serviceDefinition.GraphQLSchema, cancellationToken);
                AnsiConsole.MarkupLine($"[green]✓[/] Saved GraphQL schema: [yellow]{graphqlPath}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Failed to save GraphQL schema: {ex.Message}[/]");
            }
        }

        // Download Swagger JSON if available
        if (!string.IsNullOrWhiteSpace(serviceDefinition.SwaggerJsonUrl))
        {
            try
            {
                // Remove leading slash if present (HttpService will add the base URL)
                var swaggerUrl = serviceDefinition.SwaggerJsonUrl.TrimStart('/');
                AnsiConsole.MarkupLine($"[blue]Downloading Swagger JSON from:[/] [yellow]{swaggerUrl}[/]");
                
                // Create a new HttpService instance for this request
                using var swaggerHttpService = new HttpService();
                var swaggerResult = await swaggerHttpService.ExecuteRequestAsync(
                    "get",
                    swaggerUrl.ToLower(),
                    settings.Section);

                if (swaggerResult.Success && !string.IsNullOrWhiteSpace(swaggerResult.ResponseBody))
                {
                    var swaggerPath = Path.Combine(outputPath, "swagger.json");
                    
                    // Try to format as JSON if valid
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(swaggerResult.ResponseBody);
                        var formattedJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true });
                        await File.WriteAllTextAsync(swaggerPath, formattedJson, cancellationToken);
                    }
                    catch
                    {
                        // If not valid JSON, save as-is
                        await File.WriteAllTextAsync(swaggerPath, swaggerResult.ResponseBody, cancellationToken);
                    }
                    
                    AnsiConsole.MarkupLine($"[green]✓[/] Saved Swagger JSON: [yellow]{swaggerPath}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning: Failed to download Swagger JSON: {swaggerResult.ErrorMessage}[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Failed to download Swagger JSON: {ex.Message}[/]");
            }
        }

        // Download Model Definition if available
        if (!string.IsNullOrWhiteSpace(serviceDefinition.ModelDefinitionUrl))
        {
            try
            {
                // Remove leading slash if present (HttpService will add the base URL)
                var modelUrl = serviceDefinition.ModelDefinitionUrl.TrimStart('/');
                AnsiConsole.MarkupLine($"[blue]Downloading Model Definition from:[/] [yellow]{modelUrl}[/]");
                
                // Create a new HttpService instance for this request
                using var modelHttpService = new HttpService();
                var modelResult = await modelHttpService.ExecuteRequestAsync(
                    "get",
                    modelUrl.ToLower(),
                    settings.Section);

                if (modelResult.Success && !string.IsNullOrWhiteSpace(modelResult.ResponseBody))
                {
                    var modelPath = Path.Combine(outputPath, "model-definition.json");
                    
                    // Try to format as JSON if valid
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(modelResult.ResponseBody);
                        var formattedJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true });
                        await File.WriteAllTextAsync(modelPath, formattedJson, cancellationToken);
                    }
                    catch
                    {
                        // If not valid JSON, save as-is
                        await File.WriteAllTextAsync(modelPath, modelResult.ResponseBody, cancellationToken);
                    }
                    
                    AnsiConsole.MarkupLine($"[green]✓[/] Saved Model Definition: [yellow]{modelPath}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning: Failed to download Model Definition: {modelResult.ErrorMessage}[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Failed to download Model Definition: {ex.Message}[/]");
            }
        }

        // Create README with all information
        if (!string.IsNullOrWhiteSpace(serviceDefinition.SwaggerJsonUrl))
        {
            try
            {
                var readmePath = Path.Combine(outputPath, "README.md");
                var readmeContent = $@"# {serviceDefinition.Name} Service Definition

- **Name:** {serviceDefinition.Name}
- **Version:** {serviceDefinition.Version}
- **Type:** {serviceDefinition.Type}
- **Base URL:** {serviceDefinition.BaseUrl}
- **Swagger JSON:** {serviceDefinition.SwaggerJsonUrl}
- **Model Definition:** {serviceDefinition.ModelDefinitionUrl}

## Files

- `service-definition.json` - Complete service definition
{(!string.IsNullOrWhiteSpace(serviceDefinition.SwaggerJsonUrl) ? "- `swagger.json` - OpenAPI/Swagger specification" : "")}
{(!string.IsNullOrWhiteSpace(serviceDefinition.ModelDefinitionUrl) ? "- `model-definition.json` - Model definition specification" : "")}
{(serviceDefinition.GrpcDefinitions?.Count > 0 ? $"- `*.proto` - gRPC protocol buffer definitions ({serviceDefinition.GrpcDefinitions.Count} file(s))" : "")}
{(!string.IsNullOrWhiteSpace(serviceDefinition.GraphQLSchema) ? "- `schema.graphql` - GraphQL schema definition" : "")}

## Contracts

{(serviceDefinition.Contracts?.Count > 0 ? string.Join("\n", serviceDefinition.Contracts.Select(c => $"- Provider: {c.ProviderName}")) : "No contracts defined")}
";

                await File.WriteAllTextAsync(readmePath, readmeContent, cancellationToken);
                AnsiConsole.MarkupLine($"[green]✓[/] Saved README: [yellow]{readmePath}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Failed to save README: {ex.Message}[/]");
            }
        }

        AnsiConsole.MarkupLine($"[green]? Service definition for '{serviceName}' downloaded successfully![/]");
        return 0;
    }
}

/// <summary>
/// Represents a service definition response from the OXS API.
/// </summary>
public class ServiceDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string? SwaggerJsonUrl { get; set; }
    public string? ModelDefinitionUrl { get; set; }
    public List<GrpcDefinition>? GrpcDefinitions { get; set; }
    public string Type { get; set; } = string.Empty;
    public List<Contract>? Contracts { get; set; }
    public string? GraphQLSchema { get; set; }
}

/// <summary>
/// Represents a gRPC service definition.
/// </summary>
public class GrpcDefinition
{
    public string Package { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string ProtoFile { get; set; } = string.Empty;
}

/// <summary>
/// Represents a service contract.
/// </summary>
public class Contract
{
    public string ProviderName { get; set; } = string.Empty;
    public List<EndpointContract>? EndpointContracts { get; set; }
    public List<EndpointContract>? RequiredEndpointContracts { get; set; }
}

/// <summary>
/// Represents an endpoint contract.
/// </summary>
public class EndpointContract
{
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
}
