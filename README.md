# Bloop
Bloops things! A postman alternative that uses TOML as a configuration file, and automatically detects used variables and performs requests needed to satisfy them.

## Usage
```
$ bloop list
somejson:       { Uri: https://stackoverflow.com/api/recent-chat, Method: GET }
    { Variable: activeUsers, JPath: $.activeUsers }
echoquery:      { Uri: http://localhost:5284/echo/query?something=${activeUsers}, Method: GET }

variables:
activeUsers:    { Source: somejson }

$ bloop echoquery -v
Request Uri: http://localhost:5284/echo/query?something=42
Status: OK

42
```
