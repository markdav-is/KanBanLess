# KanBanFS Skill

KanBanFS is a Kanban board managed entirely in the file system. Each board is a directory, columns are subdirectories, and tasks are Markdown files with YAML frontmatter.

## Directory Structure

```
<board>/
  backlog/      ← new tasks land here
  todo/         ← ready to be worked on
  doing/        ← currently in progress
  done/         ← completed tasks
```

## Task File Format

Every task is a `.md` file named with a slug derived from the title:

```markdown
---
id: task-slug
title: Full Task Title
created: YYYY-MM-DD
updated: YYYY-MM-DD
priority: low | medium | high
tags: []
---

## Description

Brief description of the task.

## Checklist

- [ ] Step one
- [ ] Step two
- [ ] Step three
```

## Commands

| Command | Description |
|---------|-------------|
| `kanban init` | Create the 4 column directories in the current folder |
| `kanban add <title>` | Create a new task `.md` in `backlog/` |
| `kanban move <task> <column>` | Move a task file to a new column directory |
| `kanban list [column]` | List tasks in one or all columns |
| `kanban show <task>` | Display a task's content |
| `kanban check <task> <item>` | Mark a checklist item complete |
| `kanban status` | Show board summary (count per column) |

## Global Options

- `--board <path>` — Path to the board directory (defaults to current working directory)

## Usage Examples

```bash
# Initialize a new board
kanban init

# Add tasks
kanban add "Build the login page" --priority high
kanban add "Write unit tests" --tags testing,ci

# List all tasks
kanban list

# List tasks in a specific column
kanban list backlog

# Move a task between columns
kanban move build-the-login-page doing

# Show task details
kanban show build-the-login-page

# Mark a checklist item complete
kanban check build-the-login-page "Step one"

# Show board summary
kanban status
```

## Principles

- No database, no server — the file system is the source of truth
- All files are plain text and git-friendly
- Task slugs are derived from titles (lowercase, hyphenated)
- Partial slug matching is supported for `move`, `show`, `check`, and `list`
