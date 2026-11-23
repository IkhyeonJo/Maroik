---
paths:
  - "**/AI/**"
  - "**/AIChats/**"
  - "**/AI*Controller*"
  - "**/*AIChat*"
  - "**/*AIPolicy*"
  - "**/*AIToolbox*"
  - "**/Assistants/**"
  - "**/*Llm*"
  - "**/*Ollama*"
---

# Intended design: AI chat

Part of CLAUDE.md's "Intended design" (do not "fix" these): the items below look like gaps or smells but are
deliberate. Do not change them, add guards around them, or refactor them away unless the owner explicitly asks for
that specific change. Check every candidate review finding in this area against them before reporting it.

- The AI chat (`/AI/UserIndex`, User role only; since 2026-11-24, owner's request) answers questions about the signed-in user's
  own account book with a local model on the server: Ollama (`Maroik.Ollama`) on the internal Docker network, CPU only, one
  question at a time (`OLLAMA_NUM_PARALLEL=1`, the container capped at 8 GB). The default model is Llama 3.1 8B (`AI_MODEL` /
  `AI__Model`, a setting). No Chinese models (Qwen, DeepSeek, ...; owner's decision, 2026-11-24). Like the other internal
  services, Ollama has no authentication: the network is its boundary.
- The chat is off for every account until an administrator ticks "Allow the AI chat" in `Management/Account`
  (`Account.AIAllowed`, default false; in the grid, its search, the Excel export and the admin update audit line), because each
  question keeps the server's CPU busy for a while. The demo account is treated like any other account (off).
- Every fact comes through function calling: the model gets only read-only tools (`AIToolbox`: assets, incomes, expenditures,
  expenditure totals by class, fixed incomes, fixed expenditures). Each tool reads only the signed-in account — the account is
  never an argument — and there are no write tools; the model cannot add, change or delete anything. The model names a period
  ("this_month") and the domain works out the days from the account's today (`AssistantPeriod`); the sums are added up by the
  server (`AssistantTotals`), never by the model. A refused tool call goes back to the model as `{"error": ...}` so it can try
  again; at most `AssistantPolicy.MaxToolRounds` rounds, then "could not answer".
- Nothing is stored: the browser keeps the chat and sends the earlier turns with each question. The server keeps only the last
  `MaxHistoryMessages` user / assistant turns (a forged "system" or "tool" turn is dropped) and never logs the question's text.
- `/AI/Ask` streams newline-delimited JSON (`{ type, text }` per line) instead of one `{ result, error }` reply, so the answer
  shows as it is written; a refusal or failure is its last line, of type `error` (HTTP 200, the localized message, never the
  exception's text). Its failure contract is the dedicated `AIControllerTests.Ask_AServiceFailure_EndsWithTheGenericMessage_AndIsLogged`.
  The page writes the answer as plain text (no Markdown or HTML rendering).
- There is no per-user rate limit or question quota: the administrator's per-account switch and Ollama's one-at-a-time queue are
  the limit (see Accounts on rate limiting).
