using Spectre.Console;
using Spectre.Console.Cli;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace oxs.Commands.Manifest;

public class PlaceholderInfo
{
    public string Name { get; set; } = string.Empty;
    public bool IsOptional { get; set; }
    public string? DefaultValue { get; set; }
    public string FullPlaceholder { get; set; } = string.Empty;
}

public class ManifestAddDeploymentCommand : Command<ManifestAddDeploymentSettings>
{
    public override int Execute(CommandContext context, ManifestAddDeploymentSettings settings, CancellationToken cancellationToken)
    {
        return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
    }

    private async Task<int> ExecuteAsync(CommandContext context, ManifestAddDeploymentSettings settings, CancellationToken cancellationToken)
    {
        // Get template name (required)
        if (string.IsNullOrWhiteSpace(settings.Template))
        {
            // Get available templates
            var availableTemplates = GetAvailableTemplates();
            if (!availableTemplates.Any())
            {
                AnsiConsole.MarkupLine("[red]No templates found[/]");
                return 1;
            }

            settings.Template = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Please select a [green]template[/]?")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to select the template)[/]")
                    .AddChoices(availableTemplates));
        }

        // Validate template exists
        if (!DoesTemplateExist(settings.Template))
        {
            var availableTemplates = GetAvailableTemplates();
            AnsiConsole.MarkupLine($"[red]Template '{settings.Template}' not found. Available templates: {string.Join(", ", availableTemplates)}[/]");
            return 1;
        }

        // Get deployment name if not provided
        if (string.IsNullOrWhiteSpace(settings.Name))
        {
            settings.Name = AnsiConsole.Ask<string>("Deployment [green]name[/]?");
            if (string.IsNullOrWhiteSpace(settings.Name))
            {
                AnsiConsole.MarkupLine("[red]Deployment name is required[/]");
                return 1;
            }
        }

        // Create deployments directory if it doesn't exist
        var deploymentsDir = Path.Combine(Directory.GetCurrentDirectory(), "deployments");
        if (!Directory.Exists(deploymentsDir))
        {
            try
            {
                Directory.CreateDirectory(deploymentsDir);
                AnsiConsole.MarkupLine($"[green]Created directory:[/] {deploymentsDir}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to create deployments directory: {ex.Message}[/]");
                return 1;
            }
        }

        // Load all template files for this template
        Dictionary<string, string> templateFiles;
        try
        {
            templateFiles = LoadAllTemplateFiles(settings.Template);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to load template '{settings.Template}': {ex.Message}[/]");
            return 1;
        }

        // Extract all placeholders from all template files
        var allPlaceholders = ExtractPlaceholders(templateFiles);
        
        // Prompt user for values for each placeholder
        var placeholderValues = new Dictionary<string, string> { { "name", settings.Name } };
        
        foreach (var placeholder in allPlaceholders.Where(p => p.Name != "name"))
        {
            string userValue;
            if (placeholder.IsOptional)
            {
                var prompt = $"Enter value for [green]{placeholder.Name}[/] (optional";
                if (!string.IsNullOrEmpty(placeholder.DefaultValue))
                {
                    prompt += $", default: {placeholder.DefaultValue}";
                }
                prompt += "):";
                
                userValue = AnsiConsole.Ask<string>(prompt, placeholder.DefaultValue ?? "");
            }
            else
            {
                var prompt = $"Enter value for [green]{placeholder.Name}[/]:";
                if (!string.IsNullOrEmpty(placeholder.DefaultValue))
                {
                    userValue = AnsiConsole.Ask<string>(prompt, placeholder.DefaultValue);
                }
                else
                {
                    userValue = AnsiConsole.Ask<string>(prompt);
                }
            }
            
            placeholderValues[placeholder.Name] = userValue;
        }

        // Replace placeholders in all template files
        var processedFiles = new Dictionary<string, string>();
        foreach (var templateFile in templateFiles)
        {
            var content = templateFile.Value;
            
            // Replace name placeholder first
            content = content.Replace("<name>", settings.Name);
            
            // Replace all other placeholders with their full syntax
            foreach (var placeholder in allPlaceholders.Where(p => p.Name != "name"))
            {
                var value = placeholderValues.ContainsKey(placeholder.Name) ? placeholderValues[placeholder.Name] : "";
                content = content.Replace(placeholder.FullPlaceholder, value);
            }
            
            processedFiles[templateFile.Key] = content;
        }

        // Check if any files already exist
        var existingFiles = new List<string>();
        foreach (var templateFile in processedFiles)
        {
            var fileName = GetOutputFileName(templateFile.Key, settings.Name);
            var filePath = Path.Combine(deploymentsDir, fileName);
            if (File.Exists(filePath))
            {
                existingFiles.Add(fileName);
            }
        }

        if (existingFiles.Any())
        {
            var overwrite = AnsiConsole.Confirm($"The following files already exist: {string.Join(", ", existingFiles)}. Overwrite?");
            if (!overwrite)
            {
                AnsiConsole.MarkupLine("[yellow]Operation cancelled[/]");
                return 0;
            }
        }

        try
        {
            var createdFiles = new List<string>();
            
            foreach (var processedFile in processedFiles)
            {
                var fileName = GetOutputFileName(processedFile.Key, settings.Name);
                var filePath = Path.Combine(deploymentsDir, fileName);
                
                string finalContent = processedFile.Value;
                
                // Pretty print JSON files
                if (processedFile.Key.EndsWith(".json"))
                {
                    var jsonDocument = JsonDocument.Parse(finalContent);
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    finalContent = JsonSerializer.Serialize(jsonDocument.RootElement, options);
                }
                
                await File.WriteAllTextAsync(filePath, finalContent, cancellationToken);
                createdFiles.Add(filePath);
            }
            
            AnsiConsole.MarkupLine($"[green]✓[/] Deployment created successfully with {createdFiles.Count} files:");
            foreach (var file in createdFiles)
            {
                AnsiConsole.MarkupLine($"  [yellow]{file}[/]");
            }
            AnsiConsole.MarkupLine($"[blue]Template:[/] {settings.Template}");
            AnsiConsole.MarkupLine($"[blue]Name:[/] {settings.Name}");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to create deployment files: {ex.Message}[/]");
            return 1;
        }
    }

    private List<string> GetAvailableTemplates()
    {
        var templates = new List<string>();
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        foreach (var resourceName in resourceNames)
        {
            if (resourceName.Contains(".Templates."))
            {
                // Extract template name from resource name
                // Example: oxs.Commands.Manifest.Templates.logistics.shipment.status.template.shipment.status.json
                var parts = resourceName.Split('.');
                var templatesIndex = Array.IndexOf(parts, "Templates");
                if (templatesIndex >= 0 && templatesIndex < parts.Length - 2)
                {
                    // Get the directory name (template name) which is the part after "Templates"
                    var templateName = parts[templatesIndex + 1];
                    // Handle multi-part template names like "logistics.shipment.status"
                    var templateParts = new List<string> { templateName };
                    for (int i = templatesIndex + 2; i < parts.Length - 1; i++)
                    {
                        if (!parts[i].StartsWith("template") && !parts[i].StartsWith("README"))
                        {
                            templateParts.Add(parts[i]);
                        }
                        else
                        {
                            break;
                        }
                    }
                    var fullTemplateName = string.Join(".", templateParts);
                    if (!templates.Contains(fullTemplateName))
                    {
                        templates.Add(fullTemplateName);
                    }
                }
            }
        }

        return templates.OrderBy(t => t).ToList();
    }

    private bool DoesTemplateExist(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();
        
        // Look for any resource that matches the template pattern
        var expectedResourcePattern = $".Templates.{templateName}.";
        return resourceNames.Any(name => name.Contains(expectedResourcePattern));
    }

    private Dictionary<string, string> LoadAllTemplateFiles(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();
        var templateFiles = new Dictionary<string, string>();
        
        // Find all resources that match the template
        var expectedResourcePattern = $".Templates.{templateName}.";
        var matchingResources = resourceNames.Where(name => name.Contains(expectedResourcePattern)).ToList();
        
        if (!matchingResources.Any())
        {
            throw new FileNotFoundException($"No template resources found for '{templateName}'");
        }

        foreach (var resourceName in matchingResources)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new FileNotFoundException($"Could not load template resource '{resourceName}'");
            }

            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            
            // Extract the file name from the resource name
            var fileName = ExtractFileNameFromResourceName(resourceName, templateName);
            templateFiles[fileName] = content;
        }

        return templateFiles;
    }

    private string ExtractFileNameFromResourceName(string resourceName, string templateName)
    {
        // Example: oxs.Commands.Manifest.Templates.logistics.shipment.status.template.shipment.status.json
        // Should return: template.shipment.status.json
        var templatePattern = $".Templates.{templateName}.";
        var startIndex = resourceName.IndexOf(templatePattern) + templatePattern.Length;
        return resourceName.Substring(startIndex);
    }

    private List<PlaceholderInfo> ExtractPlaceholders(Dictionary<string, string> templateFiles)
    {
        var placeholders = new Dictionary<string, PlaceholderInfo>();
        var placeholderPattern = new Regex(@"<([^>]+)>", RegexOptions.Compiled);
        
        foreach (var templateFile in templateFiles.Values)
        {
            var matches = placeholderPattern.Matches(templateFile);
            foreach (Match match in matches)
            {
                var fullPlaceholder = match.Value; // e.g., "<roles:optional>"
                var placeholderContent = match.Groups[1].Value; // e.g., "roles:optional"
                
                var placeholderInfo = ParsePlaceholderContent(placeholderContent, fullPlaceholder);
                
                // Use the placeholder name as key to avoid duplicates
                if (!placeholders.ContainsKey(placeholderInfo.Name))
                {
                    placeholders[placeholderInfo.Name] = placeholderInfo;
                }
            }
        }
        
        return placeholders.Values.ToList();
    }

    private PlaceholderInfo ParsePlaceholderContent(string placeholderContent, string fullPlaceholder)
    {
        var parts = placeholderContent.Split(':');
        var placeholderInfo = new PlaceholderInfo
        {
            Name = parts[0],
            FullPlaceholder = fullPlaceholder,
            IsOptional = false,
            DefaultValue = null
        };
        
        // Parse modifiers
        for (int i = 1; i < parts.Length; i++)
        {
            var modifier = parts[i];
            if (modifier == "optional")
            {
                placeholderInfo.IsOptional = true;
            }
            else if (modifier.StartsWith("default="))
            {
                placeholderInfo.DefaultValue = modifier.Substring(8); // Remove "default="
            }
        }
        
        return placeholderInfo;
    }

    private string GetOutputFileName(string templateFileName, string deploymentName)
    {
        // Convert template file names to output file names
        // e.g., template.shipment.status.json -> deploymentName.json
        // e.g., README.md -> deploymentName-README.md
        
        if (templateFileName.StartsWith("template.") && templateFileName.EndsWith(".json"))
        {
            return $"{deploymentName}.json";
        }
        else if (templateFileName == "README.md")
        {
            return $"{deploymentName}-README.md";
        }
        else
        {
            // For any other file, prefix with deployment name
            return $"{deploymentName}-{templateFileName}";
        }
    }
}