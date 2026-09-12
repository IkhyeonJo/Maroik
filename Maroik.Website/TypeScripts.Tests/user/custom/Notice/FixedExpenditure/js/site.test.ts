import { describe, it, expect } from "vitest";
import { loadSite, hidden, antiForgery, hiddenByStyle } from "@tests/_common/harness";

// wwwroot/user/custom/Notice/FixedExpenditure/js/site.js
const subClassMap = { Living: ["Rent"], Transfer: ["ToSavings"] };
const depositAssetSubClasses = ["ToSavings"];
const maxDepositDayByMonth = { "2": "28" };

function fixture(): string {
  return (
    antiForgery +
    hidden("expenditureSubClassMap", JSON.stringify(subClassMap).replace(/"/g, "&quot;")) +
    hidden("expenditureDepositAssetSubClasses", JSON.stringify(depositAssetSubClasses).replace(/"/g, "&quot;")) +
    hidden("maxDepositDayByMonth", JSON.stringify(maxDepositDayByMonth).replace(/"/g, "&quot;")) +
    hidden("noMaturityDate", "9999-12-31") +
    hidden("localizerFailedToLoadAmountLabel", "Failed to load the amount label.") +
    `<div id="createFixedExpenditureTabs"></div><div id="editFixedExpenditureTabs"></div><input id="gridSearch" />
     <input id="createFixedExpenditureMaturityDate" /><input id="editFixedExpenditureMaturityDate" />
     <select id="createFixedExpenditureMainClass"><option value="Living">Living</option><option value="Transfer">Transfer</option></select>
     <select id="createFixedExpenditureSubClass"><option value="Rent">Rent</option><option value="ToSavings">ToSavings</option></select>
     <select id="editFixedExpenditureMainClass"></select><select id="editFixedExpenditureSubClass"></select>
     <select id="createFixedExpenditureDepositMonth"><option value="2">2</option></select>
     <select id="editFixedExpenditureDepositMonth"></select>
     <select id="createFixedExpenditureDepositDay">${Array.from({ length: 31 }, (_, i) => `<option value="${i + 1}">${i + 1}</option>`).join("")}</select>
     <select id="editFixedExpenditureDepositDay"></select>
     <div id="divCreateFixedExpenditureMyDepositAsset" style="display:none"></div>
     <div id="divEditFixedExpenditureMyDepositAsset" style="display:none"></div>
     <form id="formCreateFixedExpenditure"></form><form id="formEditFixedExpenditure"></form>
     <button id="btnExportExcelFixedExpenditure"></button><button id="btnEditFixedExpenditureGridRow"></button>
     <button id="btnConfirmDeleteFixedExpenditure"></button><button id="btnDeleteFixedExpenditure"></button>
     <select id="createFixedExpenditurePaymentMethod"><option value="Card" selected>Card</option></select>
     <select id="editFixedExpenditurePaymentMethod"><option value="Card">Card</option><option value="Cash">Cash</option></select>
     <span id="labelCreateFixedExpenditureAmount"></span><span id="labelEditFixedExpenditureAmount"></span>`
  );
}

describe("Notice/FixedExpenditure", () => {
  it("reveals the deposit-asset block only for a transfer sub-class", () => {
    const h = loadSite("user", "Notice", "FixedExpenditure", fixture());
    h.$("#createFixedExpenditureSubClass").val("ToSavings").trigger("change");
    expect(hiddenByStyle(h.win.document.getElementById("divCreateFixedExpenditureMyDepositAsset"))).toBe(false);
    h.$("#createFixedExpenditureSubClass").val("Rent").trigger("change");
    expect(hiddenByStyle(h.win.document.getElementById("divCreateFixedExpenditureMyDepositAsset"))).toBe(true);
  });

  it("Excel export targets /Notice/ExportExcelFixedExpenditure", () => {
    const h = loadSite("user", "Notice", "FixedExpenditure", fixture());
    h.$("#btnExportExcelFixedExpenditure").trigger("click");
    expect(h.submittedForms.at(-1)!.action).toContain("/Notice/ExportExcelFixedExpenditure");
  });

  it("shows a toastr error when the create-form amount-label lookup fails", () => {
    const h = loadSite("user", "Notice", "FixedExpenditure", fixture());
    // loadSite's onload $(function(){...}) already fired one GetFixedExpenditureAmountLabel
    // lookup for the preselected create-form payment method.
    const call = h.ajaxCalls.find((c) => String(c.url).includes("GetFixedExpenditureAmountLabel"));

    call?.error?.();

    expect(h.toastr.error).toHaveBeenCalledWith("Failed to load the amount label.");
  });

  it("shows a toastr error when the edit-form amount-label lookup fails", () => {
    const h = loadSite("user", "Notice", "FixedExpenditure", fixture());

    h.$("#editFixedExpenditurePaymentMethod").val("Cash").trigger("change");
    const call = h.lastAjax();
    expect(call.url).toContain("GetFixedExpenditureAmountLabel");

    call.error?.();

    expect(h.toastr.error).toHaveBeenCalledWith("Failed to load the amount label.");
  });
});
