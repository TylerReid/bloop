#!/usr/bin/env bash
set -euo pipefail

sudo cp "./releases/$1/bloop" /usr/local/bin/bloop
