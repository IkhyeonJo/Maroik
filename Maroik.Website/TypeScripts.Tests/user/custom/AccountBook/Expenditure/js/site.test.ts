import { describe, it, expect } from "vitest";
import { loadSite, hidden, antiForgery, hiddenByStyle } from "@tests/_common/harness";

// wwwroot/user/custom/AccountBook/Expenditure/js/site.js
const subClassMap = { Living: ["Food", "Rent"], Transfer: ["ToSavings"] };
const depositAssetSubClasses = ["ToSavings"];

function fixture(): string {
  return (
    antiForgery +
    hidden("expenditureSubClassMap", JSON.stringify(subClassMap).replace(/"/g, "&quot;")) +
    hidden("expenditureDepositAssetSubClasses", JSON.stringify(depositAssetSubClasses).replace(/"/g, "&quot;")) +
    hidden("localizerFailedToLoadAmountLabel", "Failed to load the amount label.") +
    `<div id="createExpenditureTabs"></div><div id="editExpenditureTabs"></div>
     <input id="createExpenditureDate" value="2024-05-04" /><input id="editExpenditureDate" /><input id="gridSearch" />
     <select id="createExpenditureMainClass"><option value="Living">Living</option><option value="Transfer">Transfer</option></select>
     <select id="createExpenditureSubClass"><option value="Food">Food</option><option value="Rent">Rent</option><option value="ToSavings">ToSavings</option></select>
     <select id="editExpenditureMainClass"></select><select id="editExpenditureSubClass"></select>
     <div id="divCreateExpenditureMyDepositAsset" style="display:none"></div>
     <div id="divEditExpenditureMyDepositAsset" style="display:none"></div>
     <form id="formCreateExpenditure"></form><form id="formEditExpenditure"></form>
     <button id="btnExportExcelExpenditure"></button>
     <button id="btnEditExpenditureGridRow"></button><button id="btnConfirmDeleteExpenditure"></button>
     <button id="btnDeleteExpenditure"></button>
     <select id="createExpenditurePaymentMethod"><option value="Card" selected>Card</option></select>
     <select id="editExpenditurePaymentMethod"><option value="Card">Card</option><option value="Cash">Cash</option></select>
     <span id="labelCreateExpenditureAmount"></span><span id="labelEditExpenditureAmount"></span>`
  );
}

describe("AccountBook/Expenditure", () => {
  it("restricts sub-class options to the chosen main class", () => {
    const h = loadSite("user", "AccountBook", "Expenditure", fixture());
    h.$("#createExpenditureMainClass").val("Living").trigger("change");
    const enabled = [...h.win.document.querySelectorAll<HTMLOptionElement>("#createExpenditureSubClass option")]
      .filter((o) => !o.disabled).map((o) => o.value).sort();
    expect(enabled).toEqual(["Food", "Rent"]);
  });

  it("only reveals the deposit-asset block for a transfer sub-class", () => {
    const h = loadSite("user", "AccountBook", "Expenditure", fixture());
    h.$("#createExpenditureMainClass").val("Transfer").trigger("change"); // sub-class becomes ToSavings
    expect(hiddenByStyle(h.win.document.getElementById("divCreateExpenditureMyDepositAsset"))).toBe(false);

    h.$("#createExpenditureMainClass").val("Living").trigger("change"); // sub-class becomes Food
    expect(hiddenByStyle(h.win.document.getElementById("divCreateExpenditureMyDepositAsset"))).toBe(true);
  });

  it("Excel export targets /AccountBook/ExportExcelExpenditure", () => {
    const h = loadSite("user", "AccountBook", "Expenditure", fixture());
    h.$("#btnExportExcelExpenditure").trigger("click");
    expect(h.submittedForms.at(-1)!.action).toContain("/AccountBook/ExportExcelExpenditure");
  });

  it("shows a toastr error when the create-form amount-label lookup fails", () => {
    const h = loadSite("user", "AccountBook", "Expenditure", fixture());
    // loadSite's onload $(function(){...}) already fired one GetExpenditureAmountLabel lookup
    // for the preselected create-form payment method.
    const call = h.ajaxCalls.find((c) => String(c.url).includes("GetExpenditureAmountLabel"));

    call?.error?.();

    expect(h.toastr.error).toHaveBeenCalledWith("Failed to load the amount label.");
  });

  it("shows a toastr error when the edit-form amount-label lookup fails", () => {
    const h = loadSite("user", "AccountBook", "Expenditure", fixture());

    h.$("#editExpenditurePaymentMethod").val("Cash").trigger("change");
    const call = h.lastAjax();
    expect(call.url).toContain("GetExpenditureAmountLabel");

    call.error?.();

    expect(h.toastr.error).toHaveBeenCalledWith("Failed to load the amount label.");
  });
});
