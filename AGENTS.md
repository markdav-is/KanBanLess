# AGENTS.md — KanBanLess

Source of truth for all repo instructions. AI agents and human contributors should read this file before making changes.

---

## Project Purpose

KanBanLess is a Kanban board that lives entirely in the file system — no database, no server, no runtime dependencies. A board is a directory; columns are subdirectories; tasks are Markdown files.

---

## Stack

| Layer | Choice | Notes |
|---|---|---|
| Primary CLI | **C# .NET 10 global tool** (`KanBanLess.Cli`) | Cross-platform (Windows, Linux, macOS). Target framework: `net10.0`. Published as `KanBanLess.Cli` on NuGet. Command name: `kanban`. |
| Fallback CLI | **Bash script** (`./kanban`) | Pure Bash 4+, zero external dependencies (no Python, no Node, no Ruby). Linux/macOS only. |
| Data store | **File system** | Markdown files with YAML frontmatter. No database, no server. Git-compatible by design. |
| Task format | **Markdown + YAML frontmatter** | Frontmatter keys: `priority` (`low`\|`medium`\|`high`), `tags` (array). Filesystem metadata (`ctime`/`mtime`) replaces explicit `created`/`updated` fields. |
| Column order | **`order.txt`** | Optional plain-text file in each column directory (`backlog/order.txt`, etc.). One slug per line. Tasks listed here appear first in `kanban list`; unlisted tasks follow sorted by priority. Maintained automatically by `add`, `move`, and `order` commands. |
| Future GUI | **MAUI Blazor Hybrid** | Planned cross-platform desktop/mobile client. Reads/writes the same file system structure. Offline, no backend required. |

---

## Repository Layout

```
KanBanLess/
  kanban                        # Bash fallback script (pure Bash 4+)
  src/
    KanBanLess.Cli/
      KanBanLess.Cli.csproj     # .NET 10 global tool
      Program.cs                # Entry point and all commands
  .github/
    skills/kanbanless/SKILL.md  # GitHub Copilot agent skill
  .claude/
    skills/kanbanless/SKILL.md  # Claude Code agent skill
  AGENTS.md                     # ← you are here
  README.md
```

---

## Development Conventions

### C# CLI (`src/KanBanLess.Cli/`)

- Target `net10.0`; enable nullable reference types and implicit usings.
- All commands live in `Program.cs` (single-file style; split only if the file becomes unmanageable).
- No third-party NuGet packages unless absolutely necessary — keep the tool lean.
- Slug rules: lowercase, runs of non-alphanumeric characters replaced with `-`, no leading/trailing `-`.

### Bash script (`kanban`)

- Pure Bash 4+ — no Python, no awk one-liners that require GNU extensions, no `sed -i` (use Bash builtins or POSIX-compatible substitutions).
- Use `set -euo pipefail` at the top.
- Use `(( ++n ))` (pre-increment) instead of `(( n++ ))` when `n` may start at 0, to avoid tripping `set -e`.
- Keep the Bash script and the C# tool behaviourally identical — same commands, same output format.

### Both implementations

- Columns are fixed: `backlog → todo → doing → done`. Do not add new columns without updating both implementations and the skill files.
- Never write metadata (id, created, updated) into frontmatter — derive it from the filename and filesystem.
- Output is plain text, human-readable, and stable enough for scripts to parse.
- `order.txt` in each column directory controls display order. One slug per line. `add` appends to `backlog/order.txt`; `move` removes from source and appends to destination; `order` rewrites the file. Missing entries in `order.txt` (new tasks added externally) sort after listed entries, by priority then slug. Stale entries (slug with no `.md` file) are silently ignored.

---

## Agent Skill Files

Two skill files teach AI agents how to use KanBanLess:

| Agent | File |
|---|---|
| Claude Code | `.claude/skills/kanbanless/SKILL.md` |
| GitHub Copilot | `.github/skills/kanbanless/SKILL.md` |

Keep these in sync when commands or file formats change. If you intentionally update only one skill file (for example, while migrating one provider to a new YAML format), document the divergence in this section and clearly state which file is currently canonical.

---

## Making Changes

1. Run both implementations against the same scenario to confirm identical output.
2. Update both skill files if any command interface or file format changes.
3. Keep `README.md` in sync for user-facing documentation.
4. All board state is plain text — commit freely.
