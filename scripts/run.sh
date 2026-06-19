#!/usr/bin/env bash
# Run the Cortex Engine.
# Examples:
#   ./scripts/run.sh                           # run with defaults
#   ./scripts/run.sh -- --mcp-port 5000        # run with MCP HTTP server
#   ./scripts/run.sh -- --mcp-port 0           # run without MCP

ENGINE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"

unset SDL_VIDEODRIVER 2>/dev/null

cd "$ENGINE_DIR"
exec dotnet run --project "$ENGINE_DIR/src/CortexEngine.App/CortexEngine.App.csproj" -c Debug -- "$@"
