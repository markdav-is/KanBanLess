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

## Agent Behaviour Guidelines

When a user asks to manage their Kanban board:

1. **Always run `./kanban init`** if the column directories do not exist yet.
2. Use `./kanban status` to understand the current board state before making changes.
3. Use `./kanban list` to enumerate tasks; use `./kanban show <task>` to read task details.
4. When creating tasks, derive a meaningful title from the user's request.
5. When moving tasks, confirm the destination column is appropriate for the workflow stage.
6. Check off checklist items with `./kanban check` as work is completed.
7. All operations are local file-system changes — they are git-friendly and can be committed.

## Constraints

- No database, no server — the file system is the source of truth.
- All files are plain text and git-compatible.
- The task slug is the filename without `.md`; use it (not the full title) in `move`, `show`, and `check` commands.
