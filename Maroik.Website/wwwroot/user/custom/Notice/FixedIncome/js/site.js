"use strict";
(function () {
    const $createFixedIncomeTabs = $("#createFixedIncomeTabs");
    const $editFixedIncomeTabs = $("#editFixedIncomeTabs");
    const $createFixedIncomeMaturityDate = $("#createFixedIncomeMaturityDate");
    const $editFixedIncomeMaturityDate = $("#editFixedIncomeMaturityDate");
    const $gridSearch = $("#gridSearch");
    const $createFixedIncomeSubClass = $("#createFixedIncomeSubClass");
    const $createFixedIncomeDepositDay = $("#createFixedIncomeDepositDay");
    const $editFixedIncomeSubClass = $("#editFixedIncomeSubClass");
    const $editFixedIncomeDepositDay = $("#editFixedIncomeDepositDay");
    const $formCreateFixedIncome = $("#formCreateFixedIncome");
    const $createFixedIncomeMainClass = $("#createFixedIncomeMainClass");
    const $createFixedIncomeContent = $("#createFixedIncomeContent");
    const $createFixedIncomeAmount = $("#createFixedIncomeAmount");
    const $createFixedIncomeDepositMonth = $("#createFixedIncomeDepositMonth");
    const $createFixedIncomeNote = $("#createFixedIncomeNote");
    const $createFixedIncomeDepositMyAssetProductName = $("#createFixedIncomeDepositMyAssetProductName");
    const $__RequestVerificationToken = $('input[name="__RequestVerificationToken"]');
    const failedToLoadAmountLabelMessage = $("#localizerFailedToLoadAmountLabel").val();
    const $createFixedIncomeDialogModal = $("#createFixedIncomeDialogModal");
    const $editFixedIncomeId = $("#editFixedIncomeId");
    const $editFixedIncomeMainClass = $("#editFixedIncomeMainClass");
    const $editFixedIncomeContent = $("#editFixedIncomeContent");
    const $editFixedIncomeAmount = $("#editFixedIncomeAmount");
    const $editFixedIncomeDepositMonth = $("#editFixedIncomeDepositMonth");
    const $editFixedIncomeNote = $("#editFixedIncomeNote");
    const $editFixedIncomeDepositMyAssetProductName = $("#editFixedIncomeDepositMyAssetProductName");
    const $editFixedIncomeUnpunctuality = $("#editFixedIncomeUnpunctuality");
    const $editFixedIncomeDialogModal = $("#editFixedIncomeDialogModal");
    const $formEditFixedIncome = $("#formEditFixedIncome");
    const $confirmDeleteFixedIncomeDialogModal = $("#confirmDeleteFixedIncomeDialogModal");
    const $labelCreateFixedIncomeAmount = $("#labelCreateFixedIncomeAmount");
    const $labelEditFixedIncomeAmount = $("#labelEditFixedIncomeAmount");
    const $btnEditFixedIncomeGridRow = $("#btnEditFixedIncomeGridRow");
    const $btnConfirmDeleteFixedIncome = $("#btnConfirmDeleteFixedIncome");
    const $btnExportExcelFixedIncome = $("#btnExportExcelFixedIncome");
    const $btnDeleteFixedIncome = $("#btnDeleteFixedIncome");
    const incomeSubClassMap = JSON.parse($("#incomeSubClassMap").val() || "{}");
    const maxDepositDayByMonth = JSON.parse($("#maxDepositDayByMonth").val() || "{}");
    const noMaturityIso = $("#noMaturityDate").val();
    const [nmYear, nmMonth, nmDay] = noMaturityIso.split("-").map(Number);
    const NO_MATURITY_DATE = new Date(nmYear, nmMonth - 1, nmDay);
    function ApplyAllowedOptions($select, allowedValues, valueToSelect) {
        $select.find("option").each(function () {
            const allowed = allowedValues.indexOf(this.value) !== -1;
            $(this).prop("disabled", !allowed).prop("hidden", !allowed);
        });
        const target = (valueToSelect != null && allowedValues.indexOf(valueToSelect) !== -1)
            ? valueToSelect
            : allowedValues[0];
        if (target != null) {
            $select.val(target).change();
        }
    }
    function DepositDayValues(monthValue) {
        const maxDay = parseInt(maxDepositDayByMonth[String(monthValue)], 10);
        const values = [];
        for (let day = 1; day <= maxDay; day++) {
            values.push(String(day));
        }
        return values;
    }
    const localizer = {
        PrevText: $("#localizerPrevText").val(),
        NextText: $("#localizerNextText").val(),
        January: $("#localizerJanuary").val(),
        February: $("#localizerFebruary").val(),
        March: $("#localizerMarch").val(),
        April: $("#localizerApril").val(),
        May: $("#localizerMay").val(),
        June: $("#localizerJune").val(),
        July: $("#localizerJuly").val(),
        August: $("#localizerAugust").val(),
        September: $("#localizerSeptember").val(),
        October: $("#localizerOctober").val(),
        November: $("#localizerNovember").val(),
        December: $("#localizerDecember").val(),
        Jan: $("#localizerJan").val(),
        Feb: $("#localizerFeb").val(),
        Mar: $("#localizerMar").val(),
        Apr: $("#localizerApr").val(),
        Jun: $("#localizerJun").val(),
        Jul: $("#localizerJul").val(),
        Aug: $("#localizerAug").val(),
        Sep: $("#localizerSep").val(),
        Oct: $("#localizerOct").val(),
        Nov: $("#localizerNov").val(),
        Dec: $("#localizerDec").val(),
        Sunday: $("#localizerSunday").val(),
        Monday: $("#localizerMonday").val(),
        Tuesday: $("#localizerTuesday").val(),
        Wednesday: $("#localizerWednesday").val(),
        Thursday: $("#localizerThursday").val(),
        Friday: $("#localizerFriday").val(),
        Saturday: $("#localizerSaturday").val(),
        Sun: $("#localizerSun").val(),
        Mon: $("#localizerMon").val(),
        Tue: $("#localizerTue").val(),
        Wed: $("#localizerWed").val(),
        Thu: $("#localizerThu").val(),
        Fri: $("#localizerFri").val(),
        Sat: $("#localizerSat").val(),
        Su: $("#localizerSu").val(),
        Mo: $("#localizerMo").val(),
        Tu: $("#localizerTu").val(),
        We: $("#localizerWe").val(),
        Th: $("#localizerTh").val(),
        Fr: $("#localizerFr").val(),
        Sa: $("#localizerSa").val(),
        YearSuffix: $("#localizerYearSuffix").val(),
        NoMaturityDate: $("#localizerNoMaturityDate").val(),
        Today: $("#localizerToday").val(),
        MaturityDateError: $("#localizerMaturityDateError").val(),
    };
    $.datepicker.setDefaults({
        dateFormat: "yy-mm-dd",
        prevText: localizer.PrevText,
        nextText: localizer.NextText,
        monthNames: [localizer.January, localizer.February, localizer.March, localizer.April, localizer.May, localizer.June, localizer.July, localizer.August, localizer.September, localizer.October, localizer.November, localizer.December],
        monthNamesShort: [localizer.Jan, localizer.Feb, localizer.Mar, localizer.Apr, localizer.May, localizer.Jun, localizer.Jul, localizer.Aug, localizer.Sep, localizer.Oct, localizer.Nov, localizer.Dec],
        dayNames: [localizer.Sunday, localizer.Monday, localizer.Tuesday, localizer.Wednesday, localizer.Thursday, localizer.Friday, localizer.Saturday],
        dayNamesShort: [localizer.Sun, localizer.Mon, localizer.Tue, localizer.Wed, localizer.Thu, localizer.Fri, localizer.Sat],
        dayNamesMin: [localizer.Su, localizer.Mo, localizer.Tu, localizer.We, localizer.Th, localizer.Fr, localizer.Sa],
        showMonthAfterYear: true,
        yearSuffix: localizer.YearSuffix
    });
    $(function () {
        $createFixedIncomeTabs.tabs();
        $editFixedIncomeTabs.tabs();
        $createFixedIncomeMaturityDate.datepicker({
            showButtonPanel: true,
            minDate: 0,
            beforeShow: function (input) {
                setTimeout(function () {
                    let buttonPane = $(input)
                        .datepicker("widget")
                        .find(".ui-datepicker-buttonpane");
                    $("<button>", {
                        text: localizer.NoMaturityDate,
                        click: function () {
                            $.datepicker._clearDate(input);
                            $createFixedIncomeMaturityDate.datepicker("setDate", NO_MATURITY_DATE);
                        }
                    }).appendTo(buttonPane).addClass("ui-datepicker-clear ui-state-default ui-priority-primary ui-corner-all");
                    $("<button>", {
                        text: localizer.Today,
                        click: function () {
                            $.datepicker._clearDate(input);
                            $createFixedIncomeMaturityDate.datepicker("setDate", new Date());
                        }
                    }).appendTo(buttonPane).addClass("ui-datepicker-clear ui-state-default ui-priority-primary ui-corner-all");
                }, 1);
            },
            onChangeMonthYear: function (year, month, instance) {
                setTimeout(function () {
                    let buttonPane = $(instance)
                        .datepicker("widget")
                        .find(".ui-datepicker-buttonpane");
                    $("<button>", {
                        text: localizer.NoMaturityDate,
                        click: function () {
                            $.datepicker._clearDate(instance.input);
                            $createFixedIncomeMaturityDate.datepicker("setDate", NO_MATURITY_DATE);
                        }
                    }).appendTo(buttonPane).addClass("ui-datepicker-clear ui-state-default ui-priority-primary ui-corner-all");
                    $("<button>", {
                        text: localizer.Today,
                        click: function () {
                            $.datepicker._clearDate(instance.input);
                            $createFixedIncomeMaturityDate.datepicker("setDate", new Date());
                        }
                    }).appendTo(buttonPane).addClass("ui-datepicker-clear ui-state-default ui-priority-primary ui-corner-all");
                }, 1);
            }
        });
        $editFixedIncomeMaturityDate.datepicker({
            showButtonPanel: true,
            minDate: 0,
            beforeShow: function (input) {
                setTimeout(function () {
                    let buttonPane = $(input)
                        .datepicker("widget")
                        .find(".ui-datepicker-buttonpane");
                    $("<button>", {
                        text: localizer.NoMaturityDate,
                        click: function () {
                            $.datepicker._clearDate(input);
                            $editFixedIncomeMaturityDate.datepicker("setDate", NO_MATURITY_DATE);
                        }
                    }).appendTo(buttonPane).addClass("ui-datepicker-clear ui-state-default ui-priority-primary ui-corner-all");
                    $("<button>", {
                        text: localizer.Today,
                        click: function () {
                            $.datepicker._clearDate(input);
                            $editFixedIncomeMaturityDate.datepicker("setDate", new Date());
                        }
                    }).appendTo(buttonPane).addClass("ui-datepicker-clear ui-state-default ui-priority-primary ui-corner-all");
                }, 1);
            },
            onChangeMonthYear: function (year, month, instance) {
                setTimeout(function () {
                    let buttonPane = $(instance)
                        .datepicker("widget")
                        .find(".ui-datepicker-buttonpane");
                    $("<button>", {
                        text: localizer.NoMaturityDate,
                        click: function () {
                            $.datepicker._clearDate(instance.input);
                            $editFixedIncomeMaturityDate.datepicker("setDate", NO_MATURITY_DATE);
                        }
                    }).appendTo(buttonPane).addClass("ui-datepicker-clear ui-state-default ui-priority-primary ui-corner-all");
                    $("<button>", {
                        text: localizer.Today,
                        click: function () {
                            $.datepicker._clearDate(instance.input);
                            $editFixedIncomeMaturityDate.datepicker("setDate", new Date());
                        }
                    }).appendTo(buttonPane).addClass("ui-datepicker-clear ui-state-default ui-priority-primary ui-corner-all");
                }, 1);
            }
        });
        $createFixedIncomeMaturityDate.datepicker("setDate", new Date());
        $editFixedIncomeMaturityDate.datepicker("setDate", new Date());
        $("<style> .ui-datepicker-close { display: none; } </style>").appendTo("head");
        $("<style> .ui-datepicker-current { display: none; } </style>").appendTo("head");
    });
    let selectedRowColor = "table-primary";
    $(document).off("rowclick.FixedIncome").on("rowclick.FixedIncome", (e) => {
        let selectedRow = e.detail;
        let selectedRowId = selectedRow.data.Id;
        let $clsGridRow = $(".clsGridRow");
        $clsGridRow.each(function () {
            $(this).removeClass(selectedRowColor);
        });
        $clsGridRow.each(function () {
            if ((String(selectedRowId).valueOf() === String($(this).attr("data-id")).valueOf())) {
                $(this).addClass(selectedRowColor);
            }
        });
    });
    $(document).off("reloadstart.FixedIncome").on("reloadstart.FixedIncome", _ => {
    });
    $(document).off("reloadend.FixedIncome").on("reloadend.FixedIncome", _ => {
    });
    $(document).off("reloadfail.FixedIncome").on("reloadfail.FixedIncome", _ => {
    });
    $(document).off("gridconfigure.FixedIncome").on("gridconfigure.FixedIncome", _ => {
    });
    $(document).off("dblclick.FixedIncome", ".clsGridRow").on("dblclick.FixedIncome", ".clsGridRow", function () {
        $(".clsGridRow").each(function () {
            if ($(this).attr("class").includes(selectedRowColor)) {
                EditFixedIncomeGridRow();
            }
        });
    });
    $gridSearch.off("input").on("input", function () {
        const grid = new MvcGrid(document.querySelector(".mvc-grid"));
        grid.url.searchParams.set("wholeSearch", this.value);
        grid.reload();
    });
    function CreateFormShowFixedIncomeSubClassBySelectedFixedIncomeMainClass(createFixedIncomeMainClass) {
        ApplyAllowedOptions($createFixedIncomeSubClass, incomeSubClassMap[createFixedIncomeMainClass.value] || [], null);
    }
    function CreateFormShowFixedIncomeDepositDayBySelectedFixedIncomeDepositMonth(createFixedIncomeDepositMonth) {
        ApplyAllowedOptions($createFixedIncomeDepositDay, DepositDayValues(createFixedIncomeDepositMonth.value), null);
    }
    function EditFormShowFixedIncomeSubClassBySelectedFixedIncomeMainClass(editFixedIncomeMainClass) {
        ApplyAllowedOptions($editFixedIncomeSubClass, incomeSubClassMap[editFixedIncomeMainClass.value] || [], null);
    }
    function EditFormShowFixedIncomeDepositDayBySelectedFixedIncomeDepositMonth(editFixedIncomeDepositMonth) {
        ApplyAllowedOptions($editFixedIncomeDepositDay, DepositDayValues(editFixedIncomeDepositMonth.value), null);
    }
    function CreateFixedIncome() {
        if (!$formCreateFixedIncome.valid()) {
            return false;
        }
        let mainClass = $createFixedIncomeMainClass.val();
        let subClass = $createFixedIncomeSubClass.val();
        let content = $createFixedIncomeContent.val();
        let amount = $createFixedIncomeAmount.val();
        let depositMonth = $createFixedIncomeDepositMonth.val();
        let depositDay = $createFixedIncomeDepositDay.val();
        let maturityDate = $createFixedIncomeMaturityDate.val();
        let note = $createFixedIncomeNote.val();
        let depositMyAssetProductName = $createFixedIncomeDepositMyAssetProductName.val();
        let paramValue = JSON.stringify({
            MainClass: mainClass,
            SubClass: subClass,
            Content: content,
            Amount: amount,
            DepositMonth: depositMonth,
            DepositDay: depositDay,
            MaturityDate: maturityDate,
            Note: note,
            DepositMyAssetProductName: depositMyAssetProductName
        });
        $.ajax({
            url: "/Notice/CreateFixedIncome",
            type: "POST",
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: "json",
            data: paramValue,
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                if (data.result) {
                    $createFixedIncomeDialogModal.modal("hide");
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
    function EditFixedIncomeGridRow(errorMessageSelectGridRow) {
        let selectedRowId = "";
        $(".clsGridRow").each(function () {
            if ($(this).attr("class").includes(selectedRowColor)) {
                selectedRowId = $(this).attr("data-id");
            }
        });
        if (selectedRowId === "") {
            toastr.error(errorMessageSelectGridRow);
            return false;
        }
        $.ajax({
            url: "/Notice/IsFixedIncomeExists" + "?id=" + selectedRowId,
            type: "POST",
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: "json",
            data: null,
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                if (data.result) {
                    $editFixedIncomeId.val(data.fixedIncome.id);
                    $editFixedIncomeMainClass.val(data.fixedIncome.mainClass).change();
                    ApplyAllowedOptions($editFixedIncomeSubClass, incomeSubClassMap[data.fixedIncome.mainClass] || [], data.fixedIncome.subClass);
                    $editFixedIncomeContent.val(data.fixedIncome.content);
                    $editFixedIncomeAmount.val(data.fixedIncome.amount);
                    $editFixedIncomeDepositMonth.val(data.fixedIncome.depositMonth.toString()).change();
                    ApplyAllowedOptions($editFixedIncomeDepositDay, DepositDayValues(data.fixedIncome.depositMonth.toString()), data.fixedIncome.depositDay.toString());
                    $editFixedIncomeMaturityDate.val(data.fixedIncome.maturityDate);
                    $editFixedIncomeNote.val(data.fixedIncome.note);
                    $editFixedIncomeDepositMyAssetProductName.val(data.fixedIncome.depositMyAssetProductName).change();
                    $editFixedIncomeUnpunctuality.prop("checked", data.fixedIncome.unpunctuality);
                    $editFixedIncomeDialogModal.modal({
                        keyboard: false,
                        backdrop: "static"
                    });
                    $editFixedIncomeDialogModal.modal("toggle");
                    $editFixedIncomeDialogModal.modal("show");
                }
                else {
                    toastr.error(data.error);
                }
            }
        });
    }
    function UpdateFixedIncome() {
        if (!$formEditFixedIncome.valid()) {
            return false;
        }
        let id = $editFixedIncomeId.val();
        let mainClass = $editFixedIncomeMainClass.val();
        let subClass = $editFixedIncomeSubClass.val();
        let content = $editFixedIncomeContent.val();
        let amount = $editFixedIncomeAmount.val();
        let depositMonth = $editFixedIncomeDepositMonth.val();
        let depositDay = $editFixedIncomeDepositDay.val();
        let maturityDate = $editFixedIncomeMaturityDate.val();
        let note = $editFixedIncomeNote.val();
        let depositMyAssetProductName = $editFixedIncomeDepositMyAssetProductName.val();
        let unpunctuality = $editFixedIncomeUnpunctuality.is(":checked");
        let paramValue = JSON.stringify({
            Id: id,
            MainClass: mainClass,
            SubClass: subClass,
            Content: content,
            Amount: amount,
            DepositMonth: depositMonth,
            DepositDay: depositDay,
            MaturityDate: maturityDate,
            Note: note,
            DepositMyAssetProductName: depositMyAssetProductName,
            Unpunctuality: unpunctuality
        });
        $.ajax({
            url: "/Notice/UpdateFixedIncome",
            type: "POST",
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: "json",
            data: paramValue,
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                if (data.result) {
                    $editFixedIncomeDialogModal.modal("hide");
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
    function ConfirmDeleteFixedIncome(errorMessageSelectGridRow) {
        let selectedRowId = "";
        $(".clsGridRow").each(function () {
            if ($(this).attr("class").includes(selectedRowColor)) {
                selectedRowId = $(this).attr("data-id");
            }
        });
        if (selectedRowId === "") {
            toastr.error(errorMessageSelectGridRow);
            return false;
        }
        $confirmDeleteFixedIncomeDialogModal.modal({
            keyboard: false,
            backdrop: "static"
        });
        $confirmDeleteFixedIncomeDialogModal.modal("toggle");
        $confirmDeleteFixedIncomeDialogModal.modal("show");
    }
    function DeleteFixedIncome(errorMessageSelectGridRow) {
        let selectedRowId = "";
        $(".clsGridRow").each(function () {
            if ($(this).attr("class").includes(selectedRowColor)) {
                selectedRowId = $(this).attr("data-id");
            }
        });
        if (selectedRowId === "") {
            toastr.error(errorMessageSelectGridRow);
            return false;
        }
        $.ajax({
            url: "/Notice/IsFixedIncomeExists" + "?id=" + selectedRowId,
            type: "POST",
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: "json",
            data: null,
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                if (data.result) {
                    let paramValue = JSON.stringify({
                        Id: data.fixedIncome.id
                    });
                    $.ajax({
                        url: "/Notice/DeleteFixedIncome",
                        type: "POST",
                        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
                        dataType: "json",
                        data: paramValue,
                        contentType: "application/json; charset=utf-8",
                        success: function (data) {
                            if (data.result) {
                                $confirmDeleteFixedIncomeDialogModal.modal("hide");
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
    function ExportExcelFixedIncome() {
        let form = document.createElement("form");
        let element1 = document.createElement("input");
        let element2 = document.createElement("input");
        form.method = "POST";
        form.action = "/Notice/ExportExcelFixedIncome";
        element1.name = "__RequestVerificationToken";
        element1.value = $__RequestVerificationToken.val();
        form.appendChild(element1);
        element2.name = "fileName";
        element2.value = "FixedIncome";
        form.appendChild(element2);
        document.body.appendChild(form);
        form.submit();
    }
    function ChangeCreateFixedIncomeAmountLabel(productName) {
        $.ajax({
            url: "/Notice/GetFixedIncomeAmountLabel" + "?productName=" + productName,
            type: "POST",
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: "json",
            data: null,
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                if (data.result) {
                    $labelCreateFixedIncomeAmount.text(data.label);
                }
                else {
                    $labelCreateFixedIncomeAmount.text(data.label);
                }
            },
            error: function () {
                toastr.error(failedToLoadAmountLabelMessage);
            }
        });
    }
    function ChangeEditFixedIncomeAmountLabel(productName) {
        $.ajax({
            url: "/Notice/GetFixedIncomeAmountLabel" + "?productName=" + productName,
            type: "POST",
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: "json",
            data: null,
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                if (data.result) {
                    $labelEditFixedIncomeAmount.text(data.label);
                }
                else {
                    $labelEditFixedIncomeAmount.text(data.label);
                }
            },
            error: function () {
                toastr.error(failedToLoadAmountLabelMessage);
            }
        });
    }
    $btnEditFixedIncomeGridRow.off("click").on("click", function () {
        EditFixedIncomeGridRow($(this).attr("data-errorMessageSelectGridRow"));
    });
    $btnConfirmDeleteFixedIncome.off("click").on("click", function () {
        ConfirmDeleteFixedIncome($(this).attr("data-errorMessageSelectGridRow"));
    });
    $btnExportExcelFixedIncome.off("click").on("click", function () {
        ExportExcelFixedIncome();
    });
    $btnDeleteFixedIncome.off("click").on("click", function () {
        DeleteFixedIncome($(this).attr("data-errorMessageSelectGridRow"));
    });
    $formCreateFixedIncome.off("submit").on("submit", function () {
        return CreateFixedIncome();
    });
    $formEditFixedIncome.off("submit").on("submit", function () {
        return UpdateFixedIncome();
    });
    $createFixedIncomeMainClass.off("change").on("change", function () {
        return CreateFormShowFixedIncomeSubClassBySelectedFixedIncomeMainClass(this);
    });
    $createFixedIncomeDepositMonth.off("change").on("change", function () {
        return CreateFormShowFixedIncomeDepositDayBySelectedFixedIncomeDepositMonth(this);
    });
    $editFixedIncomeMainClass.off("change").on("change", function () {
        return EditFormShowFixedIncomeSubClassBySelectedFixedIncomeMainClass(this);
    });
    $editFixedIncomeDepositMonth.off("change").on("change", function () {
        return EditFormShowFixedIncomeDepositDayBySelectedFixedIncomeDepositMonth(this);
    });
    $createFixedIncomeDepositMyAssetProductName.off("change").on("change", function () {
        ChangeCreateFixedIncomeAmountLabel(this.value);
    });
    $editFixedIncomeDepositMyAssetProductName.off("change").on("change", function () {
        ChangeEditFixedIncomeAmountLabel(this.value);
    });
    $(function () {
        ChangeCreateFixedIncomeAmountLabel($createFixedIncomeDepositMyAssetProductName.find("option:selected").val());
    });
})();
