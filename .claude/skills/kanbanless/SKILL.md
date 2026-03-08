# KanBanLess Skill

Manage a Kanban board entirely in the file system.

- **Board** = a directory
- **Columns** = subdirectories: `backlog/`, `todo/`, `doing/`, `done/`
- **Tasks** = `.md` files with YAML frontmatter and a markdown checklist body
- **Movement** = moving files between column directories

---

## How to Execute Commands

KanBanLess ships a cross-platform C# .NET global tool and a bash fallback.
**Choose the first method that applies to the current environment:**

### 1. `kanban` CLI (preferred — works on Windows, Linux, macOS)

Install once via:
```
dotnet tool install -g KanBanLess.Cli
```
Then call:
```
kanban init [name]
kanban add "My task"
```

### 2. Bash script (Linux / macOS only)

```
./kanban init [name]
./kanban add "My task"
```

### 3. Direct file operations (agent fallback — no install required)

If neither CLI is available, perform all operations using your built-in file and directory tools:

| KanBanLess operation | Agent action |
|---|---|
| `init [name]` | Create `<name>/backlog/`, `<name>/todo/`, `<name>/doing/`, `<name>/done/` |
| `add <title>` | Write a `.md` file to `backlog/<slug>.md` using the task format below |
| `move <task> <col>` | Move (rename) the `.md` file from its current column dir to the target |
| `list [col]` | Read directory listings for one or all column dirs |
| `show <task>` | Read the `.md` file content |
| `check <task> <item>` | Read the file, replace `- [ ] <item>` with `- [x] <item>`, write back |
| `status` | Count `.md` files in each column directory |

---

## Commands

| Command | Description |
|---|---|
| `kanban init [name]` | Create a board folder (default: `kanban`) with 4 column directories. Auto-increments if the name exists (`kanban-1`, `kanban-2`, …). |
| `kanban add <title>` | Create a new task `.md` in `backlog/` |
| `kanban move <task> <column>` | Move a task file to a new column directory |
| `kanban list [column]` | List tasks in one or all columns |
| `kanban show <task>` | Display a task's content |
| `kanban check <task> <item>` | Mark a checklist item complete |
| `kanban status` | Show board summary (count per column) |

---

## Task File Format

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

- `id` = filename without `.md` (slug derived from title at creation)
- `created` = file `ctime` (filesystem)
- `updated` = file `mtime` (filesystem)

Slug rules: lowercase, non-alphanumeric runs replaced with `-`, no leading/trailing `-`.

---

## Directory Structure

```
<board-name>/
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

## Agent Behaviour Guidelines

1. **Detect execution method** — check for `kanban` in PATH; fall back to `./kanban`; fall back to direct file ops.
2. **Initialise if needed** — run `kanban init` if the 4 column directories don't exist yet.
3. **Assess before acting** — run `kanban status` to understand current board state.
4. **Read before modifying** — use `kanban list` / `kanban show` to inspect existing tasks.
5. **Derive meaningful titles** from the user's description when adding tasks.
6. **Use the slug** (filename without `.md`) — not the full title — in `move`, `show`, and `check`.
7. **Progress naturally**: `backlog` → `todo` → `doing` → `done`.
8. **Mark completion**: use `kanban check` as individual checklist steps are finished.
9. **Commit changes**: all board state is plain text — commit to git to preserve history.

---

## Constraints

- No database, no server — the file system is the source of truth.
- All files are plain text and git-compatible.
- Columns are fixed: `backlog`, `todo`, `doing`, `done`.
- Task slugs are set at creation and never renamed.
