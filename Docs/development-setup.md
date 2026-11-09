# Development setup

How to get GodBoard running on a new machine (Windows or macOS), including the AI-assisted workflow.

## 1. Required software

| Tool | Why | Notes |
|---|---|---|
| **Unity Hub** + **Unity 6000.6.5f1** | The editor | In Unity Hub → Installs → add **Web Build Support** (and Mac Build Support on macOS, Windows Build Support on Windows if you want desktop builds) |
| **Git** (+ GitHub Desktop, optional) | Version control | macOS: `xcode-select --install` provides git |
| **.NET SDK 8+** (10 recommended) | CSharpier formatter and C# language server | `dotnet --version` |
| **Node.js 18+** | Runs the C# format hook (`.claude/hooks/format-csharp.js`) | `node --version` |
| **Python 3.10+** and **uv** | Required by MCP for Unity | macOS: `brew install uv`; Windows: `winget install astral-sh.uv` |
| **Claude Code** | AI workflow (agents, skills, hooks) | Sign in once |

## 2. Get the project

```bash
git clone https://github.com/DanDavD/UnityGameJam.git GameJam
cd GameJam
dotnet tool restore
dotnet tool install --global csharp-ls
```

Open the folder in **Unity Hub → Add → Add project from disk**, with editor **6000.6.5f1**. The first import takes a few minutes (`Library/` is rebuilt locally).

Then:
- **File → Build Profiles → Web → Switch Platform** (only needed before building).
- **Edit → Preferences → External Tools → Regenerate project files** (for the C# language server).
- Open `Assets/_Project/Scenes/Match.unity` and press Play.

## 3. Tests

- In the editor: **Window → General → Test Runner → EditMode → Run All** (about 208 tests; the `Balance` test is explicit and only runs on demand).
- Headless (editor closed):
  - Windows: `"C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml -logFile Logs/test.log`
  - macOS: `/Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml -logFile Logs/test.log`

## 4. WebGL build

File → Build Profiles → Web → Build, output to `Builds/WebGL/` (gitignored). Settings already in the project: product name **GodBoard**, Gzip compression with **Decompression Fallback**. Zip the contents of `Builds/WebGL/` (with `index.html` at the root) for itch.io.

To test locally, serve the folder (opening `index.html` directly does not work):

```bash
python3 -m http.server 8090 --directory Builds/WebGL
```

Then open http://127.0.0.1:8090.

## 5. AI workflow (Claude Code)

Everything shared lives in the repository: `CLAUDE.md`, `.claude/agents/`, `.claude/skills/`, `.claude/settings.json` (permissions, CSharpier hook, enabled plugins) and `.mcp.json` (Unity MCP).

1. **Plugins** (official marketplace, project scope). In the project folder:
   ```bash
   claude plugin install unity@claude-plugins-official --scope project
   claude plugin install context7@claude-plugins-official --scope project
   claude plugin install csharp-lsp@claude-plugins-official --scope project
   ```
   Then authorize Context7 once: run `/mcp` in Claude Code → context7 → authenticate.
2. **MCP for Unity**: the package is already in `Packages/manifest.json`. With the project open: **Window → MCP for Unity** → confirm Python and uv are found → transport **HTTP Local**, `http://127.0.0.1:8080` → **Start Server**. Do **not** use "Configure" for Claude Code: `.mcp.json` already points to it. Start the server every time you open Unity; if Claude Code started before the server, reconnect with `/mcp` → unityMCP → Reconnect.
3. **Local-only files** (never committed): `CLAUDE.local.md` and `Docs/devlog-decisiones.md`. Copy them from your private backup into the project folder, then add them to `.git/info/exclude` so git ignores them without touching `.gitignore`:
   ```
   Docs/devlog-decisiones.md
   CLAUDE.local.md
   ```

## 6. Troubleshooting

- **Unity can't find uv** after installing it: fully quit Unity Hub (it keeps running in the menu bar / tray) and reopen it.
- **Clicks don't register in the WebGL page**: the first click may only focus the canvas; click again.
- **Editor freezes while an agent works**: force-quit Unity, reopen the project and restart the MCP server. Saved scenes and assets are not lost.
