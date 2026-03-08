# KanBanLess Skill

Manage a Kanban board entirely in the file system using the `kanban` CLI script.

## Overview

- **Board** = a directory (current working directory)
- **Columns** = subdirectories: `backlog/`, `todo/`, `doing/`, `done/`
- **Tasks** = `.md` files with YAML frontmatter and a markdown checklist body
- **Movement** = moving files between column directories

The `kanban` script lives at the repository root. Run it as `./kanban <command>`.

## Commands

| Command | Description |
|---|---|
| `./kanban init [name]` | Create a board folder (default: `kanban`) with 4 column directories. Auto-increments if the name exists (`kanban-1`, `kanban-2`, …). |
| `./kanban add <title>` | Create a new task `.md` in `backlog/` |
| `./kanban move <task> <column>` | Move a task file to a new column directory |
| `./kanban list [column]` | List tasks in one or all columns |
| `./kanban show <task>` | Display a task's content |
| `./kanban check <task> <item>` | Mark a checklist item complete |
| `./kanban status` | Show board summary (count per column) |

## Task File Format

Each task is a `.md` file named with a slug derived from the title:

```
---
priority: low | medium | high
tags: []
---

# Full Task Title

Brief description of the task.

## Checklist

- [ ] Step one
- [ ] Step two
- [ ] Step three
```

- `id` = filename without `.md`
- `created` = file `ctime` (from the filesystem)
- `updated` = file `mtime` (from the filesystem)

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

## Usage Examples

```bash
# Initialise a new board in the current directory
./kanban init

# Add tasks
./kanban add "Set up CI pipeline"
./kanban add "Write unit tests"

# View the board
./kanban status
./kanban list
./kanban list todo

# Work a task
./kanban move set-up-ci-pipeline todo
./kanban move set-up-ci-pipeline doing
./kanban show set-up-ci-pipeline

# Complete checklist items
./kanban check set-up-ci-pipeline "Step one"

# Finish
./kanban move set-up-ci-pipeline done
```

## Claude Code Agent Guidelines

When invoked to manage a Kanban board:

1. **Initialise first**: run `./kanban init` if the column directories do not exist.
2. **Assess state**: run `./kanban status` before making any changes.
3. **Read before acting**: use `./kanban list` and `./kanban show <task>` to understand existing tasks.
4. **Derive meaningful titles** from the user's description when creating tasks with `./kanban add`.
5. **Use the slug** (filename without `.md`) — not the full title — in `move`, `show`, and `check` commands.
6. **Progress naturally**: move tasks from `backlog` → `todo` → `doing` → `done` as work advances.
7. **Mark completion**: use `./kanban check <task> <item>` as individual checklist steps are finished.
8. **Commit changes**: all board state is plain text — commit to git to preserve history.

## Constraints

- No database, no server — the file system is the source of truth.
- All files are plain text and git-compatible.
- Columns are fixed: `backlog`, `todo`, `doing`, `done`.
- The task `id` is the filename slug derived at creation time and never renamed.
