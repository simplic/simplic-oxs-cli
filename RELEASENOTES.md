# 1.0.3

- Add signed version of oxs-cli.
- Fix minor bugs in configuration handling. 

# 1.0.4

- Add flow command `oxs flow push-node-def -p <path-to-flow-dll> -s <section>`

# 1.0.5

- Fix bug in `oxs flow push-node-def` command where section parameter was not being handled correctly. (type and taget)

# 1.0.6

- `oxs flow push-node-def` no supports pushing markdown documentation that is compiled into the flow dll as embedded resources. The name of the file must be `<class-name>.md`

# 1.0.7

- Add project commands:
- `oxs project init -n <project-name>`: Initializes a new oxs project with the specified name.
- `oxs project clean`: Cleans the build artifacts of the current oxs project.
- `oxs project build`: Builds the current oxs project.
- `oxs project deploy`: Deploys the current oxs project.
- `oxs project run`: Runs the current oxs project. 
- `oxs package install`: Deploys the current oxs project.

# 1.0.8

- Extend `oxs package install` command to support installing packages from directory artifacts.

# 1.0.10

- Improve readme and documentation for oxs-cli tool.

# 1.0.11

- Add `oxs manifest init` to create a new manifest file

# 1.0.12

- Add `oxs manifest list-templates` 

# 1.0.13

- Add `oxs service get-definition -e <endpoint> -v <version>` command to download service definitions from OXS API
  - `-e/--endpoint` - Service endpoint (e.g., document, storage-management, provider-rossum)
  - `-v/--version` - Service version (e.g., v1, v2)
  - `-s/--section` - Configuration section (default: default)
  - `-o/--output` - Output directory (default: current directory)
  - Downloads service definition JSON
  - Downloads and saves Swagger/OpenAPI specification (swagger.json)
  - Downloads and saves model definition (model-definition.json)
  - Extracts and saves gRPC proto files
  - Saves GraphQL schemas
  - Creates organized folder structure by service name and version

# 1.0.14

- Update dependencies to latest versions.
- Add debug output for push-node-def command.

# 1.0.15

- Add `oxs report` commands:
  - `oxs report list` - Lists available reports.
  - `oxs report download -n <report-name> -f <report-file>` - Downloads a specific report
  - `oxs report upload -n <report-name> -f <report-file>` - Uploads a report