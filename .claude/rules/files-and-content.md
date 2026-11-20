---
paths:
  - "**/Boards/**"
  - "**/*Board*"
  - "**/Forum/**"
  - "**/*Forum*"
  - "**/*PrivateNote*"
  - "**/*Sanitizer*"
  - "**/*HtmlParser*"
  - "**/*HtmlContent*"
  - "**/*Attach*"
  - "**/Uploads/**"
  - "**/*Upload*"
  - "**/*Summernote*"
  - "**/*ImageUpload*"
  - "**/*ImageValidator*"
  - "**/StrippedImage*"
  - "**/*FileClient*"
  - "Maroik.FileStorage/**"
  - "Maroik.FileStorage.Tests/**"
  - "**/*CalendarEvent*"
  - "**/*CalendarService*"
  - "**/*CalendarController*"
  - "**/*Management*"
---

# Intended design: Files and content

Part of CLAUDE.md's "Intended design" (do not "fix" these): the items below look like gaps or smells but are
deliberate. Do not change them, add guards around them, or refactor them away unless the owner explicitly asks for
that specific change. Check every candidate review finding in this area against them before reporting it.

- Maroik.FileStorage is only ever reachable on the internal network; its `[AllowAnonymous]` endpoints
  and always-on Swagger UI are intended.
- Editing a post, private note or calendar event without choosing a new file removes the existing
  attachment (pinned by tests such as `EditBoardAsync_ClearsTheExistingAttachmentRecord_WhenNoFileIsSubmitted`).
- The same holds when an admin clears the lock on someone else's FreeForum post (the
  `BoardEdit.LockCleared` path of `BoardService.EditBoardAsync`): `HandleAttachmentAsync` still runs, so
  a request without a file removes the writer's attachment (pinned by
  `EditBoardAsync_AdminNonOwner_ClearingALockWithoutAFile_ClearsTheWritersAttachmentRecord`).
- Inline images in post / event bodies are fetched and embedded as base64 on every view
  (`PrepareHtmlForDisplayAsync`). Keep that rendering path. It is bounded (since 2026-11-10): a body holds at most
  `TitledContentPolicy.MaxInlineImages` (20) `<img>` tags, a repeated image counted each time, checked on the sanitized body
  when a post, private note or event is written (`IAttachmentContentService.CheckInlineImages`), and one view downloads a
  repeated image once. Without it ~180 tags of one 10 MB image fit in a body and one view built gigabytes in the web process.
- Replacing or clearing an attachment (post, private note, calendar event) only rewrites its database
  record; the old file stays in Maroik.FileStorage, as does a file uploaded by a write whose transaction
  later rolls back. There is no delete API for them on purpose — do not add one or a cleanup job. (The File Manager's
  `api/FileManager/delete` and the class-suggestion models' `api/ClassModel/delete` are the exceptions, and each only accepts its own
  paths, `upload/FileManager/{guid}` / `upload/ClassModels/{guid}.zip`; see File Manager and Finance.)
- `HtmlContentSanitizerService` starts from the original allow-list (Ganss defaults plus `class`) and narrows only
  three things: URL schemes to http/https, no `data-*` attributes, and no `position` / `top` / `left` / `right` /
  `bottom` / `z-index` / `opacity` in inline styles (so a post cannot overlay the page). The rest of that
  breadth — the `name` attribute, form elements (`form`, `input`, `button`, `select`, `textarea`) — is kept on
  purpose. Do not raise it again.
- An administrator cannot clear someone else's post lock from the UI (the edit page is owner-only); only a direct
  POST to the edit endpoint can.
- `BoardOutputViewModel.BoardAttachedFilePath` (Forum and Management) is populated by the controllers but
  not rendered by any view. Keep it; do not remove it, and do not render the storage path in a view or
  JSON response.
