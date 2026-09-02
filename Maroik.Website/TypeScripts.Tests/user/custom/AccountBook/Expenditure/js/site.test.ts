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
     <select id="editExpenditurePaymentMethod"></select>`
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
});
