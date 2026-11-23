import { describe, it, expect, vi } from "vitest";
import { loadSite, hidden, antiForgery, type SiteHandle } from "@tests/_common/harness";

// wwwroot/user/custom/AI/UserIndex/js/site.js — the AI chat: a question is sent with the earlier turns to /AI/Ask, whose answer
// streams back as one JSON object per line (tool notices, the answer's pieces, or an error as the last line).

/** The page: the localized texts, the chat, and the form with its anti-forgery token. */
function fixture(): string {
    return (
        hidden("localizerAIThinking", "Thinking") +
        hidden("localizerAINoAnswer", "No answer came") +
        `<button type="button" id="btnAINewChat">New chat</button>
         <div id="aiChatMessages"></div>
         <form id="formAIChat">${antiForgery}
            <textarea id="aiQuestion" maxlength="500"></textarea>
            <button type="submit" id="btnAIAsk">Send</button>
         </form>`
    );
}

/** A request the page sent through `fetch`. */
interface SentRequest {
    url: string;
    init: RequestInit;
}

/** A stream the test feeds piece by piece, and that errors when the page aborts its request. */
class AnswerStream {
    private controller: ReadableStreamDefaultController<Uint8Array> | null = null;
    readonly body = new ReadableStream<Uint8Array>({ start: (c) => { this.controller = c; } });

    /** Sends `text` (or raw bytes) as the next piece. */
    push(piece: string | Uint8Array): void {
        this.controller?.enqueue(typeof piece === "string" ? new TextEncoder().encode(piece) : piece);
    }

    /** Ends the answer. */
    end(): void {
        this.controller?.close();
    }

    /** Fails the answer as an aborted fetch does. */
    abort(): void {
        this.controller?.error(new DOMException("The operation was aborted.", "AbortError"));
    }
}

/** Loads the page with a `fetch` that answers each request from the next stream (or status), and records the requests. */
function load(): { h: SiteHandle; sent: SentRequest[]; answer: (status?: number) => AnswerStream } {
    const h = loadSite("user", "AI", "UserIndex", fixture());
    const sent: SentRequest[] = [];
    const queue: { stream: AnswerStream; status: number }[] = [];
    h.win.fetch = vi.fn<typeof fetch>((input, init) => {
        const next = queue.shift();
        if (next === undefined) throw new Error("no answer scripted");
        sent.push({ url: String(input), init: init ?? {} });
        init?.signal?.addEventListener("abort", () => next.stream.abort());
        return Promise.resolve(new Response(next.stream.body, { status: next.status, headers: { "Content-Type": "application/x-ndjson" } }));
    });
    return {
        h,
        sent,
        answer: (status = 200) => {
            const stream = new AnswerStream();
            queue.push({ stream, status });
            return stream;
        },
    };
}

/** Lets the page's pending promises and stream reads run. */
async function settle(): Promise<void> {
    for (let i = 0; i < 10; i++) await new Promise((resolve) => setTimeout(resolve, 0));
}

/** Types `question` and sends it. */
function ask(h: SiteHandle, question: string): void {
    h.$("#aiQuestion").val(question);
    h.$("#formAIChat").trigger("submit");
}

/** The JSON body of a sent request. */
function bodyOf(request: SentRequest | undefined): unknown {
    return JSON.parse(String(request?.init.body));
}

/** One answer line. */
function line(type: string, text: string): string {
    return JSON.stringify({ type, text }) + "\n";
}

/** The texts of the chat's messages, in order. */
function messages(h: SiteHandle): string[] {
    return h.$("#aiChatMessages .ai-chat-text").toArray().map((el) => el.textContent ?? "");
}

describe("AI/UserIndex", () => {
    it("sends the question with the anti-forgery token and an empty history, and shows it with the thinking status", async () => {
        const { h, sent, answer } = load();
        answer();

        ask(h, "  How much did I spend?  ");
        await settle();

        expect(sent[0]?.url).toBe("/AI/Ask");
        expect(sent[0]?.init.method).toBe("POST");
        expect(sent[0]?.init.headers).toMatchObject({ "RequestVerificationToken": "tok", "Content-Type": "application/json; charset=utf-8" });
        expect(bodyOf(sent[0])).toEqual({ Question: "How much did I spend?", History: [] });
        expect(messages(h)).toEqual(["How much did I spend?", ""]);
        expect(h.$(".ai-chat-assistant .ai-chat-status").text()).toBe("Thinking…");
        expect(h.$("#aiQuestion").val()).toBe("");
        expect(h.$("#btnAIAsk").prop("disabled")).toBe(true);
    });

    it("shows a tool notice as the status, then the answer as it streams, and sends the turn with the next question", async () => {
        const { h, sent, answer } = load();
        const first = answer();
        ask(h, "Assets?");
        await settle();

        first.push(line("tool", "Looking up your assets"));
        await settle();
        expect(h.$(".ai-chat-assistant .ai-chat-status").text()).toBe("Looking up your assets…");

        first.push(line("text", "You have "));
        first.push(line("text", "1,000 KRW."));
        first.end();
        await settle();
        expect(messages(h)).toEqual(["Assets?", "You have 1,000 KRW."]);
        expect(h.$(".ai-chat-assistant .ai-chat-status").text()).toBe("");
        expect(h.$("#btnAIAsk").prop("disabled")).toBe(false);

        answer();
        ask(h, "And incomes?");
        await settle();
        expect(bodyOf(sent[1])).toEqual({
            Question: "And incomes?",
            History: [{ Role: "user", Content: "Assets?" }, { Role: "assistant", Content: "You have 1,000 KRW." }],
        });
    });

    it("reads a line split across pieces, a Korean letter split across bytes included", async () => {
        const { h, answer } = load();
        const stream = answer();
        ask(h, "q");
        await settle();

        const bytes = new TextEncoder().encode(line("text", "식비"));
        stream.push(bytes.slice(0, 13));
        stream.push(bytes.slice(13));
        stream.push(JSON.stringify({ type: "text", text: "!" }));
        stream.end();
        await settle();

        expect(messages(h)[1]).toBe("식비!");
    });

    it("writes the answer as text, never as markup", async () => {
        const { h, answer } = load();
        const stream = answer();
        ask(h, "<b>q</b>");
        stream.push(line("text", "<img src=x onerror=alert(1)>"));
        stream.end();
        await settle();

        expect(h.$("#aiChatMessages img").length).toBe(0);
        expect(h.$("#aiChatMessages b").length).toBe(0);
        expect(messages(h)).toEqual(["<b>q</b>", "<img src=x onerror=alert(1)>"]);
    });

    it("toasts an error line, shows it in place of the answer, and leaves the turn out of the history", async () => {
        const { h, sent, answer } = load();
        const stream = answer();
        ask(h, "q");
        stream.push(line("error", "Please ask the administrator."));
        stream.end();
        await settle();

        expect(h.toastr.error).toHaveBeenCalledWith("Please ask the administrator.");
        expect(messages(h)[1]).toBe("Please ask the administrator.");
        expect(h.$(".ai-chat-assistant .ai-chat-text").hasClass("ai-chat-error")).toBe(true);

        answer();
        ask(h, "again");
        await settle();
        expect(bodyOf(sent[1])).toEqual({ Question: "again", History: [] });
    });

    it("says so when no answer came", async () => {
        const { h, answer } = load();
        const stream = answer();
        ask(h, "q");
        stream.end();
        await settle();

        expect(messages(h)[1]).toBe("No answer came");
    });

    it("shows the generic error for a failed request or a line /AI/Ask does not write (a login page)", async () => {
        const { h, answer } = load();
        answer(500).end();
        ask(h, "q");
        await settle();
        expect(h.toastr.error).toHaveBeenLastCalledWith("L_TemporaryError");
        expect(messages(h)[1]).toBe("L_TemporaryError");

        const page = answer();
        ask(h, "q2");
        page.push("<!DOCTYPE html><html>\n");
        page.end();
        await settle();
        expect(h.toastr.error).toHaveBeenCalledTimes(2);
        expect(h.$("#btnAIAsk").prop("disabled")).toBe(false);
    });

    it("sends nothing for a blank question, or while an answer is still coming", async () => {
        const { h, sent, answer } = load();
        ask(h, "   ");
        await settle();
        expect(sent).toEqual([]);

        answer();
        ask(h, "first");
        await settle();
        ask(h, "second");
        await settle();
        expect(sent.length).toBe(1);
    });

    it("sends on Enter, but not on Shift+Enter or while a Korean word is being composed", async () => {
        const { h, sent, answer } = load();
        answer();
        h.$("#aiQuestion").val("q");

        h.$("#aiQuestion").trigger(h.$.Event("keydown", { key: "Enter", shiftKey: true }));
        h.$("#aiQuestion")[0]?.dispatchEvent(new KeyboardEvent("keydown", { key: "Enter", isComposing: true, bubbles: true }));
        await settle();
        expect(sent.length).toBe(0);

        h.$("#aiQuestion")[0]?.dispatchEvent(new KeyboardEvent("keydown", { key: "Enter", bubbles: true }));
        await settle();
        expect(sent.length).toBe(1);
    });

    it("\"New chat\" stops the answer still coming, empties the chat and forgets the history", async () => {
        const { h, sent, answer } = load();
        const first = answer();
        ask(h, "q");
        first.push(line("text", "a"));
        first.end();
        await settle();
        answer();
        ask(h, "still coming");
        await settle();

        h.$("#btnAINewChat").trigger("click");
        await settle();

        expect(messages(h)).toEqual([]);
        expect(h.toastr.error).not.toHaveBeenCalled();
        expect(h.$("#btnAIAsk").prop("disabled")).toBe(false);
        answer();
        ask(h, "new");
        await settle();
        expect(bodyOf(sent[2])).toEqual({ Question: "new", History: [] });
    });
});
