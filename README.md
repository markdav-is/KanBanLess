# KanBanFS
Kanban in the File System

KanBanFS is an AI agent skill for managing a Kanban board entirely in the file system. No database, no server — plain text Markdown files in directories.

## Quick Start

```bash
# Build and install the CLI tool
dotnet build

# Initialize a board in the current directory
kanban init

# Add a task
kanban add "My first task" --priority high

# List all tasks
kanban list

# Move a task to "doing"
kanban move my-first-task doing

# Mark a checklist item done
kanban check my-first-task "Step one"

# Show board summary
kanban status
```

## Board Structure

```
my-board/
  backlog/          ← new tasks
  todo/             ← ready to work on
  doing/            ← in progress
  done/             ← completed
```

## Task File Format

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
| `kanban init` | Create the 4 column directories |
| `kanban add <title>` | Create a new task in `backlog/` |
| `kanban move <task> <column>` | Move a task to a column |
| `kanban list [column]` | List tasks in one or all columns |
| `kanban show <task>` | Display a task's content |
| `kanban check <task> <item>` | Mark a checklist item complete |
| `kanban status` | Show board summary (count per column) |

## Agent Skills

- GitHub Copilot: `.github/skills/kanbanfs/SKILL.md`
- Claude Code: `.claude/skills/kanbanfs/SKILL.md`

## Building & Testing

```bash
dotnet build KanBanFS.slnx
dotnet test tests/KanBanFS.Cli.Tests/KanBanFS.Cli.Tests.csproj
```
