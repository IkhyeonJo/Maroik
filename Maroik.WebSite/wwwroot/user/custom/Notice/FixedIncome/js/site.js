const $createFixedIncomeTabs = $('#createFixedIncomeTabs');
const $editFixedIncomeTabs = $('#editFixedIncomeTabs');
const $createFixedIncomeMaturityDate = $('#createFixedIncomeMaturityDate');
const $editFixedIncomeMaturityDate = $('#editFixedIncomeMaturityDate');
const $gridSearch = $('#gridSearch');
const $createFixedIncomeSubClass = $('#createFixedIncomeSubClass');
const $createFixedIncomeDepositDay = $('#createFixedIncomeDepositDay');
const $editFixedIncomeSubClass = $('#editFixedIncomeSubClass');
const $editFixedIncomeDepositDay = $('#editFixedIncomeDepositDay');
const $formCreateFixedIncome = $('#formCreateFixedIncome');
const $createFixedIncomeMainClass = $('#createFixedIncomeMainClass');
const $createFixedIncomeContent = $('#createFixedIncomeContent');
const $createFixedIncomeAmount = $('#createFixedIncomeAmount');
const $createFixedIncomeDepositMonth = $('#createFixedIncomeDepositMonth');
const $createFixedIncomeNote = $('#createFixedIncomeNote');
const $createFixedIncomeDepositMyAssetProductName = $('#createFixedIncomeDepositMyAssetProductName');
const $__RequestVerificationToken = $('input[name="__RequestVerificationToken"]');
const $createFixedIncomeDialogModal = $('#createFixedIncomeDialogModal');
const $editFixedIncomeId = $('#editFixedIncomeId');
const $editFixedIncomeMainClass = $('#editFixedIncomeMainClass');
const $editFixedIncomeContent = $('#editFixedIncomeContent');
const $editFixedIncomeAmount = $('#editFixedIncomeAmount');
const $editFixedIncomeDepositMonth = $('#editFixedIncomeDepositMonth');
const $editFixedIncomeNote = $('#editFixedIncomeNote');
const $editFixedIncomeDepositMyAssetProductName = $('#editFixedIncomeDepositMyAssetProductName');
const $editFixedIncomeUnpunctuality = $('#editFixedIncomeUnpunctuality');
const $editFixedIncomeDialogModal = $('#editFixedIncomeDialogModal');
const $formEditFixedIncome = $('#formEditFixedIncome');
const $confirmDeleteFixedIncomeDialogModal = $('#confirmDeleteFixedIncomeDialogModal');
const $labelCreateFixedIncomeAmount = $('#labelCreateFixedIncomeAmount');
const $labelEditFixedIncomeAmount = $('#labelEditFixedIncomeAmount');
const $btnEditFixedExpenditureGridRow = $('#btnEditFixedExpenditureGridRow');
const $btnConfirmDeleteFixedIncome = $('#btnConfirmDeleteFixedIncome');
const $btnExportExcelFixedIncome = $('#btnExportExcelFixedIncome');
const $btnDeleteFixedIncome = $('#btnDeleteFixedIncome');

const localizer = {
    PrevText: $('#LocalizerPrevText').val(),
    NextText: $('#LocalizerNextText').val(),
    January: $('#LocalizerJanuary').val(),
    February: $('#LocalizerFebruary').val(),
    March: $('#LocalizerMarch').val(),
    April: $('#LocalizerApril').val(),
    May: $('#LocalizerMay').val(),
    June: $('#LocalizerJune').val(),
    July: $('#LocalizerJuly').val(),
    August: $('#LocalizerAugust').val(),
    September: $('#LocalizerSeptember').val(),
    October: $('#LocalizerOctober').val(),
    November: $('#LocalizerNovember').val(),
    December: $('#LocalizerDecember').val(),
    Jan: $('#LocalizerJan').val(),
    Feb: $('#LocalizerFeb').val(),
    Mar: $('#LocalizerMar').val(),
    Apr: $('#LocalizerApr').val(),
    Jun: $('#LocalizerJun').val(),
    Jul: $('#LocalizerJul').val(),
    Aug: $('#LocalizerAug').val(),
    Sep: $('#LocalizerSep').val(),
    Oct: $('#LocalizerOct').val(),
    Nov: $('#LocalizerNov').val(),
    Dec: $('#LocalizerDec').val(),
    Sunday: $('#LocalizerSunday').val(),
    Monday: $('#LocalizerMonday').val(),
    Tuesday: $('#LocalizerTuesday').val(),
    Wednesday: $('#LocalizerWednesday').val(),
    Thursday: $('#LocalizerThursday').val(),
    Friday: $('#LocalizerFriday').val(),
    Saturday: $('#LocalizerSaturday').val(),
    Sun: $('#LocalizerSun').val(),
    Mon: $('#LocalizerMon').val(),
    Tue: $('#LocalizerTue').val(),
    Wed: $('#LocalizerWed').val(),
    Thu: $('#LocalizerThu').val(),
    Fri: $('#LocalizerFri').val(),
    Sat: $('#LocalizerSat').val(),
    Su: $('#LocalizerSu').val(),
    Mo: $('#LocalizerMo').val(),
    Tu: $('#LocalizerTu').val(),
    We: $('#LocalizerWe').val(),
    Th: $('#LocalizerTh').val(),
    Fr: $('#LocalizerFr').val(),
    Sa: $('#LocalizerSa').val(),
    YearSuffix: $('#LocalizerYearSuffix').val(),
    NoMaturityDate: $('#LocalizerNoMaturityDate').val(),
    Today: $('#LocalizerToday').val(),
    MaturityDateError: $('#LocalizerMaturityDateError').val(),
};

$.datepicker.setDefaults({
    dateFormat: 'yy-mm-dd',
    prevText: localizer.prevText,
    nextText: localizer.nextText,
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
        beforeShow: function (input) {
            setTimeout(function () {
                let buttonPane = $(input)
                    .datepicker("widget")
                    .find(".ui-datepicker-buttonpane");

                $("<button>", {
                    text: localizer.NoMaturityDate,
                    click: function () {
                        $.datepicker._clearDate(input);
                        $createFixedIncomeMaturityDate.datepicker("setDate", new Date('9999', '11', '31'));
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
                        $createFixedIncomeMaturityDate.datepicker("setDate", new Date('9999', '11', '31'));
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
        },
        onSelect: function (date) {
            if (!date) {
                date = '9999-12-31';
            }
            let tempCurrentDate = new Date();
            let month = tempCurrentDate.getMonth() + 1;
            let day = tempCurrentDate.getDate();

            month = month >= 10 ? month : '0' + month;
            day = day >= 10 ? day : '0' + day;

            let currentDate = tempCurrentDate.getFullYear() + '-' + month + '-' + day;

            if (date < currentDate) {
                alert(localizer.MaturityDateError);
                $createFixedIncomeMaturityDate.datepicker("setDate", new Date());
            }
        }
    });

    $editFixedIncomeMaturityDate.datepicker({
        showButtonPanel: true,
        beforeShow: function (input) {
            setTimeout(function () {
                let buttonPane = $(input)
                    .datepicker("widget")
                    .find(".ui-datepicker-buttonpane");

                $("<button>", {
                    text: localizer.NoMaturityDate,
                    click: function () {
                        $.datepicker._clearDate(input);
                        $editFixedIncomeMaturityDate.datepicker("setDate", new Date('9999', '11', '31'));
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
                        $editFixedIncomeMaturityDate.datepicker("setDate", new Date('9999', '11', '31'));
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
        },
        onSelect: function (date) {
            if (!date) {
                date = '9999-12-31';
            }
            let tempCurrentDate = new Date();
            let month = tempCurrentDate.getMonth() + 1;
            let day = tempCurrentDate.getDate();

            month = month >= 10 ? month : '0' + month;
            day = day >= 10 ? day : '0' + day;

            let currentDate = tempCurrentDate.getFullYear() + '-' + month + '-' + day;

            if (date < currentDate) {
                alert(localizer.MaturityDateError);
                $editFixedIncomeMaturityDate.datepicker("setDate", new Date());
            }
        }
    });

    $createFixedIncomeMaturityDate.datepicker("setDate", new Date());
    $editFixedIncomeMaturityDate.datepicker("setDate", new Date());

    $('<style> .ui-datepicker-close { display: none; } </style>').appendTo("head");
    $('<style> .ui-datepicker-current { display: none; } </style>').appendTo("head");
});

let selectedRowColor = "table-primary";

$(document).off('rowclick.FixedIncome').on('rowclick.FixedIncome', e => {
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

$(document).off('reloadstart.FixedIncome').on('reloadstart.FixedIncome', _ => {
});

$(document).off('reloadend.FixedIncome').on('reloadend.FixedIncome', _ => {
});

$(document).off('reloadfail.FixedIncome').on('reloadfail.FixedIncome', _ => {
});

$(document).off('gridconfigure.FixedIncome').on('gridconfigure.FixedIncome', _ => {
});

$(document).off('dblclick.FixedIncome', '.clsGridRow').on('dblclick.FixedIncome', '.clsGridRow', function () {
    $('.clsGridRow').each(function () {
        if ($(this).attr("class").includes(selectedRowColor)) {
            EditFixedIncomeGridRow();
        }
    });
});

$gridSearch.off('input').on('input', function () {
    const grid = new MvcGrid(document.querySelector('.mvc-grid'));
    grid.url.searchParams.set("wholeSearch", this.value);
    grid.reload();
});

function CreateFormShowFixedIncomeSubClassBySelectedFixedIncomeMainClass(createFixedIncomeMainClass) {

    if (createFixedIncomeMainClass.value === "RegularIncome") {
        $createFixedIncomeSubClass.find('option[value=LaborIncome]').prop('disabled', true).prop('hidden', true);
        $createFixedIncomeSubClass.find('option[value=BusinessIncome]').prop('disabled', true).prop('hidden', true);
        $createFixedIncomeSubClass.find('option[value=PensionIncome]').prop('disabled', true).prop('hidden', true);
        $createFixedIncomeSubClass.find('option[value=FinancialIncome]').prop('disabled', true).prop('hidden', true);
        $createFixedIncomeSubClass.find('option[value=RentalIncome]').prop('disabled', true).prop('hidden', true);
        $createFixedIncomeSubClass.find('option[value=OtherIncome]').prop('disabled', true).prop('hidden', true);

        $createFixedIncomeSubClass.find('option:selected').removeAttr('selected');

        $createFixedIncomeSubClass.find('option[value=LaborIncome]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $createFixedIncomeSubClass.find('option[value=BusinessIncome]').prop('disabled', false).prop('hidden', false);
        $createFixedIncomeSubClass.find('option[value=PensionIncome]').prop('disabled', false).prop('hidden', false);
        $createFixedIncomeSubClass.find('option[value=FinancialIncome]').prop('disabled', false).prop('hidden', false);
        $createFixedIncomeSubClass.find('option[value=RentalIncome]').prop('disabled', false).prop('hidden', false);
        $createFixedIncomeSubClass.find('option[value=OtherIncome]').prop('disabled', false).prop('hidden', false);

        $createFixedIncomeSubClass.val("LaborIncome").change();
    }
    else if (createFixedIncomeMainClass.value === "IrregularIncome") {
        $createFixedIncomeSubClass.find('option[value=LaborIncome]').prop('disabled', true).prop('hidden', true);
        $createFixedIncomeSubClass.find('option[value=BusinessIncome]').prop('disabled', true).prop('hidden', true);
        $createFixedIncomeSubClass.find('option[value=PensionIncome]').prop('disabled', true).prop('hidden', true);
        $createFixedIncomeSubClass.find('option[value=FinancialIncome]').prop('disabled', true).prop('hidden', true);
        $createFixedIncomeSubClass.find('option[value=RentalIncome]').prop('disabled', true).prop('hidden', true);
        $createFixedIncomeSubClass.find('option[value=OtherIncome]').prop('disabled', true).prop('hidden', true);

        $createFixedIncomeSubClass.find('option:selected').removeAttr('selected');

        $createFixedIncomeSubClass.find('option[value=LaborIncome]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $createFixedIncomeSubClass.find('option[value=OtherIncome]').prop('disabled', false).prop('hidden', false);

        $createFixedIncomeSubClass.val("LaborIncome").change();
    }
}

function CreateFormShowFixedIncomeDepositDayBySelectedFixedIncomeDepositMonth(createFixedIncomeDepositMonth) {

    if (createFixedIncomeDepositMonth.value === "1" ||
        createFixedIncomeDepositMonth.value === "3" ||
        createFixedIncomeDepositMonth.value === "5" ||
        createFixedIncomeDepositMonth.value === "7" ||
        createFixedIncomeDepositMonth.value === "8" ||
        createFixedIncomeDepositMonth.value === "10" ||
        createFixedIncomeDepositMonth.value === "12"
    ) {
        $createFixedIncomeDepositDay.find('option[value=29]').prop('disabled', true).prop('hidden', true);
        $createFixedIncomeDepositDay.find('option[value=30]').prop('disabled', true).prop('hidden', true);
        $createFixedIncomeDepositDay.find('option[value=31]').prop('disabled', true).prop('hidden', true);

        $createFixedIncomeDepositDay.find('option:selected').removeAttr('selected');

        $createFixedIncomeDepositDay.find('option[value=1]').attr('selected', 'selected');
        $createFixedIncomeDepositDay.find('option[value=29]').prop('disabled', false).prop('hidden', false);
        $createFixedIncomeDepositDay.find('option[value=30]').prop('disabled', false).prop('hidden', false);
        $createFixedIncomeDepositDay.find('option[value=31]').prop('disabled', false).prop('hidden', false);

        $createFixedIncomeDepositDay.val("1").change();
    }
    else if (createFixedIncomeDepositMonth.value === "2") {
        $createFixedIncomeDepositDay.find('option[value=29]').prop('disabled', true).prop('hidden', true);
        $createFixedIncomeDepositDay.find('option[value=30]').prop('disabled', true).prop('hidden', true);
        $createFixedIncomeDepositDay.find('option[value=31]').prop('disabled', true).prop('hidden', true);

        $createFixedIncomeDepositDay.find('option:selected').removeAttr('selected');

        $createFixedIncomeDepositDay.find('option[value=1]').attr('selected', 'selected');
        $createFixedIncomeDepositDay.find('option[value=29]').prop('disabled', false).prop('hidden', false);

        $createFixedIncomeDepositDay.val("1").change();
    }
    else if (createFixedIncomeDepositMonth.value === "4" ||
        createFixedIncomeDepositMonth.value === "6" ||
        createFixedIncomeDepositMonth.value === "9" ||
        createFixedIncomeDepositMonth.value === "11"
    ) {
        $createFixedIncomeDepositDay.find('option[value=29]').prop('disabled', true).prop('hidden', true);
        $createFixedIncomeDepositDay.find('option[value=30]').prop('disabled', true).prop('hidden', true);
        $createFixedIncomeDepositDay.find('option[value=31]').prop('disabled', true).prop('hidden', true);

        $createFixedIncomeDepositDay.find('option:selected').removeAttr('selected');

        $createFixedIncomeDepositDay.find('option[value=1]').attr('selected', 'selected');
        $createFixedIncomeDepositDay.find('option[value=29]').prop('disabled', false).prop('hidden', false);
        $createFixedIncomeDepositDay.find('option[value=30]').prop('disabled', false).prop('hidden', false);

        $createFixedIncomeDepositDay.val("1").change();
    }
}

function EditFormShowFixedIncomeSubClassBySelectedFixedIncomeMainClass(editFixedIncomeMainClass) {

    if (editFixedIncomeMainClass.value === "RegularIncome") {
        $editFixedIncomeSubClass.find('option[value=LaborIncome]').prop('disabled', true).prop('hidden', true);
        $editFixedIncomeSubClass.find('option[value=BusinessIncome]').prop('disabled', true).prop('hidden', true);
        $editFixedIncomeSubClass.find('option[value=PensionIncome]').prop('disabled', true).prop('hidden', true);
        $editFixedIncomeSubClass.find('option[value=FinancialIncome]').prop('disabled', true).prop('hidden', true);
        $editFixedIncomeSubClass.find('option[value=RentalIncome]').prop('disabled', true).prop('hidden', true);
        $editFixedIncomeSubClass.find('option[value=OtherIncome]').prop('disabled', true).prop('hidden', true);

        $editFixedIncomeSubClass.find('option:selected').removeAttr('selected');

        $editFixedIncomeSubClass.find('option[value=LaborIncome]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $editFixedIncomeSubClass.find('option[value=BusinessIncome]').prop('disabled', false).prop('hidden', false);
        $editFixedIncomeSubClass.find('option[value=PensionIncome]').prop('disabled', false).prop('hidden', false);
        $editFixedIncomeSubClass.find('option[value=FinancialIncome]').prop('disabled', false).prop('hidden', false);
        $editFixedIncomeSubClass.find('option[value=RentalIncome]').prop('disabled', false).prop('hidden', false);
        $editFixedIncomeSubClass.find('option[value=OtherIncome]').prop('disabled', false).prop('hidden', false);

        $editFixedIncomeSubClass.val("LaborIncome").change();
    }
    else if (editFixedIncomeMainClass.value === "IrregularIncome") {
        $editFixedIncomeSubClass.find('option[value=LaborIncome]').prop('disabled', true).prop('hidden', true);
        $editFixedIncomeSubClass.find('option[value=BusinessIncome]').prop('disabled', true).prop('hidden', true);
        $editFixedIncomeSubClass.find('option[value=PensionIncome]').prop('disabled', true).prop('hidden', true);
        $editFixedIncomeSubClass.find('option[value=FinancialIncome]').prop('disabled', true).prop('hidden', true);
        $editFixedIncomeSubClass.find('option[value=RentalIncome]').prop('disabled', true).prop('hidden', true);
        $editFixedIncomeSubClass.find('option[value=OtherIncome]').prop('disabled', true).prop('hidden', true);

        $editFixedIncomeSubClass.find('option:selected').removeAttr('selected');

        $editFixedIncomeSubClass.find('option[value=LaborIncome]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $editFixedIncomeSubClass.find('option[value=OtherIncome]').prop('disabled', false).prop('hidden', false);

        $editFixedIncomeSubClass.val("LaborIncome").change();
    }
}

function EditFormShowFixedIncomeDepositDayBySelectedFixedIncomeDepositMonth(editFixedIncomeDepositMonth) {

    if (editFixedIncomeDepositMonth.value === "1" ||
        editFixedIncomeDepositMonth.value === "3" ||
        editFixedIncomeDepositMonth.value === "5" ||
        editFixedIncomeDepositMonth.value === "7" ||
        editFixedIncomeDepositMonth.value === "8" ||
        editFixedIncomeDepositMonth.value === "10" ||
        editFixedIncomeDepositMonth.value === "12"
    ) {
        $editFixedIncomeDepositDay.find('option[value=29]').prop('disabled', true).prop('hidden', true);
        $editFixedIncomeDepositDay.find('option[value=30]').prop('disabled', true).prop('hidden', true);
        $editFixedIncomeDepositDay.find('option[value=31]').prop('disabled', true).prop('hidden', true);

        $editFixedIncomeDepositDay.find('option:selected').removeAttr('selected');

        $editFixedIncomeDepositDay.find('option[value=1]').attr('selected', 'selected');
        $editFixedIncomeDepositDay.find('option[value=29]').prop('disabled', false).prop('hidden', false);
        $editFixedIncomeDepositDay.find('option[value=30]').prop('disabled', false).prop('hidden', false);
        $editFixedIncomeDepositDay.find('option[value=31]').prop('disabled', false).prop('hidden', false);

        $editFixedIncomeDepositDay.val("1").change();
    }
    else if (editFixedIncomeDepositMonth.value === "2") {
        $editFixedIncomeDepositDay.find('option[value=29]').prop('disabled', true).prop('hidden', true);
        $editFixedIncomeDepositDay.find('option[value=30]').prop('disabled', true).prop('hidden', true);
        $editFixedIncomeDepositDay.find('option[value=31]').prop('disabled', true).prop('hidden', true);

        $editFixedIncomeDepositDay.find('option:selected').removeAttr('selected');

        $editFixedIncomeDepositDay.find('option[value=1]').attr('selected', 'selected');
        $editFixedIncomeDepositDay.find('option[value=29]').prop('disabled', false).prop('hidden', false);

        $editFixedIncomeDepositDay.val("1").change();
    }
    else if (editFixedIncomeDepositMonth.value === "4" ||
        editFixedIncomeDepositMonth.value === "6" ||
        editFixedIncomeDepositMonth.value === "9" ||
        editFixedIncomeDepositMonth.value === "11"
    ) {
        $editFixedIncomeDepositDay.find('option[value=29]').prop('disabled', true).prop('hidden', true);
        $editFixedIncomeDepositDay.find('option[value=30]').prop('disabled', true).prop('hidden', true);
        $editFixedIncomeDepositDay.find('option[value=31]').prop('disabled', true).prop('hidden', true);

        $editFixedIncomeDepositDay.find('option:selected').removeAttr('selected');

        $editFixedIncomeDepositDay.find('option[value=1]').attr('selected', 'selected');
        $editFixedIncomeDepositDay.find('option[value=29]').prop('disabled', false).prop('hidden', false);
        $editFixedIncomeDepositDay.find('option[value=30]').prop('disabled', false).prop('hidden', false);

        $editFixedIncomeDepositDay.val("1").change();
    }
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
        url: '/Notice/CreateFixedIncome',
        type: 'POST',
        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
        dataType: 'json',
        data: paramValue,
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            if (data.result) {
                $createFixedIncomeDialogModal.modal('hide');

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

function EditFixedIncomeGridRow(errorMessageSelectGridRow) {

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
        url: '/Notice/IsFixedIncomeExists' + '?id=' + selectedRowId,
        type: 'POST',
        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            if (data.result) {

                $editFixedIncomeId.val(data.fixedIncome.id);
                $editFixedIncomeMainClass.val(data.fixedIncome.mainClass).change();

                if (data.fixedIncome.mainClass === "RegularIncome") {
                    $editFixedIncomeSubClass.find('option[value=LaborIncome]').prop('disabled', true).prop('hidden', true);
                    $editFixedIncomeSubClass.find('option[value=BusinessIncome]').prop('disabled', true).prop('hidden', true);
                    $editFixedIncomeSubClass.find('option[value=PensionIncome]').prop('disabled', true).prop('hidden', true);
                    $editFixedIncomeSubClass.find('option[value=FinancialIncome]').prop('disabled', true).prop('hidden', true);
                    $editFixedIncomeSubClass.find('option[value=RentalIncome]').prop('disabled', true).prop('hidden', true);
                    $editFixedIncomeSubClass.find('option[value=OtherIncome]').prop('disabled', true).prop('hidden', true);

                    $editFixedIncomeSubClass.find('option:selected').removeAttr('selected');

                    $editFixedIncomeSubClass.find('option[value=LaborIncome]').prop('disabled', false).prop('hidden', false);
                    $editFixedIncomeSubClass.find('option[value=BusinessIncome]').prop('disabled', false).prop('hidden', false);
                    $editFixedIncomeSubClass.find('option[value=PensionIncome]').prop('disabled', false).prop('hidden', false);
                    $editFixedIncomeSubClass.find('option[value=FinancialIncome]').prop('disabled', false).prop('hidden', false);
                    $editFixedIncomeSubClass.find('option[value=RentalIncome]').prop('disabled', false).prop('hidden', false);
                    $editFixedIncomeSubClass.find('option[value=OtherIncome]').prop('disabled', false).prop('hidden', false);

                    $editFixedIncomeSubClass.val(data.fixedIncome.subClass).change();
                }
                else if (data.fixedIncome.mainClass === "IrregularIncome") {
                    $editFixedIncomeSubClass.find('option[value=LaborIncome]').prop('disabled', true).prop('hidden', true);
                    $editFixedIncomeSubClass.find('option[value=BusinessIncome]').prop('disabled', true).prop('hidden', true);
                    $editFixedIncomeSubClass.find('option[value=PensionIncome]').prop('disabled', true).prop('hidden', true);
                    $editFixedIncomeSubClass.find('option[value=FinancialIncome]').prop('disabled', true).prop('hidden', true);
                    $editFixedIncomeSubClass.find('option[value=RentalIncome]').prop('disabled', true).prop('hidden', true);
                    $editFixedIncomeSubClass.find('option[value=OtherIncome]').prop('disabled', true).prop('hidden', true);

                    $editFixedIncomeSubClass.find('option:selected').removeAttr('selected');

                    $editFixedIncomeSubClass.find('option[value=LaborIncome]').prop('disabled', false).prop('hidden', false);
                    $editFixedIncomeSubClass.find('option[value=OtherIncome]').prop('disabled', false).prop('hidden', false);

                    $editFixedIncomeSubClass.val(data.fixedIncome.subClass).change();
                }

                $editFixedIncomeContent.val(data.fixedIncome.content);
                $editFixedIncomeAmount.val(data.fixedIncome.amount);

                $editFixedIncomeDepositMonth.val(data.fixedIncome.depositMonth.toString()).change();

                if (data.fixedIncome.depositMonth.toString() === "1" ||
                    data.fixedIncome.depositMonth.toString() === "3" ||
                    data.fixedIncome.depositMonth.toString() === "5" ||
                    data.fixedIncome.depositMonth.toString() === "7" ||
                    data.fixedIncome.depositMonth.toString() === "8" ||
                    data.fixedIncome.depositMonth.toString() === "10" ||
                    data.fixedIncome.depositMonth.toString() === "12"
                ) {
                    $editFixedIncomeDepositDay.find('option[value=29]').prop('disabled', true).prop('hidden', true);
                    $editFixedIncomeDepositDay.find('option[value=30]').prop('disabled', true).prop('hidden', true);
                    $editFixedIncomeDepositDay.find('option[value=31]').prop('disabled', true).prop('hidden', true);

                    $editFixedIncomeDepositDay.find('option:selected').removeAttr('selected');

                    $editFixedIncomeDepositDay.find('option[value=29]').prop('disabled', false).prop('hidden', false);
                    $editFixedIncomeDepositDay.find('option[value=30]').prop('disabled', false).prop('hidden', false);
                    $editFixedIncomeDepositDay.find('option[value=31]').prop('disabled', false).prop('hidden', false);

                    $editFixedIncomeDepositDay.val(data.fixedIncome.depositDay.toString()).change();
                }
                else if (data.fixedIncome.depositMonth.toString() === "2") {
                    $editFixedIncomeDepositDay.find('option[value=29]').prop('disabled', true).prop('hidden', true);
                    $editFixedIncomeDepositDay.find('option[value=30]').prop('disabled', true).prop('hidden', true);
                    $editFixedIncomeDepositDay.find('option[value=31]').prop('disabled', true).prop('hidden', true);

                    $editFixedIncomeDepositDay.find('option:selected').removeAttr('selected');

                    $editFixedIncomeDepositDay.find('option[value=29]').prop('disabled', false).prop('hidden', false);

                    $editFixedIncomeDepositDay.val(data.fixedIncome.depositDay.toString()).change();
                }
                else if (data.fixedIncome.depositMonth.toString() === "4" ||
                    data.fixedIncome.depositMonth.toString() === "6" ||
                    data.fixedIncome.depositMonth.toString() === "9" ||
                    data.fixedIncome.depositMonth.toString() === "11"
                ) {
                    $editFixedIncomeDepositDay.find('option[value=29]').prop('disabled', true).prop('hidden', true);
                    $editFixedIncomeDepositDay.find('option[value=30]').prop('disabled', true).prop('hidden', true);
                    $editFixedIncomeDepositDay.find('option[value=31]').prop('disabled', true).prop('hidden', true);

                    $editFixedIncomeDepositDay.find('option:selected').removeAttr('selected');

                    $editFixedIncomeDepositDay.find('option[value=29]').prop('disabled', false).prop('hidden', false);
                    $editFixedIncomeDepositDay.find('option[value=30]').prop('disabled', false).prop('hidden', false);

                    $editFixedIncomeDepositDay.val(data.fixedIncome.depositDay.toString()).change();
                }

                $editFixedIncomeMaturityDate.val(data.fixedIncome.maturityDate);
                $editFixedIncomeNote.val(data.fixedIncome.note);
                $editFixedIncomeDepositMyAssetProductName.val(data.fixedIncome.depositMyAssetProductName).change();
                $editFixedIncomeUnpunctuality.prop("checked", data.fixedIncome.unpunctuality);

                $editFixedIncomeDialogModal.modal({
                    keyboard: false,
                    backdrop: "static"
                });

                $editFixedIncomeDialogModal.modal('toggle');
                $editFixedIncomeDialogModal.modal('show');
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
        url: '/Notice/UpdateFixedIncome',
        type: 'POST',
        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
        dataType: 'json',
        data: paramValue,
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            if (data.result) {
                $editFixedIncomeDialogModal.modal('hide');

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

function ConfirmDeleteFixedIncome(errorMessageSelectGridRow) {

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

    $confirmDeleteFixedIncomeDialogModal.modal({
        keyboard: false,
        backdrop: "static"
    });

    $confirmDeleteFixedIncomeDialogModal.modal('toggle');
    $confirmDeleteFixedIncomeDialogModal.modal('show');
}

function DeleteFixedIncome(errorMessageSelectGridRow) {

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
        url: '/Notice/IsFixedIncomeExists' + '?id=' + selectedRowId,
        type: 'POST',
        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            if (data.result) {

                let paramValue = JSON.stringify({
                    Id: data.fixedIncome.id
                });

                $.ajax({
                    url: '/Notice/DeleteFixedIncome',
                    type: 'POST',
                    headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
                    dataType: 'json',
                    data: paramValue,
                    contentType: 'application/json; charset=utf-8',
                    success: function (data) {
                        if (data.result) {
                            $confirmDeleteFixedIncomeDialogModal.modal('hide');

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
        url: '/Notice/GetFixedIncomeAmountLabel' + '?productName=' + productName,
        type: 'POST',
        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            if (data.result) {
                $labelCreateFixedIncomeAmount.text(data.label);
            }
            else {
                $labelCreateFixedIncomeAmount.text(data.label);
            }
        }
    });
}

function ChangeEditFixedIncomeAmountLabel(productName) {
    $.ajax({
        url: '/Notice/GetFixedIncomeAmountLabel' + '?productName=' + productName,
        type: 'POST',
        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            if (data.result) {
                $labelEditFixedIncomeAmount.text(data.label);
            }
            else {
                $labelEditFixedIncomeAmount.text(data.label);
            }
        }
    });
}

$btnEditFixedExpenditureGridRow.off('click').on('click', function () {
    EditFixedIncomeGridRow($(this).attr('data-errorMessageSelectGridRow'));
});

$btnConfirmDeleteFixedIncome.off('click').on('click', function () {
    ConfirmDeleteFixedIncome($(this).attr('data-errorMessageSelectGridRow'));
});

$btnExportExcelFixedIncome.off('click').on('click', function () {
    ExportExcelFixedIncome();
});

$btnDeleteFixedIncome.off('click').on('click', function () {
    DeleteFixedIncome($(this).attr('data-errorMessageSelectGridRow'));
});

$formCreateFixedIncome.off('submit').on('submit', function () {
    return CreateFixedIncome();
});

$formEditFixedIncome.off('submit').on('submit', function () {
    return UpdateFixedIncome();
});

$createFixedIncomeMainClass.off('change').on('change', function () {
    return CreateFormShowFixedIncomeSubClassBySelectedFixedIncomeMainClass(this);
});

$createFixedIncomeDepositMonth.off('change').on('change', function () {
    return CreateFormShowFixedIncomeDepositDayBySelectedFixedIncomeDepositMonth(this);
});

$editFixedIncomeMainClass.off('change').on('change', function () {
    return EditFormShowFixedIncomeSubClassBySelectedFixedIncomeMainClass(this);
});

$editFixedIncomeDepositMonth.off('change').on('change', function () {
    return EditFormShowFixedIncomeDepositDayBySelectedFixedIncomeDepositMonth(this);
});

$createFixedIncomeDepositMyAssetProductName.off('change').on('change', function () {
    ChangeCreateFixedIncomeAmountLabel(this.value);
});

$editFixedIncomeDepositMyAssetProductName.off('change').on('change', function () {
    ChangeEditFixedIncomeAmountLabel(this.value);
});

$(function () {
    ChangeCreateFixedIncomeAmountLabel($createFixedIncomeDepositMyAssetProductName.find('option:selected').val());
})