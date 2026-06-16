#!/usr/bin/env bash
# Start the Cortex Engine with the HTTP MCP server enabled.

PORT="${1:-5000}"
ENGINE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

export DISPLAY="${DISPLAY:-:0}"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"

cd "$ENGINE_DIR"
exec dotnet run --project "$ENGINE_DIR/src/CortexEngine.App/CortexEngine.App.csproj" -- --mcp-port "$PORT"
