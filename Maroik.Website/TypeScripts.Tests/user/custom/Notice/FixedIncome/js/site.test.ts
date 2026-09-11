import { describe, it, expect } from "vitest";
import { loadSite, hidden, antiForgery } from "@tests/_common/harness";

// wwwroot/user/custom/Notice/FixedIncome/js/site.js
const subClassMap = { Labor: ["Salary"] };
const maxDepositDayByMonth = { "2": "28", "1": "31" };

function fixture(): string {
  return (
    antiForgery +
    hidden("incomeSubClassMap", JSON.stringify(subClassMap).replace(/"/g, "&quot;")) +
    hidden("maxDepositDayByMonth", JSON.stringify(maxDepositDayByMonth).replace(/"/g, "&quot;")) +
    hidden("noMaturityDate", "9999-12-31") +
    `<div id="createFixedIncomeTabs"></div><div id="editFixedIncomeTabs"></div><input id="gridSearch" />
     <input id="createFixedIncomeMaturityDate" /><input id="editFixedIncomeMaturityDate" />
     <select id="createFixedIncomeMainClass"><option value="Labor">Labor</option></select>
     <select id="createFixedIncomeSubClass"><option value="Salary">Salary</option></select>
     <select id="editFixedIncomeMainClass"></select><select id="editFixedIncomeSubClass"></select>
     <select id="createFixedIncomeDepositMonth"><option value="1">1</option><option value="2">2</option></select>
     <select id="editFixedIncomeDepositMonth"></select>
     <select id="createFixedIncomeDepositDay">${Array.from({ length: 31 }, (_, i) => `<option value="${i + 1}">${i + 1}</option>`).join("")}</select>
     <select id="editFixedIncomeDepositDay"></select>
     <form id="formCreateFixedIncome"></form><form id="formEditFixedIncome"></form>
     <button id="btnExportExcelFixedIncome"></button><button id="btnEditFixedExpenditureGridRow"></button>
     <button id="btnConfirmDeleteFixedIncome"></button><button id="btnDeleteFixedIncome"></button>
     <select id="createFixedIncomeDepositMyAssetProductName"><option value="A" selected>A</option></select>
     <select id="editFixedIncomeDepositMyAssetProductName"></select>`
  );
}

describe("Notice/FixedIncome", () => {
  it("limits the deposit-day options to the max day for the chosen month (Feb → 28)", () => {
    const h = loadSite("user", "Notice", "FixedIncome", fixture());
    h.$("#createFixedIncomeDepositMonth").val("2").trigger("change");
    const enabled = [...h.win.document.querySelectorAll<HTMLOptionElement>("#createFixedIncomeDepositDay option")]
      .filter((o) => !o.disabled).map((o) => Number(o.value));
    expect(Math.max(...enabled)).toBe(28);
    expect(enabled).toHaveLength(28);
  });

  it("Excel export targets /Notice/ExportExcelFixedIncome", () => {
    const h = loadSite("user", "Notice", "FixedIncome", fixture());
    h.$("#btnExportExcelFixedIncome").trigger("click");
    expect(h.submittedForms.at(-1)!.action).toContain("/Notice/ExportExcelFixedIncome");
  });
});
