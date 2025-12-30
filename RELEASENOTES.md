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