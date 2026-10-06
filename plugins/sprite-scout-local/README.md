# Sprite Scout local plugin

Development-only OAuth/MCP proof. Start the HTTPS server and configure Google before installing. See [local setup](../../docs/central-auth-phase-1.md).

Server: `https://localhost:7082/mcp`. Public client: `sprite-scout-codex`. No client secret. Codex chooses a loopback port for `http://127.0.0.1/callback`. The only tool is `who_am_i`; it returns your central account ID, website public profile ID, and display name.

The repository marketplace exposes this plugin without automatically enabling it. Install from **Sprite Scout Development** in the plugin directory. This package is for local clients on the server's computer; it is not a cloud ChatGPT connector.
