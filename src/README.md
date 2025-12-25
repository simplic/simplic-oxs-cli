# OXS CLI

A command-line interface tool for interacting with the OXS (Simplic) API, providing easy configuration management and HTTP request capabilities.

## Installation

Build the project and ensure the executable is in your PATH or use it directly from the build output directory.

## Command Structure

The OXS CLI follows a hierarchical command structure:

```
oxs <command> <subcommand> [options]
```

Where:
- `<command>` is the main command group (e.g., `configure`, `http`)
- `<subcommand>` is the specific action within that group (e.g., `env`, `get`)
- `[options]` are command-specific parameters and flags

## Getting Started

### 1. Configure Environment

Before using the CLI, you need to set up your environment configuration. This creates a profile/section that stores your API credentials and settings.

#### Interactive Setup

```bash
oxs configure env
```

This will prompt you to:
1. Select an API environment (staging or prod)
2. Enter your account email address
3. Enter your account password
4. Select an organization from your available organizations

#### Command-Line Setup

You can also provide all parameters directly:

```bash
oxs configure env --api prod --email your.email@example.com --password yourpassword --organization "Your Organization" --section default
```

#### Parameters

| Parameter | Short | Description | Default |
|-----------|-------|-------------|---------|
| `--api` | `-a` | API environment to use (`prod` or `staging`) | Interactive prompt |
| `--email` | `-e` | Your account email address | Interactive prompt |
| `--password` | `-p` | Your account password | Interactive prompt |
| `--organization` | `-o` | Organization to use | Interactive selection |
| `--section` | `-s` | Configuration section name | `default` |

### 2. Create Multiple Profiles/Sections

You can create multiple configuration sections for different environments or organizations:

```bash
# Create a staging profile
oxs configure env --api staging --section staging

# Create a production profile
oxs configure env --api prod --section production

# Create a profile for a specific organization
oxs configure env --section myorg --organization "My Organization"
```

### 3. Making HTTP Requests

Once configured, you can make HTTP requests to the OXS API using your stored credentials.

#### Basic GET Request

```bash
oxs http get --endpoint /organization-api/v1/organization --section default -f -o json
```

#### Command Structure for HTTP

```
oxs http <method> [options]
```

Where `<method>` can be:
- `get` - HTTP GET request
- `post` - HTTP POST request
- `put` - HTTP PUT request
- `patch` - HTTP PATCH request
- `delete` - HTTP DELETE request

#### HTTP Request Parameters

| Parameter | Short | Description | Default | Required |
|-----------|-------|-------------|---------|----------|
| `--endpoint` | `-e` | API endpoint to call | - | Yes |
| `--section` | `-s` | Configuration section to use | `default` | No |
| `--body` | `-b` | Request body content or file (`$filename` for file) | - | No |
| `--headers` | `-H` | Additional headers (`Key:Value;Key2:Value2`) | - | No |
| `--format-only` | `-f` | Return only response content, no headers/status | `false` | No |
| `--output-format` | `-o` | Output format (`json`, `xml`, `text`) | `json` | No |

#### Examples

**Simple GET request:**
```bash
oxs http get --endpoint /organization-api/v1/organization
```

**GET request with specific section:**
```bash
oxs http get --endpoint /organization-api/v1/organization --section production
```

**GET request with clean JSON output:**
```bash
oxs http get --endpoint /organization-api/v1/organization --section default -f -o json
```

**POST request with inline body:**
```bash
oxs http post --endpoint /api/v1/users --body '{"name":"John Doe","email":"john@example.com"}'
```

**POST request with file body:**
```bash
oxs http post --endpoint /api/v1/users --body $user.json
```

**Request with custom headers:**
```bash
oxs http get --endpoint /api/v1/data --headers "X-Custom-Header:value;Accept:application/xml"
```

**Request with different output formats:**
```bash
# JSON output (default)
oxs http get --endpoint /api/v1/data -o json

# XML output
oxs http get --endpoint /api/v1/data -o xml

# Plain text output
oxs http get --endpoint /api/v1/data -o text
```

## Configuration Management

### Configuration Sections

The CLI supports multiple configuration sections, allowing you to manage different environments, organizations, or accounts:

- Each section stores API endpoint, credentials, organization details, and authentication tokens
- The default section is named `default`
- You can specify which section to use with the `--section` parameter

### Configuration Storage

Configuration files are stored in the `.oxs` folder in your user directory, with separate files for:
- General configuration (API endpoints, organization info)
- Secure credentials (authentication tokens)

### Switching Between Configurations

```bash
# Use default configuration
oxs http get --endpoint /api/endpoint

# Use staging configuration
oxs http get --endpoint /api/endpoint --section staging

# Use production configuration
oxs http get --endpoint /api/endpoint --section production
```

## Common Workflows

### Initial Setup Workflow

1. **Configure your environment:**
   ```bash
   oxs configure env --section default
   ```

2. **Test the connection:**
   ```bash
   oxs http get --endpoint /organization-api/v1/organization -f
   ```

3. **Create additional profiles if needed:**
   ```bash
   oxs configure env --section staging --api staging
   ```

### Daily Usage

1. **List organizations:**
   ```bash
   oxs http get --endpoint /organization-api/v1/organization -f -o json
   ```

2. **Make API calls with clean output:**
   ```bash
   oxs http get --endpoint /your-endpoint --section default -f -o json
   ```

3. **Switch between environments:**
   ```bash
   # Use staging
   oxs http get --endpoint /api/endpoint --section staging
   
   # Use production
   oxs http get --endpoint /api/endpoint --section production
   ```

## Troubleshooting

### Common Issues

1. **Invalid credentials:** Re-run the configure command to update your credentials
2. **Section not found:** Make sure you've created the configuration section first
3. **API endpoint errors:** Verify the endpoint URL and your organization permissions

### Re-configuring

To update an existing configuration section, simply run the configure command again:

```bash
oxs configure env --section mysection
```

This will overwrite the existing configuration for that section.

## API Environments

- **Production:** `https://oxs.simplic.io/`
- **Staging:** `https://dev-oxs.simplic.io/`

Choose the appropriate environment based on your needs. Production should be used for live data, while staging is suitable for testing and development.