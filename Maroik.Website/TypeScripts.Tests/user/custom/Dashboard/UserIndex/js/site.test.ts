import { describe, it, expect } from "vitest";
import { loadSite, hidden, antiForgery } from "@tests/_common/harness";

// wwwroot/user/custom/Dashboard/UserIndex/js/site.js — 10 donut charts + period/unit controls.
const CANVAS_IDS = [
  "regularIncomeYear", "irregularIncomeYear", "regularSavingsYear", "nonConsumerSpendingYear",
  "consumerSpendingYear", "regularIncomeYearMonth", "irregularIncomeYearMonth", "regularSavingsYearMonth",
  "nonConsumerSpendingYearMonth", "consumerSpendingYearMonth",
];

function fixture(): string {
  return (
    antiForgery +
    `<select id="year"><option value="2024" selected>2024</option></select>
     <select id="month"><option value="5" selected>5</option></select>
     <select id="monetaryUnit"><option value="KRW" selected>KRW</option></select>` +
    CANVAS_IDS.map((id) => `<canvas id="${id}"></canvas>`).join("") +
    // the Number($('#…').val()) inputs — absent ones just become NaN, which is fine
    ["regularIncomeLaborIncomeYear", "regularIncomeBusinessIncomeYear"].map((id) => hidden(id, "10")).join("")
  );
}

describe("Dashboard/UserIndex", () => {
  it("loads and builds one Chart per donut canvas without throwing", () => {
    // jsdom canvas.getContext returns null; the Chart global is stubbed. This asserts
    // the 10-chart $(function) block survives that.
    let h!: ReturnType<typeof loadSite>;
    expect(() => { h = loadSite("user", "Dashboard", "UserIndex", fixture()); }).not.toThrow();
    expect(h.charts).toHaveLength(CANVAS_IDS.length);
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
