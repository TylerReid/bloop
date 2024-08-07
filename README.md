# Bloop
Bloops things! A postman alternative that uses [TOML](https://toml.io) as a configuration file. It automatically detects used variables and performs actions needed to satisfy them, such as reading from a file, making another request and pulling out values, or running an arbitrary script.

Bloops runs on MacOs, Linux, and Windows.

## Usage
Bloop can be used either as [a cli](README.cli.md) or as [a terminal gui](README.gui.md).

## Requests
Named http requests to make
```toml
[request.florp]
uri = "http://localhost:5284/echo"
method = "POST"
body = "{}"
content_type = "application/json"
headers = { Authorization = "Bearer ${mySuperSecretToken}" }

[request.somejson]
uri = "https://stackoverflow.com/api/recent-chat"
```
### Properties
#### uri
  * can contain variables
  * default is `http://localhost`
#### method
  * default is `GET`
#### body
  * can contain variables
  * optional request body
#### content_type
  * optional content type
  * only used if `body` is set
#### form
  * values can contain variables
  * optional key value pairs to be used in `application/x-www-form-urlencoded` content
  * if set `body` will not be used
#### headers
  * values can contain variables
  * optional key value pairs to be sent as headers
#### query
  * values can contain variables
  * optional key value pairs to be added to the uri as query parameters

## Variables
Variables can be used in request header values, bodies, and the uri by using `${someVariableName}` inside of the definition. Values come from various sources defined in the properties or passed to the cli with `--var someKey=value,otherKey=derp`
```toml
[variable.activeUsers]
source = "somejson"
jpath = "$.activeUsers"

[variable.command]
command = "./scripts/testVariableScript.ps1"
```

### Properties
#### value
  * constant literal value for a variable
#### jpath
  * a [jpath](https://tools.ietf.org/id/draft-goessner-dispatch-jsonpath-00.html#section-1.3) used to extract values from the reponse of `source`
  * `source` is required if this is set
#### source
  * the request name that will be used to extract values
  * `jpath` is required if this is set
#### command
  * executable or script to run
  * the entire stdout is used as the variable value if the command exits successfully
#### command_args
  * optional arguments to pass to a `command`
#### file
  * read the contents of a file
#### env
  * environment variables
#### value_lifetime
  * optional timespan after which a new value will be retrieved. In the format `HH:MM:SS` Useful for token expiration.

## Variable Sets
Sometimes you want to have swappable sets of variables, for example when interacting with multiple deployment environments. To support this bloop variables support environment sets. Each variable in a variable set has all the properties of a normal top level variable. The `-s` or `--set` cli option can be used to select an env set.
```toml
[request.someApi]
uri = "https://${host}/floob"
headers = { Authorization = "Bearer ${token}" }

[variable.host]
default = "dev.example.com"

[variableset.host.staging]
env = "STAGE_HOST"

[variableset.host.prod]
file = ".prod_host"

[variable.token]
file = ".token"

[variableset.token.staging]
command = "./getToken.ps1"
command_args = "stage"
value_lifetime = "00:10:00"

[variableset.token.prod]
command = "./getToken.ps1"
command_args = "prod"
value_lifetime = "00:03:00"
```
```bash
bloop someApi --set prod
```

## Default values
```toml
[defaults]
headers = { X-Bloop = "${yep}" }
```
Header values that will be added to every request if not already specified

## Dev and Build
Bloop requires [the latest .net](https://dotnet.microsoft.com/en-us/download) for the main application, and [powershell](https://github.com/PowerShell/PowerShell) for scripts and tests

To build: `dotnet build`

To produce release mode outputs in the `./releases/` directory: `./scripts/buildCli.ps1`

To run integration tests: `./tests/IntegrationTests/runTests.ps1`
