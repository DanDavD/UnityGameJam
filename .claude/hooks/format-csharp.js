// PostToolUse hook: formats C# files with CSharpier right after Claude edits them.
// Reads the hook payload from stdin; does nothing for non-.cs files.
// Never blocks the edit: formatting failures are reported but exit code stays 0.

const { execFileSync } = require("child_process");
const path = require("path");

let raw = "";
process.stdin.setEncoding("utf8");
process.stdin.on("data", (chunk) => (raw += chunk));
process.stdin.on("end", () => {
  let filePath;
  try {
    filePath = JSON.parse(raw)?.tool_input?.file_path;
  } catch {
    return;
  }
  if (!filePath || path.extname(filePath).toLowerCase() !== ".cs") return;

  const projectDir = process.env.CLAUDE_PROJECT_DIR || process.cwd();
  try {
    execFileSync("dotnet", ["csharpier", "format", filePath], {
      cwd: projectDir,
      stdio: ["ignore", "ignore", "pipe"],
      timeout: 55000,
    });
  } catch (err) {
    const detail = err.stderr ? err.stderr.toString().trim() : err.message;
    process.stderr.write(`CSharpier could not format ${filePath}: ${detail}\n`);
  }
});
