"use strict";
(function () {
    const $createExpenditureDate = $('#createExpenditureDate');
    const $createExpenditureTabs = $('#createExpenditureTabs');
    const $editExpenditureTabs = $('#editExpenditureTabs');
    const $editExpenditureDate = $('#editExpenditureDate');
    const $gridSearch = $('#gridSearch');
    const $createExpenditureSubClass = $('#createExpenditureSubClass');
    const $editExpenditureSubClass = $('#editExpenditureSubClass');
    const $divCreateExpenditureMyDepositAsset = $("#divCreateExpenditureMyDepositAsset");
    const $createExpenditureMainClass = $('#createExpenditureMainClass');
    const $formCreateExpenditure = $('#formCreateExpenditure');
    const $createExpenditureContent = $('#createExpenditureContent');
    const $createExpenditureAmount = $('#createExpenditureAmount');
    const $createExpenditureHour = $('#createExpenditureHour');
    const $createExpenditureMinute = $('#createExpenditureMinute');
    const $createExpenditureSecond = $('#createExpenditureSecond');
    const $createExpenditurePaymentMethod = $('#createExpenditurePaymentMethod');
    const $createExpenditureNote = $('#createExpenditureNote');
    const $createExpenditureMyDepositAsset = $('#createExpenditureMyDepositAsset');
    const $__RequestVerificationToken = $('input[name="__RequestVerificationToken"]');
    const $createExpenditureDialogModal = $('#createExpenditureDialogModal');
    const $editExpenditureId = $('#editExpenditureId');
    const $editExpenditureMainClass = $('#editExpenditureMainClass');
    const $divEditExpenditureMyDepositAsset = $("#divEditExpenditureMyDepositAsset");
    const $editExpenditureContent = $('#editExpenditureContent');
    const $editExpenditureAmount = $('#editExpenditureAmount');
    const $editExpenditureHour = $('#editExpenditureHour');
    const $editExpenditureMinute = $('#editExpenditureMinute');
    const $editExpenditureSecond = $('#editExpenditureSecond');
    const $editExpenditurePaymentMethod = $('#editExpenditurePaymentMethod');
    const $editExpenditureNote = $('#editExpenditureNote');
    const $editExpenditureMyDepositAsset = $('#editExpenditureMyDepositAsset');
    const $editExpenditureDialogModal = $('#editExpenditureDialogModal');
    const $formEditExpenditure = $('#formEditExpenditure');
    const $confirmDeleteExpenditureDialogModal = $('#confirmDeleteExpenditureDialogModal');
    const $btnDeleteExpenditure = $('#btnDeleteExpenditure');
    const $btnExportExcelExpenditure = $('#btnExportExcelExpenditure');
    const $btnConfirmDeleteExpenditure = $('#btnConfirmDeleteExpenditure');
    const $btnEditExpenditureGridRow = $('#btnEditExpenditureGridRow');
    const $labelCreateExpenditureAmount = $('#labelCreateExpenditureAmount');
    const $labelEditExpenditureAmount = $('#labelEditExpenditureAmount');
    // Server-published expenditure taxonomy (Expenditure.cshtml). Authoritative in
    // ExpenditureClassPolicy; mirrored here for form UX only, the server re-validates on save.
    const expenditureSubClassMap = JSON.parse($('#expenditureSubClassMap').val() || '{}');
    const expenditureDepositAssetSubClasses = JSON.parse($('#expenditureDepositAssetSubClasses').val() || '[]');
    // Enables/shows only the options in allowedValues (disabling+hiding the rest) and selects
    // valueToSelect when it is allowed, otherwise the first allowed option.
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
    // The "deposit asset" field is only relevant for transfer-type sub-classes.
    function ToggleCreateExpenditureMyDepositAsset(subClassValue) {
        expenditureDepositAssetSubClasses.indexOf(subClassValue) !== -1
            ? $divCreateExpenditureMyDepositAsset.show()
            : $divCreateExpenditureMyDepositAsset.hide();
    }
    function ToggleEditExpenditureMyDepositAsset(subClassValue) {
        expenditureDepositAssetSubClasses.indexOf(subClassValue) !== -1
            ? $divEditExpenditureMyDepositAsset.show()
            : $divEditExpenditureMyDepositAsset.hide();
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
    const currentYear = $createExpenditureDate.val().split('-')[0];
    const currentMonth = $createExpenditureDate.val().split('-')[1];
    const currentDay = $createExpenditureDate.val().split('-')[2];
    $(function () {
        $createExpenditureTabs.tabs();
        $editExpenditureTabs.tabs();
        $createExpenditureDate.datepicker({
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
        $editExpenditureDate.datepicker({
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
    $(document).off('rowclick.Expenditure').on('rowclick.Expenditure', (e) => {
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
    $(document).off('reloadstart.Expenditure').on('reloadstart.Expenditure', _ => {
    });
    $(document).off('reloadend.Expenditure').on('reloadend.Expenditure', _ => {
    });
    $(document).off('reloadfail.Expenditure').on('reloadfail.Expenditure', _ => {
    });
    $(document).off('gridconfigure.Expenditure').on('gridconfigure.Expenditure', _ => {
    });
    $(document).off('dblclick.Expenditure', '.clsGridRow').on('dblclick.Expenditure', '.clsGridRow', function () {
        $('.clsGridRow').each(function () {
            if ($(this).attr("class").includes(selectedRowColor)) {
                EditExpenditureGridRow();
            }
        });
    });
    $gridSearch.off('input').on('input', function () {
        const grid = new MvcGrid(document.querySelector('.mvc-grid'));
        grid.url.searchParams.set("wholeSearch", this.value);
        grid.reload();
    });
    function CreateFormShowExpenditureSubClassBySelectedExpenditureMainClass(createExpenditureMainClass) {
        ApplyAllowedOptions($createExpenditureSubClass, expenditureSubClassMap[createExpenditureMainClass.value] || [], null);
        ToggleCreateExpenditureMyDepositAsset($createExpenditureSubClass.val());
    }
    function CreateFormShowExpenditureDivCreateExpenditureMyDepositAssetBySelectedExpenditureSubClass(createExpenditureSubClass) {
        ToggleCreateExpenditureMyDepositAsset(createExpenditureSubClass.value);
    }
    function CreateExpenditure() {
        if (!$formCreateExpenditure.valid()) {
            return false;
        }
        let mainClass = $createExpenditureMainClass.val();
        let subClass = $createExpenditureSubClass.val();
        let content = $createExpenditureContent.val();
        let amount = $createExpenditureAmount.val();
        let year = parseInt($createExpenditureDate.val().substring(0, 4));
        let month = parseInt($createExpenditureDate.val().substring(5, 7));
        let day = parseInt($createExpenditureDate.val().substring(8, 10));
        let hour = parseInt($createExpenditureHour.val());
        let minute = parseInt($createExpenditureMinute.val());
        let second = parseInt($createExpenditureSecond.val());
        let created = new Date(year, month - 1, day, hour, minute, second).toISOString();
        let paymentMethod = $createExpenditurePaymentMethod.val();
        let note = $createExpenditureNote.val();
        let myDepositAsset = $createExpenditureMyDepositAsset.val();
        let paramValue = JSON.stringify({
            MainClass: mainClass,
            SubClass: subClass,
            Content: content,
            Amount: amount,
            Created: created,
            PaymentMethod: paymentMethod,
            Note: note,
            MyDepositAsset: myDepositAsset
        });
        $.ajax({
            url: '/AccountBook/CreateExpenditure',
            type: 'POST',
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: 'json',
            data: paramValue,
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                if (data.result) {
                    $createExpenditureDialogModal.modal('hide');
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
    function EditExpenditureGridRow(errorMessageSelectGridRow) {
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
            url: '/AccountBook/IsExpenditureExists' + '?id=' + selectedRowId,
            type: 'POST',
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: 'json',
            data: null,
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                if (data.result) {
                    $editExpenditureId.val(data.expenditure.id);
                    $editExpenditureMainClass.val(data.expenditure.mainClass).change();
                    ApplyAllowedOptions($editExpenditureSubClass, expenditureSubClassMap[data.expenditure.mainClass] || [], data.expenditure.subClass);
                    ToggleEditExpenditureMyDepositAsset(data.expenditure.subClass);
                    $editExpenditureContent.val(data.expenditure.content);
                    $editExpenditureAmount.val(data.expenditure.amount);
                    $editExpenditureDate.val(data.expenditure.created.split('T')[0]);
                    $editExpenditureHour.val(parseInt(data.expenditure.created.split('T')[1].substring(0, 2))).change();
                    $editExpenditureMinute.val(parseInt(data.expenditure.created.split('T')[1].substring(3, 5))).change();
                    $editExpenditureSecond.val(parseInt(data.expenditure.created.split('T')[1].substring(6, 8))).change();
                    $editExpenditurePaymentMethod.val(data.expenditure.paymentMethod).change();
                    $editExpenditureNote.val(data.expenditure.note);
                    if (data.expenditure.myDepositAsset) {
                        $editExpenditureMyDepositAsset.val(data.expenditure.myDepositAsset).change();
                    }
                    else {
                        $editExpenditureMyDepositAsset.val(data.expenditure.paymentMethod).change();
                    }
                    $editExpenditureDialogModal.modal({
                        keyboard: false,
                        backdrop: "static"
                    });
                    $editExpenditureDialogModal.modal('toggle');
                    $editExpenditureDialogModal.modal('show');
                }
                else {
                    toastr.error(data.error);
                }
            }
        });
    }
    function EditFormShowExpenditureSubClassBySelectedExpenditureMainClass(editExpenditureMainClass) {
        ApplyAllowedOptions($editExpenditureSubClass, expenditureSubClassMap[editExpenditureMainClass.value] || [], null);
        ToggleEditExpenditureMyDepositAsset($editExpenditureSubClass.val());
    }
    function EditFormShowExpenditureDivCreateExpenditureMyDepositAssetBySelectedExpenditureSubClass(editExpenditureSubClass) {
        ToggleEditExpenditureMyDepositAsset(editExpenditureSubClass.value);
    }
    function UpdateExpenditure() {
        if (!$formEditExpenditure.valid()) {
            return false;
        }
        let id = $editExpenditureId.val();
        let mainClass = $editExpenditureMainClass.val();
        let subClass = $editExpenditureSubClass.val();
        let content = $editExpenditureContent.val();
        let amount = $editExpenditureAmount.val();
        let year = parseInt($editExpenditureDate.val().substring(0, 4));
        let month = parseInt($editExpenditureDate.val().substring(5, 7));
        let day = parseInt($editExpenditureDate.val().substring(8, 10));
        let hour = parseInt($editExpenditureHour.val());
        let minute = parseInt($editExpenditureMinute.val());
        let second = parseInt($editExpenditureSecond.val());
        let created = new Date(year, month - 1, day, hour, minute, second).toISOString();
        let paymentMethod = $editExpenditurePaymentMethod.val();
        let note = $editExpenditureNote.val();
        let myDepositAsset = $editExpenditureMyDepositAsset.val();
        let paramValue = JSON.stringify({
            Id: id,
            MainClass: mainClass,
            SubClass: subClass,
            Content: content,
            Amount: amount,
            Created: created,
            PaymentMethod: paymentMethod,
            Note: note,
            MyDepositAsset: myDepositAsset
        });
        $.ajax({
            url: '/AccountBook/UpdateExpenditure',
            type: 'POST',
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: 'json',
            data: paramValue,
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                if (data.result) {
                    $editExpenditureDialogModal.modal('hide');
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
    function ConfirmDeleteExpenditure(errorMessageSelectGridRow) {
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
        $confirmDeleteExpenditureDialogModal.modal({
            keyboard: false,
            backdrop: "static"
        });
        $confirmDeleteExpenditureDialogModal.modal('toggle');
        $confirmDeleteExpenditureDialogModal.modal('show');
    }
    function DeleteExpenditure(errorMessageSelectGridRow) {
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
            url: '/AccountBook/IsExpenditureExists' + '?id=' + selectedRowId,
            type: 'POST',
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: 'json',
            data: null,
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                if (data.result) {
                    let paramValue = JSON.stringify({
                        Id: data.expenditure.id
                    });
                    $.ajax({
                        url: '/AccountBook/DeleteExpenditure',
                        type: 'POST',
                        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
                        dataType: 'json',
                        data: paramValue,
                        contentType: 'application/json; charset=utf-8',
                        success: function (data) {
                            if (data.result) {
                                $confirmDeleteExpenditureDialogModal.modal('hide');
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
    function ExportExcelExpenditure() {
        let form = document.createElement("form");
        let element1 = document.createElement("input");
        let element2 = document.createElement("input");
        form.method = "POST";
        form.action = "/AccountBook/ExportExcelExpenditure";
        element1.name = "__RequestVerificationToken";
        element1.value = $__RequestVerificationToken.val();
        form.appendChild(element1);
        element2.name = "fileName";
        element2.value = "Expenditure";
        form.appendChild(element2);
        document.body.appendChild(form);
        form.submit();
    }
    function ChangeCreateExpenditureAmountLabel(productName) {
        $.ajax({
            url: '/AccountBook/GetExpenditureAmountLabel' + '?productName=' + productName,
            type: 'POST',
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: 'json',
            data: null,
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                if (data.result) {
                    $labelCreateExpenditureAmount.text(data.label);
                }
                else {
                    $labelCreateExpenditureAmount.text(data.label);
                }
            }
        });
    }
    function ChangeEditExpenditureAmountLabel(productName) {
        $.ajax({
            url: '/AccountBook/GetExpenditureAmountLabel' + '?productName=' + productName,
            type: 'POST',
            headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
            dataType: 'json',
            data: null,
            contentType: 'application/json; charset=utf-8',
            success: function (data) {
                if (data.result) {
                    $labelEditExpenditureAmount.text(data.label);
                }
                else {
                    $labelEditExpenditureAmount.text(data.label);
                }
            }
        });
    }
    $btnEditExpenditureGridRow.off('click').on('click', function () {
        EditExpenditureGridRow($(this).attr("data-errorMessageSelectGridRow"));
    });
    $btnConfirmDeleteExpenditure.off('click').on('click', function () {
        ConfirmDeleteExpenditure($(this).attr("data-errorMessageSelectGridRow"));
    });
    $btnExportExcelExpenditure.off('click').on('click', function () {
        ExportExcelExpenditure();
    });
    $btnDeleteExpenditure.off('click').on('click', function () {
        DeleteExpenditure($(this).attr("data-errorMessageSelectGridRow"));
    });
    $formCreateExpenditure.off('submit').on('submit', function () {
        return CreateExpenditure();
    });
    $formEditExpenditure.off('submit').on('submit', function () {
        return UpdateExpenditure();
    });
    $createExpenditureMainClass.off('change').on('change', function () {
        CreateFormShowExpenditureSubClassBySelectedExpenditureMainClass(this);
    });
    $createExpenditureSubClass.off('change').on('change', function () {
        CreateFormShowExpenditureDivCreateExpenditureMyDepositAssetBySelectedExpenditureSubClass(this);
    });
    $editExpenditureMainClass.off('change').on('change', function () {
        EditFormShowExpenditureSubClassBySelectedExpenditureMainClass(this);
    });
    $editExpenditureSubClass.off('change').on('change', function () {
        EditFormShowExpenditureDivCreateExpenditureMyDepositAssetBySelectedExpenditureSubClass(this);
    });
    $createExpenditurePaymentMethod.off('change').on('change', function () {
        ChangeCreateExpenditureAmountLabel(this.value);
    });
    $editExpenditurePaymentMethod.off('change').on('change', function () {
        ChangeEditExpenditureAmountLabel(this.value);
    });
    $(function () {
        ChangeCreateExpenditureAmountLabel($createExpenditurePaymentMethod.find('option:selected').val());
    });
})();
