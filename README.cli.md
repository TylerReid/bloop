# Bloop CLI

## Examples
### Sending a requst 
```bash
bloop someRequestName -c ./bloops --var "token=$TOKEN" --var "userAgent=Bloop"
```
### Listing bloop configuration
```bash
bloop list --type requests
```
### Getting a value for a variable
```bash
bloop variable accesstoken -s prod
```
### Validating configs
```bash
bloop validate
```
## Global Options
  * `-c` `--config-path` specify the directory to load configuation from. without this default bloop will look in the current directory.
  * `--no-color` disables colorized output
  * `-i` `--insecure` disables tls certificate checks

## Request options
  * `request name` the name of the request to execute. Must be the first argument passed and has no flag name.
  * `--pretty-print` outputs indentation formatted json if the api responds with json.
  * `-v` `--verbose` prints additional info
  * `--var` supplies values for variables in `name=value` format
  * `-s` `--set` selects a variableset to be used

## List options
  * `--type` limits the values listed. Can be one of `all`, `variables`, `requests`, or `defaults`
## Variable options
  * `variable name` the name of the variable to get a value for. Must be the first argument passed and has no flag name.
  * `-s` `--set` selects a variableset to be used