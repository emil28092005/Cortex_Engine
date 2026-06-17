#!/usr/bin/env bash
# Run the Cortex Engine.
# Examples:
#   ./scripts/run.sh                           # run with defaults
#   ./scripts/run.sh --mcp-port 5000           # run with MCP HTTP server
#   ./scripts/run.sh --camera-tour             # capture screenshots and exit
#   ./scripts/run.sh --mcp-stdio               # run headless stdio MCP server

ENGINE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"

# Prefer Wayland if available, fall back to X11
if [ -n "$WAYLAND_DISPLAY" ]; then
    export SDL_VIDEODRIVER="${SDL_VIDEODRIVER:-wayland}"
else
    export DISPLAY="${DISPLAY:-:0}"
    export SDL_VIDEODRIVER="${SDL_VIDEODRIVER:-x11}"
fi

cd "$ENGINE_DIR"
exec dotnet run --project "$ENGINE_DIR/src/CortexEngine.App/CortexEngine.App.csproj" -c Debug -- "$@"
