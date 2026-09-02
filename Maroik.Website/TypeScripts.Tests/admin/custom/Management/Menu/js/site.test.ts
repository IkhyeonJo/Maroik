import { describe, it, expect } from "vitest";
import { loadSite, antiForgery, fireNative } from "@tests/_common/harness";

// wwwroot/admin/custom/Management/Menu/js/site.js
function fixture(): string {
  return (
    antiForgery +
    `<div id="createMenuTabs"></div><div id="editCategoryTabs"></div><div id="editSubCategoryTabs"></div>
     <input id="gridSearch" /><div class="mvc-grid"></div>
     <table><tbody>
       <tr class="clsGridRow" data-id="1" data-categoryid=""></tr>
       <tr class="clsGridRow" data-id="2" data-categoryid="1"></tr>
     </tbody></table>
     <form id="formCreateCategory"></form><form id="formCreateSubCategory"></form>
     <form id="formEditCategory"></form><form id="formEditSubCategory"></form>
     <button id="btnExportExcelMenu"></button>
     <button id="btnEditMenuGridRow" data-errorMessageSelectGridRow="select a row"></button>
     <button id="btnConfirmDeleteMenu" data-errorMessageSelectGridRow="select a row"></button>
     <button id="btnDeleteMenu" data-errorMessageSelectGridRow="select a row"></button>`
  );
}

describe("Management/Menu (admin)", () => {
  it("highlights the sub-category row matching id + categoryId", () => {
    const h = loadSite("admin", "Management", "Menu", fixture());
    fireNative(h.win.document, "rowclick", { data: { Id: 2, CategoryId: 1 } });
    const rows = [...h.win.document.querySelectorAll(".clsGridRow")];
    expect(rows.map((r) => r.classList.contains("table-primary"))).toEqual([false, true]);
  });

  it("errors when Edit is clicked with nothing selected", () => {
    const h = loadSite("admin", "Management", "Menu", fixture());
    h.$("#btnEditMenuGridRow").trigger("click");
    expect(h.toastr.error).toHaveBeenCalledWith("select a row");
    expect(h.ajaxCalls).toHaveLength(0);
  });

  it("Excel export targets /Management/ExportExcelMenu", () => {
    const h = loadSite("admin", "Management", "Menu", fixture());
    h.$("#btnExportExcelMenu").trigger("click");
    expect(h.submittedForms.at(-1)!.action).toContain("/Management/ExportExcelMenu");
  });
});
