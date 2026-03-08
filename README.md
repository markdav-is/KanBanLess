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

## Installation

### .NET Global Tool (Windows, Linux, macOS)

```
dotnet tool install -g KanBanLess.Cli
kanban init
```

### Bash script (Linux / macOS)

The `kanban` bash script at the repo root requires no install — just run `./kanban <command>` from the project directory.

### No install (agents)

Agents can skip the CLI entirely and operate directly on the file system using their built-in file tools. The skill files describe exactly how.

## Quick Start

```bash
# Initialise a board (creates a kanban/ folder; use a name to customise)
kanban init
kanban init "My Project"   # creates my-project/ (or my-project-1/ if taken)
cd kanban

# Add tasks
./kanban add "Design the data model"
./kanban add "Write unit tests"
./kanban add "Set up CI pipeline"

# Check the board
./kanban status
./kanban list

# Move tasks through the workflow
./kanban move design-the-data-model todo
./kanban move design-the-data-model doing
./kanban show design-the-data-model

# Check off work
./kanban check design-the-data-model "Step one"

# Complete
./kanban move design-the-data-model done
```

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

Metadata derived from the filesystem (no duplication in frontmatter):

- `id` — filename without `.md`
- `created` — file `ctime`
- `updated` — file `mtime`

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

## Agent Skills

KanBanLess ships skill definition files for AI coding agents:

| Agent | Skill file |
|---|---|
| Claude Code | `.claude/skills/kanbanless/SKILL.md` |
| GitHub Copilot | `.github/skills/kanbanless/SKILL.md` |

## Principles

- **No database, no server** — the file system is the source of truth
- **Git-compatible** — all plain text, commit your board alongside your code
- **Agent-first** — designed to be driven by AI coding agents
- **Cross-platform** — C# .NET global tool works on Windows, Linux, and macOS without Git Bash

## Future

- **Blazor Hybrid GUI** — cross-platform desktop/mobile client (MAUI Blazor Hybrid) that reads/writes the same file system structure with drag-and-drop columns, works offline, no backend required
