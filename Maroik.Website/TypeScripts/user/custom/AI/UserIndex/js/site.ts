/**
 * Script for the **AI chat** page (`Views/AI/UserIndex.cshtml`): the user asks about their own account book and the server's
 * model answers, reading the records only through read-only tools on the server.
 *
 *   • The chat lives here: each question is sent with the earlier turns (`History`), since the server stores nothing.
 *   • `/AI/Ask` streams its answer as one JSON object per line (`{ type, text }`): a `tool` line is the localized notice of a
 *     tool the model runs (shown as the answer's status), `text` lines are the answer's pieces, an `error` line — always the
 *     last — is the localized reason it failed (toasted, and shown in place of the answer).
 *   • The answer is written as text (`.text()`), never as HTML. Enter sends (Shift+Enter is a new line; not while a Korean
 *     word is still being composed); "New chat" forgets the chat and stops an answer still coming.
 *
 * The question's `maxlength` is rendered from AssistantPolicy — a UX mirror only; authoritative in AssistantQuestion (server).
 * IIFE-wrapped, no `import` / `export`.
 */
(function() {
    // The runtime-check helpers the _Layout script defines (see TypeScripts/global.d.ts).
    const { check, parseJson, fieldValue } = window;

    // Cached references: the chat, the form and its question box, and the anti-forgery input.
    const $aiChatMessages = $("#aiChatMessages");
    const $formAIChat = $("#formAIChat");
    const $aiQuestion = $("#aiQuestion");
    const $btnAIAsk = $("#btnAIAsk");
    const $btnAINewChat = $("#btnAINewChat");
    const $__RequestVerificationToken = $("input[name=\"__RequestVerificationToken\"]");
    // The localized texts: the status while the model thinks, the note for an empty answer, the generic error.
    const thinkingText = fieldValue($("#localizerAIThinking"));
    const noAnswerText = fieldValue($("#localizerAINoAnswer"));
    const temporaryErrorText = fieldValue($("#_localizerTemporaryError"));

    /** One line of `/AI/Ask`'s answer (mirrors `AIController.WriteLineAsync`). */
    const answerLine = check.object({
        type: check.oneOf(check.literal("text"), check.oneOf(check.literal("tool"), check.literal("error"))),
        text: check.string,
    });

    /** An earlier turn, as the server's `AIChatTurnInputViewModel` takes it. */
    interface Turn {
        Role: "user" | "assistant";
        Content: string;
    }

    /** The chat so far, oldest first. */
    let history: Turn[] = [];
    /** Stops the answer still coming, or null when none is. */
    let pending: AbortController | null = null;

    /** Adds a message of `role` to the chat and returns its text and status elements. */
    function AddMessage(role: "user" | "assistant", text: string): { $text: JQuery; $status: JQuery } {
        const $message = $("<div>").addClass("direct-chat-msg ai-chat-msg").addClass(role === "user" ? "right ai-chat-user" : "ai-chat-assistant");
        const $text = $("<div>").addClass("direct-chat-text ai-chat-text").text(text);
        const $status = $("<div>").addClass("ai-chat-status text-muted small");
        $message.append($text, $status);
        $aiChatMessages.append($message);
        ScrollToEnd();
        return { $text, $status };
    }

    /** Keeps the newest message in view. */
    function ScrollToEnd() {
        const element = $aiChatMessages[0];
        if (element) element.scrollTop = element.scrollHeight;
    }

    /** Turns the form on or off while an answer is coming. */
    function SetBusy(busy: boolean) {
        $aiQuestion.prop("disabled", busy);
        $btnAIAsk.prop("disabled", busy);
    }

    /**
     * Sends `question` with the chat so far and shows the streamed answer in `answer`. Returns the answer's text and whether it
     * ended with an error line. Throws when the request fails or a line is not one `/AI/Ask` writes (e.g. a login page after the
     * session expired).
     */
    async function Stream(question: string, answer: { $text: JQuery; $status: JQuery }, signal: AbortSignal): Promise<{ text: string; failed: boolean }> {
        const response = await window.fetch("/AI/Ask", {
            method: "POST",
            headers: {
                "Content-Type": "application/json; charset=utf-8",
                "RequestVerificationToken": fieldValue($__RequestVerificationToken),
            },
            body: JSON.stringify({ Question: question, History: history }),
            signal,
        });
        if (!response.ok || response.body === null) {
            throw new Error(`AI answer failed: HTTP ${response.status}`);
        }

        let text = "";
        let failed = false;
        const handle = (line: string) => {
            if (line.trim() === "") return;
            const step = parseJson(line, answerLine, "an AI answer line");
            if (step.type === "text") {
                text += step.text;
                answer.$text.text(text);
                answer.$status.text("");
            } else if (step.type === "tool") {
                answer.$status.text(step.text + "…");
            } else {
                failed = true;
                answer.$status.text("");
                answer.$text.text(step.text).addClass("ai-chat-error");
                toastr.error(step.text);
            }
            ScrollToEnd();
        };

        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = "";
        for (;;) {
            const { done, value } = await reader.read();
            if (done) break;
            buffer += decoder.decode(value, { stream: true });
            let end = buffer.indexOf("\n");
            while (end >= 0) {
                handle(buffer.slice(0, end));
                buffer = buffer.slice(end + 1);
                end = buffer.indexOf("\n");
            }
        }
        handle(buffer + decoder.decode());
        return { text, failed };
    }

    /** Form submit: shows the question, streams the answer, then keeps both in the chat. */
    async function Ask() {
        const question = fieldValue($aiQuestion).trim();
        if (question === "" || pending !== null) return;

        const controller = new AbortController();
        pending = controller;
        SetBusy(true);
        $aiQuestion.val("");
        AddMessage("user", question);
        const answer = AddMessage("assistant", "");
        answer.$status.text(thinkingText + "…");
        try {
            const result = await Stream(question, answer, controller.signal);
            if (result.failed) return;
            if (result.text === "") {
                answer.$text.text(noAnswerText).addClass("ai-chat-error");
                return;
            }
            history.push({ Role: "user", Content: question }, { Role: "assistant", Content: result.text });
        } catch (error) {
            if (controller.signal.aborted) return;
            console.error(error);
            answer.$status.text("");
            answer.$text.text(temporaryErrorText).addClass("ai-chat-error");
            toastr.error(temporaryErrorText);
        } finally {
            if (pending === controller) {
                pending = null;
                SetBusy(false);
                $aiQuestion.trigger("focus");
            }
        }
    }

    $formAIChat.on("submit", function() {
        void Ask();
        return false;
    });
    $aiQuestion.on("keydown", function(event) {
        if (event.key === "Enter" && !event.shiftKey && !event.originalEvent?.isComposing) {
            event.preventDefault();
            $formAIChat.trigger("submit");
        }
    });
    $btnAINewChat.on("click", function() {
        pending?.abort();
        pending = null;
        history = [];
        $aiChatMessages.empty();
        SetBusy(false);
        $aiQuestion.val("").trigger("focus");
    });
})();
