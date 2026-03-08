# KanBanLess

Less is more. Kanban in the File System.

An AI agent skill for managing a Kanban board entirely in the file system — no database, no server, no dependencies.

## How It Works

| Concept | File System Equivalent |
|---|---|
| Board | A directory |
| Column | A subdirectory (`backlog/`, `todo/`, `doing/`, `done/`) |
| Task | A `.md` file with YAML frontmatter and a markdown checklist |
| Move | Moving a file between column directories |

---

## Installation

### Windows

**Option A — .NET global tool (recommended)**

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet tool install -g KanBanLess.Cli
```

`kanban` is then available in any terminal (PowerShell, CMD, Windows Terminal).

**Option B — Git Bash / WSL**

If you have Git Bash or WSL installed, clone the repo and use the bash script directly:

```bash
git clone https://github.com/markdav-is/KanBanLess.git
cd KanBanLess
./kanban init
```

---

### macOS

**Option A — .NET global tool (recommended)**

Install the .NET SDK via the [official installer](https://dotnet.microsoft.com/download) or Homebrew:

```bash
brew install dotnet
dotnet tool install -g KanBanLess.Cli
```

Add the tools directory to your PATH if prompted:

```bash
export PATH="$PATH:$HOME/.dotnet/tools"   # add to ~/.zshrc or ~/.bash_profile
```

**Option B — Bash script (no .NET required)**

```bash
git clone https://github.com/markdav-is/KanBanLess.git
cd KanBanLess
./kanban init
```

---

### Linux

**Option A — .NET global tool (recommended)**

Install the .NET SDK for your distro:

```bash
# Ubuntu / Debian
sudo apt-get install -y dotnet-sdk-10.0

# Fedora / RHEL
sudo dnf install dotnet-sdk-10.0
```

Then install the tool:

```bash
dotnet tool install -g KanBanLess.Cli
export PATH="$PATH:$HOME/.dotnet/tools"   # add to ~/.bashrc
```

**Option B — Bash script (no .NET required)**

```bash
git clone https://github.com/markdav-is/KanBanLess.git
cd KanBanLess
./kanban init
```

---

### AI Agents (no install)

Agents can skip the CLI entirely. The skill files instruct them to operate directly on the file system using their built-in file and directory tools — no binary, no shell required.

| Agent | Skill file |
|---|---|
| Claude Code | `.claude/skills/kanbanless/SKILL.md` |
| GitHub Copilot | `.github/skills/kanbanless/SKILL.md` |

---

## Quick Start

```bash
# Create a board (makes a kanban/ folder; pass a name to customise)
kanban init
kanban init "My Project"   # creates my-project/ (or my-project-1/ if taken)
cd kanban

# Add tasks
kanban add "Design the data model"
kanban add "Write unit tests"
kanban add "Set up CI pipeline"

# Check the board
kanban status
kanban list

# Move tasks through the workflow
kanban move design-the-data-model todo
kanban move design-the-data-model doing
kanban show design-the-data-model

# Check off work
kanban check design-the-data-model "Step one"

# Complete
kanban move design-the-data-model done
```

---

## Commands

| Command | Description |
|---|---|
| `kanban init [name]` | Create a board folder (default: `kanban`) with 4 columns. Auto-increments if name exists. |
| `kanban add <title>` | Create a new task in `backlog/` |
| `kanban move <task> <column>` | Move a task to a new column |
| `kanban list [column]` | List tasks in one or all columns |
| `kanban show <task>` | Display a task's full content |
| `kanban check <task> <item>` | Mark a checklist item complete |
| `kanban status` | Show count per column |

---

## Task File Format

Tasks are plain Markdown files with minimal YAML frontmatter:

```markdown
---
priority: medium
tags: []
---

# Task Title

Brief description of the task.

## Checklist

- [ ] Step one
- [ ] Step two
- [ ] Step three
```

Metadata is derived from the filesystem — nothing duplicated in frontmatter:

- `id` — filename without `.md`
- `created` — file `ctime`
- `updated` — file `mtime`

---

## Directory Structure

```
my-board/
  backlog/
    task-slug.md
  todo/
    another-task.md
  doing/
    in-progress-task.md
  done/
    completed-task.md
```

---

## Principles

- **No database, no server** — the file system is the source of truth
- **Git-compatible** — all plain text, commit your board alongside your code
- **Agent-first** — designed to be driven by AI coding agents
- **Cross-platform** — C# .NET global tool works on Windows, Linux, and macOS without Git Bash

## Future

- **Blazor Hybrid GUI** — cross-platform desktop/mobile client (MAUI Blazor Hybrid) that reads/writes the same file system structure with drag-and-drop columns, works offline, no backend required
