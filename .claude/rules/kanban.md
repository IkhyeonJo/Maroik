---
paths:
  - "**/*Kanban*"
  - "**/Kanbans/**"
  - "**/Kanban/**"
---

# Intended design: Kanban

Part of CLAUDE.md's "Intended design" (do not "fix" these): the items below look like gaps or smells but are
deliberate. Do not change them, add guards around them, or refactor them away unless the owner explicitly asks for
that specific change. Check every candidate review finding in this area against them before reporting it.

- One board per user (`/Kanban/UserIndex`, User role only): the four fixed lanes of `KanbanLanes` (Backlog, To do, In progress,
  Done), which cannot be added, renamed, deleted or reordered, and no board table. A lane's cards are in creation order (ties by id);
  no position is stored and the order is not user-changeable.
- A card never changes lane: no drag-and-drop, and the edit request has no lane (`KanbanCardUpdateRequest`). Moving work means
  deleting the card and creating a new one in the other lane. Do not add a "move" action or a sortable library.
- Whether a due date is overdue is decided by the domain in the owner's time zone (`KanbanCard.DueStateAt`), not by the browser.
- Card writes are single non-transactional writes, and the demo account may use the board (no write protection, as elsewhere).
