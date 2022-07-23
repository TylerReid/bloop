# Bloop
Bloops things! A postman alternative that uses TOML as a configuration file, and automatically detects used variables and performs requests needed to satisfy them.

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
