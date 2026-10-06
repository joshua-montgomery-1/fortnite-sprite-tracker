# Sprite Scout local plugin

Development-only OAuth/MCP proof. Start the HTTPS server and configure Google before installing. Configure the server project with `ConnectionStrings:sprite-tracker`, `Authentication:Google:ClientId`, `Authentication:Google:ClientSecret`, and `CentralAuth:Enabled=true` using user secrets. Register Google callbacks `https://localhost:7082/signin-google` and `https://localhost:7082/identity/signin-google`. Central auth requires Development and an HTTPS loopback issuer. Account import runs through migrations when enabled.

Server: `https://localhost:7082/mcp`. Public client: `sprite-scout-codex`. No client secret. Codex chooses a loopback port for `http://127.0.0.1/callback`. The only tool is `who_am_i`; it returns your central account ID, website public profile ID, and display name.

The repository marketplace exposes this plugin without automatically enabling it. Install from **Sprite Scout Development** in the plugin directory. This package is for local clients on the server's computer; it is not a cloud ChatGPT connector.
