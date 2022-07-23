# Bloop
Bloops things! A postman alternative that uses [TOML](https://toml.io) as a configuration file, and automatically detects used variables and performs requests needed to satisfy them.

## Usage

```console
$ bloop --help
  request    (Default Verb)

  list       list requests and variables

  help       Display more information on a specific command.

  version    Display version information.

$ bloop request --help
  --prettyprint        (Default: true)

  -v, --verbose        (Default: false)

  -c, --config-path    (Default: bloop.toml)

  -i, --insecure       (Default: false) disables certificate validation

  --help               Display this help screen.

  --version            Display version information.

  request (pos. 0)     name of the request to send
```

```console
$ bloop list
requests:
somejson:       { Uri: https://stackoverflow.com/api/recent-chat, Method: GET }
echoquery:      { Uri: http://localhost:5284/echo/query?something=${activeUsers}, Method: GET }

variables:
activeUsers:    { Source: somejson, Jpath: $.activeUsers }

$ bloop echoquery -v
Request Uri: http://localhost:5284/echo/query?something=42
Status: OK

42
```

## Requests
Named http requests to make
```toml
[request.florp]
uri = "http://localhost:5284/echo"
method = "Get"
body = "{}"
content_type = "application/json"
headers = { Authorization = "Bearer ${command}" }
```

## Variables
Variables can be used in request header values, bodies, and the uri by using `${someVariableName}` inside of the definition.
```toml
[variable.activeUsers]
source = "somejson"
jpath = "$.activeUsers"

[variable.command]
command = "./scripts/testVariableScript.ps1"

```

### Variable Sources
* Setting a constant value on its `value` property
* Some other request, extracted from its response via a [jpath](https://tools.ietf.org/id/draft-goessner-dispatch-jsonpath-00.html#section-1.3)
* An external program or script by using `command` and optionally `command_args`
* the contents of a file by using `file`
* an environment variable by using `env`
