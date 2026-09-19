"use strict";
(function () {
    const $createMenuTabs = $("#createMenuTabs");
    const $editCategoryTabs = $("#editCategoryTabs");
    const $editSubCategoryTabs = $("#editSubCategoryTabs");
    const $gridSearch = $("#gridSearch");
    const $formCreateCategory = $("#formCreateCategory");
    const $createCategoryName = $("#createCategoryName");
    const $createCategoryDisplayName = $("#createCategoryDisplayName");
    const $createCategoryIconPath = $("#createCategoryIconPath");
    const $createCategoryController = $("#createCategoryController");
    const $createCategoryAction = $("#createCategoryAction");
    const $createCategoryRole = $("#createCategoryRole");
    const $createCategoryOrder = $("#createCategoryOrder");
    const $__RequestVerificationToken = $('input[name="__RequestVerificationToken"]');
    const $createMenuDialogModal = $("#createMenuDialogModal");
    const $formCreateSubCategory = $("#formCreateSubCategory");
    const $createSubcategoryCategoryId = $("#createSubcategoryCategoryId");
    const $createSubcategoryName = $("#createSubcategoryName");
    const $createSubcategoryDisplayName = $("#createSubcategoryDisplayName");
    const $createSubcategoryIconPath = $("#createSubcategoryIconPath");
    const $createSubcategoryAction = $("#createSubcategoryAction");
    const $createSubcategoryRole = $("#createSubcategoryRole");
    const $createSubcategoryOrder = $("#createSubcategoryOrder");
    const $editCategoryId = $("#editCategoryId");
    const $editCategoryName = $("#editCategoryName");
    const $editCategoryDisplayName = $("#editCategoryDisplayName");
    const $editCategoryIconPath = $("#editCategoryIconPath");
    const $editCategoryController = $("#editCategoryController");
    const $editCategoryAction = $("#editCategoryAction");
    const $editCategoryRole = $("#editCategoryRole");
    const $editCategoryOrder = $("#editCategoryOrder");
    const $editCategoryDialogModal = $("#editCategoryDialogModal");
    const $editSubCategoryId = $("#editSubCategoryId");
    const $editSubCategoryCategoryId = $("#editSubCategoryCategoryId");
    const $editSubCategoryName = $("#editSubCategoryName");
    const $editSubCategoryDisplayName = $("#editSubCategoryDisplayName");
    const $editSubCategoryIconPath = $("#editSubCategoryIconPath");
    const $editSubCategoryAction = $("#editSubCategoryAction");
    const $editSubCategoryRole = $("#editSubCategoryRole");
    const $editSubCategoryOrder = $("#editSubCategoryOrder");
    const $editSubCategoryDialogModal = $("#editSubCategoryDialogModal");
    const $formEditCategory = $("#formEditCategory");
    const $formEditSubCategory = $("#formEditSubCategory");
    const $confirmDeleteMenuDialogModal = $("#confirmDeleteMenuDialogModal");
    const $btnEditMenuGridRow = $("#btnEditMenuGridRow");
    const $btnConfirmDeleteMenu = $("#btnConfirmDeleteMenu");
    const $btnExportExcelMenu = $("#btnExportExcelMenu");
    const $btnDeleteMenu = $("#btnDeleteMenu");
    const selectedRowColor = "table-primary";
    $(function () {
        $createMenuTabs.tabs();
        $editCategoryTabs.tabs();
        $editSubCategoryTabs.tabs();
    });
    $(document).off("rowclick.Menu").on("rowclick.Menu", (e) => {
        let selectedRow = e.detail;
        let selectedRowId = selectedRow.data.Id;
        let selectedRowCategoryId = selectedRow.data.CategoryId;
        if (selectedRowCategoryId === "") {
            selectedRowCategoryId = "-1";
        }
        let $clsGridRow = $(".clsGridRow");
        $clsGridRow.each(function () {
            $(this).removeClass(selectedRowColor);
        });
        $clsGridRow.each(function () {
            if ((String(selectedRowId).valueOf() === String($(this).attr("data-id")).valueOf()) && (String(selectedRowCategoryId).valueOf() === String($(this).attr("data-categoryid")).valueOf())) {
                $(this).addClass(selectedRowColor);
            }
        });
    });
    $(document).off("reloadstart.Menu").on("reloadstart.Menu", _ => {
    });
    $(document).off("reloadend.Menu").on("reloadend.Menu", _ => {
    });
    $(document).off("reloadfail.Menu").on("reloadfail.Menu", _ => {
    });
    $(document).off("gridconfigure.Menu").on("gridconfigure.Menu", _ => {
    });
    $(document).off("dblclick.Menu", ".clsGridRow").on("dblclick.Menu", ".clsGridRow", function () {
        $(".clsGridRow").each(function () {
            if ($(this).attr("class").includes(selectedRowColor)) {
                EditMenuGridRow();
            }
        });
    });
    $gridSearch.off("input").on("input", function () {
        const grid = new MvcGrid(document.querySelector(".mvc-grid"));
        grid.url.searchParams.set("wholeSearch", this.value);
        grid.reload();
    });
    function CreateCategory() {
        if (!$formCreateCategory.valid()) {
            return false;
        }
        let name = $createCategoryName.val();
        let displayName = $createCategoryDisplayName.val();
        let iconPath = $createCategoryIconPath.val();
        let controller = $createCategoryController.val();
        let action = $createCategoryAction.val();
        let role = $createCategoryRole.val();
        let order = $createCategoryOrder.val();
        let paramValue = JSON.stringify({
            Name: name,
            DisplayName: displayName,
            IconPath: iconPath,
            Controller: controller,
            Action: action,
            Role: role,
            Order: order
        });
        $.ajax({
            url: "/Management/CreateCategory",
            type: "POST",
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: "json",
            data: paramValue,
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                if (data.result) {
                    $createMenuDialogModal.modal("hide");
                    const grid = new MvcGrid(document.querySelector(".mvc-grid"));
                    grid.reload();
                    toastr.success(data.message);
                }
                else {
                    toastr.error(data.error);
                }
            }
        });
        return false;
    }
    function CreateSubCategory() {
        if (!$formCreateSubCategory.valid()) {
            return false;
        }
        let categoryId = $createSubcategoryCategoryId.val();
        let name = $createSubcategoryName.val();
        let displayName = $createSubcategoryDisplayName.val();
        let iconPath = $createSubcategoryIconPath.val();
        let action = $createSubcategoryAction.val();
        let role = $createSubcategoryRole.val();
        let order = $createSubcategoryOrder.val();
        let paramValue = JSON.stringify({
            CategoryId: categoryId,
            Name: name,
            DisplayName: displayName,
            IconPath: iconPath,
            Action: action,
            Role: role,
            Order: order
        });
        $.ajax({
            url: "/Management/CreateSubCategory",
            type: "POST",
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: "json",
            data: paramValue,
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                if (data.result) {
                    $createMenuDialogModal.modal("hide");
                    const grid = new MvcGrid(document.querySelector(".mvc-grid"));
                    grid.reload();
                    toastr.success(data.message);
                }
                else {
                    toastr.error(data.error);
                }
            }
        });
        return false;
    }
    function EditMenuGridRow(errorMessageSelectGridRow) {
        let selectedRowId = "-1";
        let selectedRowCategoryId = "-1";
        $(".clsGridRow").each(function () {
            if ($(this).attr("class").includes(selectedRowColor)) {
                selectedRowId = $(this).attr("data-id");
                selectedRowCategoryId = $(this).attr("data-categoryid");
            }
        });
        if (selectedRowId === "-1") {
            toastr.error(errorMessageSelectGridRow);
            return false;
        }
        if (selectedRowCategoryId === "-1") {
            $.ajax({
                url: "/Management/IsCategoryExists" + "?id=" + selectedRowId,
                type: "POST",
                headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
                dataType: "json",
                data: null,
                contentType: "application/json; charset=utf-8",
                success: function (data) {
                    if (data.result) {
                        $editCategoryId.val(data.category.id);
                        $editCategoryName.val(data.category.name);
                        $editCategoryDisplayName.val(data.category.displayName);
                        $editCategoryIconPath.val(data.category.iconPath);
                        $editCategoryController.val(data.category.controller);
                        $editCategoryAction.val(data.category.action);
                        $editCategoryRole.val(data.category.role).change();
                        $editCategoryOrder.val(data.category.order);
                        $editCategoryDialogModal.modal({
                            keyboard: false,
                            backdrop: "static"
                        });
                        $editCategoryDialogModal.modal("toggle");
                        $editCategoryDialogModal.modal("show");
                    }
                    else {
                        toastr.error(data.error);
                    }
                }
            });
        }
        else {
            $.ajax({
                url: "/Management/IsSubCategoryExists" + "?id=" + selectedRowId,
                type: "POST",
                headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
                dataType: "json",
                data: null,
                contentType: "application/json; charset=utf-8",
                success: function (data) {
                    if (data.result) {
                        $editSubCategoryId.val(data.subCategory.id);
                        $editSubCategoryCategoryId.val(data.subCategory.categoryId);
                        $editSubCategoryName.val(data.subCategory.name);
                        $editSubCategoryDisplayName.val(data.subCategory.displayName);
                        $editSubCategoryIconPath.val(data.subCategory.iconPath);
                        $editSubCategoryAction.val(data.subCategory.action);
                        $editSubCategoryRole.val(data.subCategory.role).change();
                        $editSubCategoryOrder.val(data.subCategory.order);
                        $editSubCategoryDialogModal.modal({
                            keyboard: false,
                            backdrop: "static"
                        });
                        $editSubCategoryDialogModal.modal("toggle");
                        $editSubCategoryDialogModal.modal("show");
                    }
                    else {
                        toastr.error(data.error);
                    }
                }
            });
        }
    }
    function UpdateCategory() {
        if (!$formEditCategory.valid()) {
            return false;
        }
        let id = $editCategoryId.val();
        let name = $editCategoryName.val();
        let displayName = $editCategoryDisplayName.val();
        let iconPath = $editCategoryIconPath.val();
        let controller = $editCategoryController.val();
        let action = $editCategoryAction.val();
        let role = $editCategoryRole.val();
        let order = $editCategoryOrder.val();
        let paramValue = JSON.stringify({
            Id: id,
            Name: name,
            DisplayName: displayName,
            IconPath: iconPath,
            Controller: controller,
            Action: action,
            Role: role,
            Order: order
        });
        $.ajax({
            url: "/Management/UpdateCategory",
            type: "POST",
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: "json",
            data: paramValue,
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                if (data.result) {
                    $editCategoryDialogModal.modal("hide");
                    const grid = new MvcGrid(document.querySelector(".mvc-grid"));
                    grid.reload();
                    toastr.success(data.message);
                }
                else {
                    toastr.error(data.error);
                }
            }
        });
        return false;
    }
    function UpdateSubCategory() {
        if (!$formEditSubCategory.valid()) {
            return false;
        }
        let id = $editSubCategoryId.val();
        let categoryId = $editSubCategoryCategoryId.val();
        let name = $editSubCategoryName.val();
        let displayName = $editSubCategoryDisplayName.val();
        let iconPath = $editSubCategoryIconPath.val();
        let action = $editSubCategoryAction.val();
        let role = $editSubCategoryRole.val();
        let order = $editSubCategoryOrder.val();
        let paramValue = JSON.stringify({
            Id: id,
            CategoryId: categoryId,
            Name: name,
            DisplayName: displayName,
            IconPath: iconPath,
            Action: action,
            Role: role,
            Order: order
        });
        $.ajax({
            url: "/Management/UpdateSubCategory",
            type: "POST",
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: "json",
            data: paramValue,
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                if (data.result) {
                    $editSubCategoryDialogModal.modal("hide");
                    const grid = new MvcGrid(document.querySelector(".mvc-grid"));
                    grid.reload();
                    toastr.success(data.message);
                }
                else {
                    toastr.error(data.error);
                }
            }
        });
        return false;
    }
    function ConfirmDeleteMenu(errorMessageSelectGridRow) {
        let selectedRowId = "-1";
        $(".clsGridRow").each(function () {
            if ($(this).attr("class").includes(selectedRowColor)) {
                selectedRowId = $(this).attr("data-id");
            }
        });
        if (selectedRowId === "-1") {
            toastr.error(errorMessageSelectGridRow);
            return false;
        }
        $confirmDeleteMenuDialogModal.modal({
            keyboard: false,
            backdrop: "static"
        });
        $confirmDeleteMenuDialogModal.modal("toggle");
        $confirmDeleteMenuDialogModal.modal("show");
    }
    function DeleteMenu(errorMessageSelectGridRow) {
        let selectedRowId = "-1";
        let selectedRowCategoryId = "-1";
        $(".clsGridRow").each(function () {
            if ($(this).attr("class").includes(selectedRowColor)) {
                selectedRowId = $(this).attr("data-id");
                selectedRowCategoryId = $(this).attr("data-categoryid");
            }
        });
        if (selectedRowId === "-1") {
            toastr.error(errorMessageSelectGridRow);
            return false;
        }
        if (selectedRowCategoryId === "-1") {
            $.ajax({
                url: "/Management/IsCategoryExists" + "?id=" + selectedRowId,
                type: "POST",
                headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
                dataType: "json",
                data: null,
                contentType: "application/json; charset=utf-8",
                success: function (data) {
                    if (data.result) {
                        let paramValue = JSON.stringify({
                            Id: data.category.id,
                            Name: data.category.name,
                            DisplayName: data.category.displayName,
                            IconPath: data.category.iconPath,
                            Controller: data.category.controller,
                            Action: data.category.action,
                            Role: data.category.role,
                            Order: data.category.order
                        });
                        $.ajax({
                            url: "/Management/DeleteCategory",
                            type: "POST",
                            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
                            dataType: "json",
                            data: paramValue,
                            contentType: "application/json; charset=utf-8",
                            success: function (data) {
                                if (data.result) {
                                    $confirmDeleteMenuDialogModal.modal("hide");
                                    const grid = new MvcGrid(document.querySelector(".mvc-grid"));
                                    grid.reload();
                                    toastr.success(data.message);
                                }
                                else {
                                    toastr.error(data.error);
                                }
                            }
                        });
                    }
                    else {
                        toastr.error(data.error);
                    }
                }
            });
        }
        else {
            $.ajax({
                url: "/Management/IsSubCategoryExists" + "?id=" + selectedRowId,
                type: "POST",
                headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
                dataType: "json",
                data: null,
                contentType: "application/json; charset=utf-8",
                success: function (data) {
                    if (data.result) {
                        let paramValue = JSON.stringify({
                            Id: data.subCategory.id,
                            CategoryId: data.subCategory.categoryId,
                            Name: data.subCategory.name,
                            DisplayName: data.subCategory.displayName,
                            IconPath: data.subCategory.iconPath,
                            Action: data.subCategory.action,
                            Role: data.subCategory.role,
                            Order: data.subCategory.order
                        });
                        $.ajax({
                            url: "/Management/DeleteSubCategory",
                            type: "POST",
                            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
                            dataType: "json",
                            data: paramValue,
                            contentType: "application/json; charset=utf-8",
                            success: function (data) {
                                if (data.result) {
                                    $confirmDeleteMenuDialogModal.modal("hide");
                                    const grid = new MvcGrid(document.querySelector(".mvc-grid"));
                                    grid.reload();
                                    toastr.success(data.message);
                                }
                                else {
                                    toastr.error(data.error);
                                }
                            }
                        });
                    }
                    else {
                        toastr.error(data.error);
                    }
                }
            });
        }
    }
    function ExportExcelMenu() {
        let form = document.createElement("form");
        let element1 = document.createElement("input");
        let element2 = document.createElement("input");
        form.method = "POST";
        form.action = "/Management/ExportExcelMenu";
        element1.name = "__RequestVerificationToken";
        element1.value = $__RequestVerificationToken.val();
        form.appendChild(element1);
        element2.name = "fileName";
        element2.value = "Menu";
        form.appendChild(element2);
        document.body.appendChild(form);
        form.submit();
    }
    $btnEditMenuGridRow.off("click").on("click", function () {
        EditMenuGridRow($(this).attr("data-errorMessageSelectGridRow"));
    });
    $btnConfirmDeleteMenu.off("click").on("click", function () {
        ConfirmDeleteMenu($(this).attr("data-errorMessageSelectGridRow"));
    });
    $btnExportExcelMenu.off("click").on("click", function () {
        ExportExcelMenu();
    });
    $btnDeleteMenu.off("click").on("click", function () {
        DeleteMenu($(this).attr("data-errorMessageSelectGridRow"));
    });
    $formCreateCategory.off("submit").on("submit", function () {
        return CreateCategory();
    });
    $formCreateSubCategory.off("submit").on("submit", function () {
        return CreateSubCategory();
    });
    $formEditCategory.off("submit").on("submit", function () {
        return UpdateCategory();
    });
    $formEditSubCategory.off("submit").on("submit", function () {
        return UpdateSubCategory();
    });
})();
