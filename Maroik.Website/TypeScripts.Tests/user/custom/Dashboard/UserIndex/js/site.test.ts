import { describe, it, expect } from "vitest";
import { loadSite, hidden, antiForgery, present } from "@tests/_common/harness";

// wwwroot/user/custom/Dashboard/UserIndex/js/site.js — 10 donut charts + period/unit controls.
const CANVAS_IDS = [
    "regularIncomeYear", "irregularIncomeYear", "regularSavingsYear", "nonConsumerSpendingYear",
    "consumerSpendingYear", "regularIncomeYearMonth", "irregularIncomeYearMonth", "regularSavingsYearMonth",
    "nonConsumerSpendingYearMonth", "consumerSpendingYearMonth",
];

/** A hidden input carrying a JSON payload, as Razor encodes it into the attribute. */
function hiddenJson(id: string, value: unknown): string {
    return hidden(id, JSON.stringify(value).replaceAll('"', "&quot;"));
}

/** The dashboard DOM: the chart canvases, the server's charts and the period / unit controls. */
function fixture(amount = 1500): string {
    return (
        antiForgery +
        `<select id="year"><option value="2024" selected>2024</option></select>
     <select id="month"><option value="5" selected>5</option></select>
     <select id="monetaryUnit"><option value="KRW" selected>KRW</option></select>` +
        CANVAS_IDS.map((id) => `<canvas id="${id}"></canvas>`).join("") +
        hiddenJson("dashboardCharts", CANVAS_IDS.map((id, chart) => {
            const count = chart % 5 === 4 ? 12 : 6;
            return {
                CanvasId: id,
                SubClasses: Array.from({ length: count }, (_, slice) => `sub${slice}`),
                Amounts: Array.from({ length: count }, () => amount),
                Percentages: Array.from({ length: count }, () => 12.5),
            };
        })) +
        hiddenJson("dashboardChartLabels", Array.from({ length: 12 }, (_, slice) => ({ Key: `sub${slice}`, Label: `Label ${slice}` }))) +
        hidden("amountMaxDecimalPlaces", "4")
    );
}

describe("Dashboard/UserIndex", () => {
    it("loads and builds one Chart per donut canvas without throwing", () => {
        // The Chart global is stubbed (and jsdom's canvas context by the harness). This asserts the
        // 10-chart $(function) block runs through.
        let h: ReturnType<typeof loadSite> | undefined;
        expect(() => {
            h = loadSite("user", "Dashboard", "UserIndex", fixture());
        }).not.toThrow();
        expect(present(h).charts).toHaveLength(CANVAS_IDS.length);
    });

    it("changing the year navigates to /Dashboard/UserIndex with year & month", () => {
        const h = loadSite("user", "Dashboard", "UserIndex", fixture());
        h.$("#year").trigger("change");
        expect(h.navigations.at(-1)).toBe("/Dashboard/UserIndex?year=2024&month=5");
    });

    it("changing the monetary unit posts to /Dashboard/UserUpdateDefaultMonetary", () => {
        const h = loadSite("user", "Dashboard", "UserIndex", fixture());
        h.$("#monetaryUnit").trigger("change");
        const call = h.lastAjax();
        expect(call.url).toBe("/Dashboard/UserUpdateDefaultMonetary");
        expect(JSON.parse(String(call.data))).toEqual({ DefaultMonetaryUnit: "KRW" });
    });
});

// ---- chart data and tooltips ------------------------------------------------------------------
describe("Dashboard/UserIndex — donut charts", () => {
    it("each of the ten charts is a doughnut fed from the server's charts (one value per label)", () => {
        const h = loadSite("user", "Dashboard", "UserIndex", fixture());
        expect(h.charts).toHaveLength(10);
        for (const { cfg } of h.charts) {
            expect(cfg.type).toBe("doughnut");
            const dataset = present(cfg.data.datasets[0]);
            expect(dataset.data.length).toBe(cfg.data.labels.length);
            expect(dataset.data.length).toBe(dataset.backgroundColor.length);
            expect(dataset.data.every((v: number) => v === 1500)).toBe(true);
        }
    });

    it("every tooltip reads 'Label: 1,500 (12.50%)' for each slice, using the server-supplied percentage", () => {
        const h = loadSite("user", "Dashboard", "UserIndex", fixture());
        let checked = 0;
        for (const { cfg } of h.charts) {
            const label = cfg.options.tooltips.callbacks.label;
            cfg.data.labels.forEach((name: string, index: number) => {
                expect(label({ index }, cfg.data)).toBe(`${name}: ${(1500).toLocaleString()} (12.50%)`);
                checked++;
            });
        }
        expect(checked).toBeGreaterThan(40);
    });

    it("every tooltip shows the amount to the server-supplied number of decimal places (1234.5678 keeps all four)", () => {
        const h = loadSite("user", "Dashboard", "UserIndex", fixture(1234.5678));
        const shown = (1234.5678).toLocaleString(undefined, { maximumFractionDigits: 4 });
        expect(shown).toContain("5678");
        let checked = 0;
        for (const { cfg } of h.charts) {
            const label = cfg.options.tooltips.callbacks.label;
            cfg.data.labels.forEach((name: string, index: number) => {
                expect(label({ index }, cfg.data)).toBe(`${name}: ${shown} (12.50%)`);
                checked++;
            });
        }
        expect(checked).toBeGreaterThan(40);
    });

    it("labels each slice with the server's localized name, and colors a long chart from the long palette", () => {
        const h = loadSite("user", "Dashboard", "UserIndex", fixture());
        const { cfg } = present(h.charts[4]);
        expect(cfg.data.labels[11]).toBe("Label 11");
        expect(present(cfg.data.datasets[0]).backgroundColor).toHaveLength(12);
        expect(present(cfg.data.datasets[0]).backgroundColor[0]).toBe("#FF0000");
        expect(present(present(h.charts[0]).cfg.data.datasets[0]).backgroundColor[0]).toBe("#f56954");
    });

    it("fails fast when the page does not supply the number of decimal places", () => {
        expect(() => loadSite("user", "Dashboard", "UserIndex", fixture().replace(hidden("amountMaxDecimalPlaces", "4"), ""))).toThrow();
    });
});

describe("Dashboard/UserIndex — period and currency controls", () => {
    it("changing the month reloads the dashboard for the chosen year and month", () => {
        const h = loadSite("user", "Dashboard", "UserIndex", fixture());
        h.$("#month").trigger("change");
        expect(h.navigations.at(-1)).toBe("/Dashboard/UserIndex?year=2024&month=5");
    });

    it("changing the currency reloads the dashboard whether or not the save succeeded", () => {
        const h = loadSite("user", "Dashboard", "UserIndex", fixture());
        h.$("#monetaryUnit").trigger("change");
        expect(h.lastAjax().headers).toEqual({ RequestVerificationToken: "tok" });
        h.respond(0, { result: true });
        h.$("#monetaryUnit").trigger("change");
        h.respond(0, { result: false });
        expect(h.navigations).toEqual(["/Dashboard/UserIndex?year=2024&month=5", "/Dashboard/UserIndex?year=2024&month=5"]);
    });
});
