import { describe, it, expect } from "vitest";
import { loadSite, hidden, antiForgery } from "@tests/_common/harness";

// wwwroot/user/custom/AccountBook/Income/js/site.js
//
// Worth locking down: the income main→sub-class UX mirror (ApplyAllowedOptions),
// the Excel export form, and that an invalid create form makes no request.

const subClassMap = { Labor: ["Salary", "Bonus"], Business: ["Sales"] };

function fixture(extra = ""): string {
  return (
    antiForgery +
    hidden("incomeSubClassMap", JSON.stringify(subClassMap).replace(/"/g, "&quot;")) +
    hidden("localizerFailedToLoadAmountLabel", "Failed to load the amount label.") +
    `<div id="createIncomeTabs"></div><div id="editIncomeTabs"></div>
     <input id="createIncomeDate" /><input id="editIncomeDate" /><input id="gridSearch" />
     <select id="createIncomeMainClass">
       <option value="Labor">Labor</option><option value="Business">Business</option>
     </select>
     <select id="createIncomeSubClass">
       <option value="Salary">Salary</option><option value="Bonus">Bonus</option><option value="Sales">Sales</option>
     </select>
     <select id="editIncomeMainClass"></select><select id="editIncomeSubClass"></select>
     <form id="formCreateIncome"></form><form id="formEditIncome"></form>
     <button id="btnExportExcelIncome"></button>
     <button id="btnEditIncomeGridRow"></button><button id="btnConfirmDeleteIncome"></button>
     <button id="btnDeleteIncome"></button>
     <select id="createIncomeDepositMyAssetProductName"><option value="A" selected>A</option></select>
     <select id="editIncomeDepositMyAssetProductName"><option value="A">A</option><option value="B">B</option></select>
     <span id="labelCreateIncomeAmount"></span><span id="labelEditIncomeAmount"></span>` +
    extra
  );
}

describe("AccountBook/Income", () => {
  it("filters the create sub-class options to the selected main class", () => {
    const h = loadSite("user", "AccountBook", "Income", fixture());

    h.$("#createIncomeMainClass").val("Business").trigger("change");

    const opts = [...h.win.document.querySelectorAll<HTMLOptionElement>("#createIncomeSubClass option")];
    const enabled = opts.filter((o) => !o.disabled).map((o) => o.value);
    expect(enabled).toEqual(["Sales"]);
    expect(opts.filter((o) => o.disabled).map((o) => o.value).sort()).toEqual(["Bonus", "Salary"]);
  });

  it("Excel export posts a form to /AccountBook/ExportExcelIncome with fileName=Income", () => {
    const h = loadSite("user", "AccountBook", "Income", fixture());

    h.$("#btnExportExcelIncome").trigger("click");

    expect(h.submittedForms).toHaveLength(1);
    const form = h.submittedForms[0];
    expect(form.method.toUpperCase()).toBe("POST");
    expect(form.action).toContain("/AccountBook/ExportExcelIncome");
    expect(form.querySelector<HTMLInputElement>('input[name="fileName"]')!.value).toBe("Income");
    expect(form.querySelector<HTMLInputElement>('input[name="__RequestVerificationToken"]')!.value).toBe("tok");
  });

  it("does not call $.ajax when the create form is invalid", () => {
    const h = loadSite("user", "AccountBook", "Income", fixture());
    (h.$.fn as any).valid = () => false;

    h.$("#formCreateIncome").trigger("submit");

    expect(h.ajaxCalls.filter((c) => String(c.url).includes("CreateIncome"))).toHaveLength(0);
  });

  it("shows a toastr error when the create-form amount-label lookup fails", () => {
    const h = loadSite("user", "AccountBook", "Income", fixture());
    // loadSite's onload $(function(){...}) already fired one GetIncomeAmountLabel lookup
    // for the preselected create-form asset.
    const call = h.ajaxCalls.find((c) => String(c.url).includes("GetIncomeAmountLabel"));

    call?.error?.();

    expect(h.toastr.error).toHaveBeenCalledWith("Failed to load the amount label.");
  });

  it("shows a toastr error when the edit-form amount-label lookup fails", () => {
    const h = loadSite("user", "AccountBook", "Income", fixture());

    h.$("#editIncomeDepositMyAssetProductName").val("B").trigger("change");
    const call = h.lastAjax();
    expect(call.url).toContain("GetIncomeAmountLabel");

    call.error?.();

    expect(h.toastr.error).toHaveBeenCalledWith("Failed to load the amount label.");
  });

  it("sets the create-form amount label from a successful lookup", () => {
    const h = loadSite("user", "AccountBook", "Income", fixture());
    const call = h.ajaxCalls.find((c) => String(c.url).includes("GetIncomeAmountLabel"));

    call?.success?.({ result: true, label: "KRW" });

    expect(h.win.document.querySelector("#labelCreateIncomeAmount")!.textContent).toBe("KRW");
  });
});
