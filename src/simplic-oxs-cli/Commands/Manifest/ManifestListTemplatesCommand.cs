using Spectre.Console;
using Spectre.Console.Cli;
using System.Reflection;

namespace oxs.Commands.Manifest;

public class ManifestListTemplatesCommand : Command<ManifestListTemplatesSettings>
{
    public override int Execute(CommandContext context, ManifestListTemplatesSettings settings, CancellationToken cancellationToken)
    {
        return ExecuteAsync(context, settings, cancellationToken).GetAwaiter().GetResult();
    }

    private async Task<int> ExecuteAsync(CommandContext context, ManifestListTemplatesSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            var templates = GetAvailableTemplates();
            
            if (!templates.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No templates found[/]");
                return 0;
            }

            AnsiConsole.MarkupLine($"[green]Available deployment templates ({templates.Count}):[/]");
            AnsiConsole.WriteLine();

            if (settings.ShowDetailed)
            {
                // Show detailed information for each template
                foreach (var template in templates)
                {
                    ShowDetailedTemplateInfo(template);
                    if (template != templates.Last())
                    {
                        AnsiConsole.WriteLine();
                    }
                }
            }
            else
            {
                // Create a table to display template information
                var table = new Table();
                table.AddColumn("Template Name");
                table.AddColumn("Files");
                table.AddColumn("Description");

                foreach (var template in templates)
                {
                    var templateFiles = GetTemplateFiles(template);
                    var filesDisplay = string.Join(", ", templateFiles.Select(f => $"[dim]{f}[/]"));
                    var description = GetTemplateDescription(template);
                    
                    table.AddRow(
                        $"[cyan]{template}[/]",
                        filesDisplay,
                        description ?? "[dim]No description available[/]"
                    );
                }

                AnsiConsole.Write(table);
            }
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[blue]Usage:[/] oxs manifest add-deployment -t <template-name> -n <deployment-name>");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to list templates: {ex.Message}[/]");
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

    private List<string> GetTemplateFiles(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();
        var templateFiles = new List<string>();
        
        // Find all resources that match the template
        var expectedResourcePattern = $".Templates.{templateName}.";
        var matchingResources = resourceNames.Where(name => name.Contains(expectedResourcePattern)).ToList();

        foreach (var resourceName in matchingResources)
        {
            // Extract the file name from the resource name
            var fileName = ExtractFileNameFromResourceName(resourceName, templateName);
            templateFiles.Add(fileName);
        }

        return templateFiles.OrderBy(f => f).ToList();
    }

    private string ExtractFileNameFromResourceName(string resourceName, string templateName)
    {
        // Example: oxs.Commands.Manifest.Templates.logistics.shipment.status.template.shipment.status.json
        // Should return: template.shipment.status.json
        var templatePattern = $".Templates.{templateName}.";
        var startIndex = resourceName.IndexOf(templatePattern) + templatePattern.Length;
        return resourceName.Substring(startIndex);
    }

    private string? GetTemplateDescription(string templateName)
    {
        try
        {
            // Try to find a README file for this template
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            
            var expectedResourcePattern = $".Templates.{templateName}.README.md";
            var readmeResource = resourceNames.FirstOrDefault(name => name.Contains(expectedResourcePattern));
            
            if (readmeResource != null)
            {
                using var stream = assembly.GetManifestResourceStream(readmeResource);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    var content = reader.ReadToEnd();
                    
                    // Extract the first line or first paragraph as description
                    var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 0)
                    {
                        var firstLine = lines[0].Trim();
                        // Remove markdown heading syntax
                        if (firstLine.StartsWith("#"))
                        {
                            firstLine = firstLine.TrimStart('#').Trim();
                        }
                        return firstLine;
                    }
                }
            }

            // If no README, try to infer description from template name
            return GetInferredDescription(templateName);
        }
        catch
        {
            // If anything fails, return null
            return null;
        }
    }

    private string? GetInferredDescription(string templateName)
    {
        // Simple heuristics to provide descriptions based on template names
        return templateName switch
        {
            "logistics.shipment.status" => "Template for creating shipment status deployments",
            _ when templateName.Contains("shipment") => "Shipment-related deployment template",
            _ when templateName.Contains("logistics") => "Logistics deployment template",
            _ => null
        };
    }

    private void ShowDetailedTemplateInfo(string templateName)
    {
        AnsiConsole.MarkupLine($"[bold cyan]{templateName}[/]");
        AnsiConsole.MarkupLine($"[dim]Template: {templateName}[/]");

        var description = GetTemplateDescription(templateName);
        if (!string.IsNullOrEmpty(description))
        {
            AnsiConsole.MarkupLine($"[dim]Description: {description}[/]");
        }

        var templateFiles = GetTemplateFiles(templateName);
        AnsiConsole.MarkupLine($"[dim]Files ({templateFiles.Count}):[/]");
        foreach (var file in templateFiles)
        {
            AnsiConsole.MarkupLine($"  [yellow]• {file}[/]");
        }

        // Try to show placeholders information
        var placeholders = GetTemplatePlaceholders(templateName);
        if (placeholders.Any())
        {
            AnsiConsole.MarkupLine($"[dim]Placeholders ({placeholders.Count}):[/]");
            foreach (var placeholder in placeholders)
            {
                var modifiers = new List<string>();
                if (placeholder.IsOptional) modifiers.Add("optional");
                if (!string.IsNullOrEmpty(placeholder.DefaultValue)) modifiers.Add($"default={placeholder.DefaultValue}");
                
                var modifierText = modifiers.Any() ? $" [dim]({string.Join(", ", modifiers)})[/]" : "";
                AnsiConsole.MarkupLine($"  [green]• {placeholder.Name}[/]{modifierText}");
            }
        }

        AnsiConsole.MarkupLine($"[dim]Usage: oxs manifest add-deployment -t {templateName} -n <deployment-name>[/]");
    }

    private List<PlaceholderInfo> GetTemplatePlaceholders(string templateName)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            var templateFiles = new Dictionary<string, string>();
            
            // Find all resources that match the template
            var expectedResourcePattern = $".Templates.{templateName}.";
            var matchingResources = resourceNames.Where(name => name.Contains(expectedResourcePattern)).ToList();

            foreach (var resourceName in matchingResources)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) continue;

                using var reader = new StreamReader(stream);
                var content = reader.ReadToEnd();
                var fileName = ExtractFileNameFromResourceName(resourceName, templateName);
                templateFiles[fileName] = content;
            }

            return ExtractPlaceholders(templateFiles);
        }
        catch
        {
            return new List<PlaceholderInfo>();
        }
    }

    private List<PlaceholderInfo> ExtractPlaceholders(Dictionary<string, string> templateFiles)
    {
        var placeholders = new Dictionary<string, PlaceholderInfo>();
        var placeholderPattern = new System.Text.RegularExpressions.Regex(@"<([^>]+)>", System.Text.RegularExpressions.RegexOptions.Compiled);
        
        foreach (var templateFile in templateFiles.Values)
        {
            var matches = placeholderPattern.Matches(templateFile);
            foreach (System.Text.RegularExpressions.Match match in matches)
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
        
        return placeholders.Values.OrderBy(p => p.Name).ToList();
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
}