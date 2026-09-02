import { describe, it, expect } from "vitest";
import { loadSite, antiForgery, fireNative } from "@tests/_common/harness";

// wwwroot/admin/custom/Management/Account/js/site.js
function fixture(): string {
  return (
    antiForgery +
    `<div id="createAccountTabs"></div><div id="editAccountTabs"></div><input id="gridSearch" />
     <div class="mvc-grid"></div>
     <table><tbody>
       <tr class="clsGridRow" data-email="a@x.com"></tr>
       <tr class="clsGridRow" data-email="b@x.com"></tr>
     </tbody></table>
     <form id="formCreateAccount"></form><form id="formEditAccount"></form>
     <button id="btnExportExcelAccount"></button>
     <button id="btnEditAccountGridRow" data-errorMessageSelectGridRow="select a row"></button>
     <button id="btnConfirmDeleteAccount" data-errorMessageSelectGridRow="select a row"></button>
     <button id="btnDeleteAccount" data-errorMessageSelectGridRow="select a row"></button>`
  );
}

describe("Management/Account (admin)", () => {
  it("highlights the row matching the grid rowclick email", () => {
    const h = loadSite("admin", "Management", "Account", fixture());
    fireNative(h.win.document, "rowclick", { data: { Email: "b@x.com" } });
    const rows = [...h.win.document.querySelectorAll(".clsGridRow")];
    expect(rows.map((r) => r.classList.contains("table-primary"))).toEqual([false, true]);
  });

  it("errors and makes no request when Edit is clicked with nothing selected", () => {
    const h = loadSite("admin", "Management", "Account", fixture());
    h.$("#btnEditAccountGridRow").trigger("click");
    expect(h.toastr.error).toHaveBeenCalledWith("select a row");
    expect(h.ajaxCalls).toHaveLength(0);
  });

  it("Excel export targets /Management/ExportExcelAccount (fileName=Account)", () => {
    const h = loadSite("admin", "Management", "Account", fixture());
    h.$("#btnExportExcelAccount").trigger("click");
    const form = h.submittedForms.at(-1)!;
    expect(form.action).toContain("/Management/ExportExcelAccount");
    expect(form.querySelector<HTMLInputElement>('input[name="fileName"]')!.value).toBe("Account");
  });
});
