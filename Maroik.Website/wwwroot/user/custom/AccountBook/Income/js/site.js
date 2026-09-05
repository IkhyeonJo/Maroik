"use strict";
(function () {
    const $createIncomeTabs = $('#createIncomeTabs');
    const $editIncomeTabs = $('#editIncomeTabs');
    const $createIncomeDate = $('#createIncomeDate');
    const $editIncomeDate = $('#editIncomeDate');
    const $gridSearch = $('#gridSearch');
    const $createIncomeSubClass = $('#createIncomeSubClass');
    const $editIncomeSubClass = $('#editIncomeSubClass');
    const $editIncomeDialogModal = $('#editIncomeDialogModal');
    const $formCreateIncome = $('#formCreateIncome');
    const $__RequestVerificationToken = $('input[name="__RequestVerificationToken"]');
    const $createIncomeMainClass = $('#createIncomeMainClass');
    const $createIncomeContent = $('#createIncomeContent');
    const $createIncomeAmount = $('#createIncomeAmount');
    const $createIncomeHour = $('#createIncomeHour');
    const $createIncomeMinute = $('#createIncomeMinute');
    const $createIncomeSecond = $('#createIncomeSecond');
    const $createIncomeDepositMyAssetProductName = $('#createIncomeDepositMyAssetProductName');
    const $createIncomeNote = $('#createIncomeNote');
    const $createIncomeDialogModal = $('#createIncomeDialogModal');
    const $editIncomeId = $('#editIncomeId');
    const $editIncomeMainClass = $('#editIncomeMainClass');
    const $editIncomeContent = $('#editIncomeContent');
    const $editIncomeAmount = $('#editIncomeAmount');
    const $editIncomeHour = $('#editIncomeHour');
    const $editIncomeMinute = $('#editIncomeMinute');
    const $editIncomeSecond = $('#editIncomeSecond');
    const $editIncomeDepositMyAssetProductName = $('#editIncomeDepositMyAssetProductName');
    const $editIncomeNote = $('#editIncomeNote');
    const $formEditIncome = $('#formEditIncome');
    const $confirmDeleteIncomeDialogModal = $('#confirmDeleteIncomeDialogModal');
    const $labelCreateIncomeAmount = $('#labelCreateIncomeAmount');
    const $labelEditIncomeAmount = $('#labelEditIncomeAmount');
    const $btnEditIncomeGridRow = $('#btnEditIncomeGridRow');
    const $btnConfirmDeleteIncome = $('#btnConfirmDeleteIncome');
    const $btnExportExcelIncome = $('#btnExportExcelIncome');
    const $btnDeleteIncome = $('#btnDeleteIncome');
    const incomeSubClassMap = JSON.parse($('#incomeSubClassMap').val() || '{}');
    function ApplyAllowedOptions($select, allowedValues, valueToSelect) {
        $select.find('option').each(function () {
            const allowed = allowedValues.indexOf(this.value) !== -1;
            $(this).prop('disabled', !allowed).prop('hidden', !allowed);
        });
        const target = (valueToSelect != null && allowedValues.indexOf(valueToSelect) !== -1)
            ? valueToSelect
            : allowedValues[0];
        if (target != null) {
            $select.val(target).change();
        }
    }
    const localizer = {
        PrevText: $('#localizerPrevText').val(),
        NextText: $('#localizerNextText').val(),
        January: $('#localizerJanuary').val(),
        February: $('#localizerFebruary').val(),
        March: $('#localizerMarch').val(),
        April: $('#localizerApril').val(),
        May: $('#localizerMay').val(),
        June: $('#localizerJune').val(),
        July: $('#localizerJuly').val(),
        August: $('#localizerAugust').val(),
        September: $('#localizerSeptember').val(),
        October: $('#localizerOctober').val(),
        November: $('#localizerNovember').val(),
        December: $('#localizerDecember').val(),
        Jan: $('#localizerJan').val(),
        Feb: $('#localizerFeb').val(),
        Mar: $('#localizerMar').val(),
        Apr: $('#localizerApr').val(),
        Jun: $('#localizerJun').val(),
        Jul: $('#localizerJul').val(),
        Aug: $('#localizerAug').val(),
        Sep: $('#localizerSep').val(),
        Oct: $('#localizerOct').val(),
        Nov: $('#localizerNov').val(),
        Dec: $('#localizerDec').val(),
        Sunday: $('#localizerSunday').val(),
        Monday: $('#localizerMonday').val(),
        Tuesday: $('#localizerTuesday').val(),
        Wednesday: $('#localizerWednesday').val(),
        Thursday: $('#localizerThursday').val(),
        Friday: $('#localizerFriday').val(),
        Saturday: $('#localizerSaturday').val(),
        Sun: $('#localizerSun').val(),
        Mon: $('#localizerMon').val(),
        Tue: $('#localizerTue').val(),
        Wed: $('#localizerWed').val(),
        Thu: $('#localizerThu').val(),
        Fri: $('#localizerFri').val(),
        Sat: $('#localizerSat').val(),
        Su: $('#localizerSu').val(),
        Mo: $('#localizerMo').val(),
        Tu: $('#localizerTu').val(),
        We: $('#localizerWe').val(),
        Th: $('#localizerTh').val(),
        Fr: $('#localizerFr').val(),
        Sa: $('#localizerSa').val(),
        YearSuffix: $('#localizerYearSuffix').val()
    };
    $.datepicker.setDefaults({
        dateFormat: 'yy-mm-dd',
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
        $createIncomeTabs.tabs();
        $editIncomeTabs.tabs();
        $createIncomeDate.datepicker({
            showButtonPanel: true,
            beforeShow: function () {
                setTimeout(function () {
                }, 1);
            },
            onChangeMonthYear: function () {
                setTimeout(function () {
                }, 1);
            }
        });
        $editIncomeDate.datepicker({
            showButtonPanel: true,
            beforeShow: function () {
                setTimeout(function () {
                }, 1);
            },
            onChangeMonthYear: function () {
                setTimeout(function () {
                }, 1);
            }
        });
        $('<style> .ui-datepicker-close { display: none; } </style>').appendTo("head");
        $('<style> .ui-datepicker-current { display: none; } </style>').appendTo("head");
    });
    let selectedRowColor = "table-primary";
    $(document).off('rowclick.Income').on('rowclick.Income', (e) => {
        let selectedRow = e.detail;
        let selectedRowId = selectedRow.data.Id;
        let $clsGridRow = $('.clsGridRow');
        $clsGridRow.each(function () {
            $(this).removeClass(selectedRowColor);
        });
        $clsGridRow.each(function () {
            if ((String(selectedRowId).valueOf() === String($(this).attr('data-id')).valueOf())) {
                $(this).addClass(selectedRowColor);
            }
        });
    });
    $(document).off('reloadstart.Income').on('reloadstart.Income', _ => {
    });
    $(document).off('reloadend.Income').on('reloadend.Income', _ => {
    });
    $(document).off('reloadfail.Income').on('reloadfail.Income', _ => {
    });
    $(document).off('gridconfigure.Income').on('gridconfigure.Income', _ => {
    });
    $(document).off('dblclick.Income', '.clsGridRow').on('dblclick.Income', '.clsGridRow', function () {
        $('.clsGridRow').each(function () {
            if ($(this).attr("class").includes(selectedRowColor)) {
                EditIncomeGridRow();
            }
        });
    });
    $gridSearch.off('input').on('input', function () {
        const grid = new MvcGrid(document.querySelector('.mvc-grid'));
        grid.url.searchParams.set('wholeSearch', this.value);
        grid.reload();
    });
    function CreateFormShowIncomeSubClassBySelectedIncomeMainClass(createIncomeMainClass) {
        ApplyAllowedOptions($createIncomeSubClass, incomeSubClassMap[createIncomeMainClass.value] || [], null);
    }
    function EditFormShowIncomeSubClassBySelectedIncomeMainClass(editIncomeMainClass) {
        ApplyAllowedOptions($editIncomeSubClass, incomeSubClassMap[editIncomeMainClass.value] || [], null);
    }
    function CreateIncome() {
        if (!$formCreateIncome.valid()) {
            return false;
        }
        let mainClass = $createIncomeMainClass.val();
        let subClass = $createIncomeSubClass.val();
        let content = $createIncomeContent.val();
        let amount = $createIncomeAmount.val();
        let year = parseInt($createIncomeDate.val().substring(0, 4));
        let month = parseInt($createIncomeDate.val().substring(5, 7));
        let day = parseInt($createIncomeDate.val().substring(8, 10));
        let hour = parseInt($createIncomeHour.val());
        let minute = parseInt($createIncomeMinute.val());
        let second = parseInt($createIncomeSecond.val());
        let created = new Date(year, month - 1, day, hour, minute, second).toISOString();
        let depositMyAssetProductName = $createIncomeDepositMyAssetProductName.val();
        let note = $createIncomeNote.val();
        let paramValue = JSON.stringify({
            MainClass: mainClass,
            SubClass: subClass,
            Content: content,
            Amount: amount,
            Created: created,
            DepositMyAssetProductName: depositMyAssetProductName,
            Note: note
        });
        $.ajax({
            url: '/AccountBook/CreateIncome',
            type: 'POST',
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: 'json',
            data: paramValue,
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                if (data.result) {
                    $createIncomeDialogModal.modal('hide');
                    const grid = new MvcGrid(document.querySelector('.mvc-grid'));
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
    function EditIncomeGridRow(errorMessageSelectGridRow) {
        let selectedRowId = "";
        $('.clsGridRow').each(function () {
            if ($(this).attr("class").includes(selectedRowColor)) {
                selectedRowId = $(this).attr("data-id");
            }
        });
        if (selectedRowId === "") {
            toastr.error(errorMessageSelectGridRow);
            return false;
        }
        $.ajax({
            url: '/AccountBook/IsIncomeExists' + '?id=' + selectedRowId,
            type: 'POST',
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: 'json',
            data: null,
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                if (data.result) {
                    $editIncomeId.val(data.income.id);
                    $editIncomeMainClass.val(data.income.mainClass).change();
                    ApplyAllowedOptions($editIncomeSubClass, incomeSubClassMap[data.income.mainClass] || [], data.income.subClass);
                    $editIncomeContent.val(data.income.content);
                    $editIncomeAmount.val(data.income.amount);
                    $editIncomeDate.val(data.income.created.split('T')[0]);
                    $editIncomeHour.val(parseInt(data.income.created.split('T')[1].substring(0, 2))).change();
                    $editIncomeMinute.val(parseInt(data.income.created.split('T')[1].substring(3, 5))).change();
                    $editIncomeSecond.val(parseInt(data.income.created.split('T')[1].substring(6, 8))).change();
                    $editIncomeDepositMyAssetProductName.val(data.income.depositMyAssetProductName).change();
                    $editIncomeNote.val(data.income.note);
                    $editIncomeDialogModal.modal({
                        keyboard: false,
                        backdrop: "static"
                    });
                    $editIncomeDialogModal.modal('toggle');
                    $editIncomeDialogModal.modal('show');
                }
                else {
                    toastr.error(data.error);
                }
            }
        });
    }
    function UpdateIncome() {
        if (!$formEditIncome.valid()) {
            return false;
        }
        let id = $editIncomeId.val();
        let mainClass = $editIncomeMainClass.val();
        let subClass = $editIncomeSubClass.val();
        let content = $editIncomeContent.val();
        let amount = $editIncomeAmount.val();
        let year = parseInt($editIncomeDate.val().substring(0, 4));
        let month = parseInt($editIncomeDate.val().substring(5, 7));
        let day = parseInt($editIncomeDate.val().substring(8, 10));
        let hour = parseInt($editIncomeHour.val());
        let minute = parseInt($editIncomeMinute.val());
        let second = parseInt($editIncomeSecond.val());
        let created = new Date(year, month - 1, day, hour, minute, second).toISOString();
        let depositMyAssetProductName = $editIncomeDepositMyAssetProductName.val();
        let note = $editIncomeNote.val();
        let paramValue = JSON.stringify({
            Id: id,
            MainClass: mainClass,
            SubClass: subClass,
            Content: content,
            Amount: amount,
            Created: created,
            DepositMyAssetProductName: depositMyAssetProductName,
            Note: note
        });
        $.ajax({
            url: '/AccountBook/UpdateIncome',
            type: 'POST',
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: 'json',
            data: paramValue,
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                if (data.result) {
                    $editIncomeDialogModal.modal('hide');
                    const grid = new MvcGrid(document.querySelector('.mvc-grid'));
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
    function ConfirmDeleteIncome(errorMessageSelectGridRow) {
        let selectedRowId = "";
        $('.clsGridRow').each(function () {
            if ($(this).attr("class").includes(selectedRowColor)) {
                selectedRowId = $(this).attr("data-id");
            }
        });
        if (selectedRowId === "") {
            toastr.error(errorMessageSelectGridRow);
            return false;
        }
        $confirmDeleteIncomeDialogModal.modal({
            keyboard: false,
            backdrop: "static"
        });
        $confirmDeleteIncomeDialogModal.modal('toggle');
        $confirmDeleteIncomeDialogModal.modal('show');
    }
    function DeleteIncome(errorMessageSelectGridRow) {
        let selectedRowId = "";
        $('.clsGridRow').each(function () {
            if ($(this).attr("class").includes(selectedRowColor)) {
                selectedRowId = $(this).attr("data-id");
            }
        });
        if (selectedRowId === "") {
            toastr.error(errorMessageSelectGridRow);
            return false;
        }
        $.ajax({
            url: '/AccountBook/IsIncomeExists' + '?id=' + selectedRowId,
            type: 'POST',
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: 'json',
            data: null,
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                if (data.result) {
                    let paramValue = JSON.stringify({
                        Id: data.income.id
                    });
                    $.ajax({
                        url: '/AccountBook/DeleteIncome',
                        type: 'POST',
                        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
                        dataType: 'json',
                        data: paramValue,
                        contentType: 'application/json; charset=utf-8',
                        success: function (data) {
                            if (data.result) {
                                $confirmDeleteIncomeDialogModal.modal('hide');
                                const grid = new MvcGrid(document.querySelector('.mvc-grid'));
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
    function ExportExcelIncome() {
        let form = document.createElement("form");
        let element1 = document.createElement("input");
        let element2 = document.createElement("input");
        form.method = "POST";
        form.action = "/AccountBook/ExportExcelIncome";
        element1.name = "__RequestVerificationToken";
        element1.value = $__RequestVerificationToken.val();
        form.appendChild(element1);
        element2.name = "fileName";
        element2.value = "Income";
        form.appendChild(element2);
        document.body.appendChild(form);
        form.submit();
    }
    function ChangeCreateIncomeAmountLabel(productName) {
        $.ajax({
            url: '/AccountBook/GetIncomeAmountLabel' + '?productName=' + productName,
            type: 'POST',
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: 'json',
            data: null,
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                if (data.result) {
                    $labelCreateIncomeAmount.text(data.label);
                }
                else {
                    $labelCreateIncomeAmount.text(data.label);
                }
            }
        });
    }
    function ChangeEditIncomeAmountLabel(productName) {
        $.ajax({
            url: '/AccountBook/GetIncomeAmountLabel' + '?productName=' + productName,
            type: 'POST',
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: 'json',
            data: null,
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                if (data.result) {
                    $labelEditIncomeAmount.text(data.label);
                }
                else {
                    $labelEditIncomeAmount.text(data.label);
                }
            }
        });
    }
    $btnEditIncomeGridRow.off('click').on('click', function () {
        EditIncomeGridRow($(this).attr("data-errorMessageSelectGridRow"));
    });
    $btnConfirmDeleteIncome.off('click').on('click', function () {
        ConfirmDeleteIncome($(this).attr("data-errorMessageSelectGridRow"));
    });
    $btnExportExcelIncome.off('click').on('click', function () {
        ExportExcelIncome();
    });
    $btnDeleteIncome.off('click').on('click', function () {
        DeleteIncome($(this).attr("data-errorMessageSelectGridRow"));
    });
    $formCreateIncome.off('submit').on('submit', function () {
        return CreateIncome();
    });
    $formEditIncome.off('submit').on('submit', function () {
        return UpdateIncome();
    });
    $createIncomeMainClass.off('change').on('change', function () {
        CreateFormShowIncomeSubClassBySelectedIncomeMainClass(this);
    });
    $editIncomeMainClass.off('change').on('change', function () {
        EditFormShowIncomeSubClassBySelectedIncomeMainClass(this);
    });
    $createIncomeDepositMyAssetProductName.off('change').on('change', function () {
        ChangeCreateIncomeAmountLabel(this.value);
    });
    $editIncomeDepositMyAssetProductName.off('change').on('change', function () {
        ChangeEditIncomeAmountLabel(this.value);
    });
    $(function () {
        ChangeCreateIncomeAmountLabel($createIncomeDepositMyAssetProductName.find('option:selected').val());
    });
})();
//# sourceMappingURL=site.js.map