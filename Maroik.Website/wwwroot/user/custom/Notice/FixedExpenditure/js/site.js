const $createFixedExpenditureTabs = $("#createFixedExpenditureTabs");
const $editFixedExpenditureTabs = $("#editFixedExpenditureTabs");
const $createFixedExpenditureMaturityDate = $("#createFixedExpenditureMaturityDate");
const $editFixedExpenditureMaturityDate = $("#editFixedExpenditureMaturityDate");
const $gridSearch = $('#gridSearch');
const $createFixedExpenditureSubClass = $('#createFixedExpenditureSubClass');
const $divCreateFixedExpenditureMyDepositAsset = $("#divCreateFixedExpenditureMyDepositAsset");
const $createFixedExpenditureDepositDay = $('#createFixedExpenditureDepositDay');
const $editFixedExpenditureSubClass = $('#editFixedExpenditureSubClass');
const $divEditFixedExpenditureMyDepositAsset = $("#divEditFixedExpenditureMyDepositAsset");
const $editFixedExpenditureDepositDay = $('#editFixedExpenditureDepositDay');
const $formCreateFixedExpenditure = $('#formCreateFixedExpenditure');
const $editFixedExpenditureDialogModal = $('#editFixedExpenditureDialogModal');
const $formEditFixedExpenditure = $('#formEditFixedExpenditure');
const $confirmDeleteFixedExpenditureDialogModal = $('#confirmDeleteFixedExpenditureDialogModal');
const $__RequestVerificationToken = $('input[name="__RequestVerificationToken"]');
const $createFixedExpenditureDialogModal = $('#createFixedExpenditureDialogModal');
const $createFixedExpenditurePaymentMethod = $('#createFixedExpenditurePaymentMethod');
const $editFixedExpenditurePaymentMethod = $('#editFixedExpenditurePaymentMethod');
const $editFixedExpenditureDepositMonth = $('#editFixedExpenditureDepositMonth');
const $editFixedExpenditureMainClass = $('#editFixedExpenditureMainClass');
const $createFixedExpenditureDepositMonth = $('#createFixedExpenditureDepositMonth');
const $createFixedExpenditureMainClass = $('#createFixedExpenditureMainClass');
const $btnDeleteFixedExpenditure = $('#btnDeleteFixedExpenditure');
const $btnExportExcelFixedExpenditure = $('#btnExportExcelFixedExpenditure');
const $btnConfirmDeleteFixedExpenditure = $('#btnConfirmDeleteFixedExpenditure');
const $btnEditFixedExpenditureGridRow = $('#btnEditFixedExpenditureGridRow');
const $labelEditFixedExpenditureAmount = $('#labelEditFixedExpenditureAmount');
const $labelCreateFixedExpenditureAmount = $('#labelCreateFixedExpenditureAmount');
const $editFixedExpenditureId = $('#editFixedExpenditureId');
const $editFixedExpenditureContent = $('#editFixedExpenditureContent');
const $editFixedExpenditureAmount = $('#editFixedExpenditureAmount');
const $editFixedExpenditureNote = $('#editFixedExpenditureNote');
const $editFixedExpenditureMyDepositAsset = $('#editFixedExpenditureMyDepositAsset');
const $editFixedExpenditureUnpunctuality = $('#editFixedExpenditureUnpunctuality');
const $createFixedExpenditureContent = $('#createFixedExpenditureContent');
const $createFixedExpenditureAmount = $('#createFixedExpenditureAmount');
const $createFixedExpenditureNote = $('#createFixedExpenditureNote');
const $createFixedExpenditureMyDepositAsset = $('#createFixedExpenditureMyDepositAsset');

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
    YearSuffix: $('#localizerYearSuffix').val(),
    NoMaturityDate: $('#localizerNoMaturityDate').val(),
    Today: $('#localizerToday').val(),
    MaturityDateError: $('#localizerMaturityDateError').val(),
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
    $createFixedExpenditureTabs.tabs();
    $editFixedExpenditureTabs.tabs();
    $createFixedExpenditureMaturityDate.datepicker({
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
                        $createFixedExpenditureMaturityDate.datepicker("setDate", new Date('9999', '11', '31'));
                    }
                }).appendTo(buttonPane).addClass("ui-datepicker-clear ui-state-default ui-priority-primary ui-corner-all");

                $("<button>", {
                    text: localizer.Today,
                    click: function () {
                        $.datepicker._clearDate(input);
                        $createFixedExpenditureMaturityDate.datepicker("setDate", new Date());
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
                        $createFixedExpenditureMaturityDate.datepicker("setDate", new Date('9999', '11', '31'));
                    }
                }).appendTo(buttonPane).addClass("ui-datepicker-clear ui-state-default ui-priority-primary ui-corner-all");

                $("<button>", {
                    text: localizer.Today,
                    click: function () {
                        $.datepicker._clearDate(instance.input);
                        $createFixedExpenditureMaturityDate.datepicker("setDate", new Date());
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
                $createFixedExpenditureMaturityDate.datepicker("setDate", new Date());
            }
        }
    });

    $editFixedExpenditureMaturityDate.datepicker({
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
                        $editFixedExpenditureMaturityDate.datepicker("setDate", new Date('9999', '11', '31'));
                    }
                }).appendTo(buttonPane).addClass("ui-datepicker-clear ui-state-default ui-priority-primary ui-corner-all");

                $("<button>", {
                    text: localizer.Today,
                    click: function () {
                        $.datepicker._clearDate(input);
                        $editFixedExpenditureMaturityDate.datepicker("setDate", new Date());
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
                        $editFixedExpenditureMaturityDate.datepicker("setDate", new Date('9999', '11', '31'));
                    }
                }).appendTo(buttonPane).addClass("ui-datepicker-clear ui-state-default ui-priority-primary ui-corner-all");

                $("<button>", {
                    text: localizer.Today,
                    click: function () {
                        $.datepicker._clearDate(instance.input);
                        $editFixedExpenditureMaturityDate.datepicker("setDate", new Date());
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
                $editFixedExpenditureMaturityDate.datepicker("setDate", new Date());
            }
        }
    });

    $createFixedExpenditureMaturityDate.datepicker("setDate", new Date());
    $editFixedExpenditureMaturityDate.datepicker("setDate", new Date());

    $('<style> .ui-datepicker-close { display: none; } </style>').appendTo("head");
    $('<style> .ui-datepicker-current { display: none; } </style>').appendTo("head");
});

let selectedRowColor = "table-primary";

$(document).off('rowclick.FixedExpenditure').on('rowclick.FixedExpenditure', e => {
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

$(document).off('reloadstart.FixedExpenditure').on('reloadstart.FixedExpenditure', _ => {
});

$(document).off('reloadend.FixedExpenditure').on('reloadend.FixedExpenditure', _ => {
});

$(document).off('reloadfail.FixedExpenditure').on('reloadfail.FixedExpenditure', _ => {
});

$(document).off('gridconfigure.FixedExpenditure').on('gridconfigure.FixedExpenditure', _ => {
});

$(document).off('dblclick.FixedExpenditure', '.clsGridRow').on('dblclick.FixedExpenditure', '.clsGridRow', function () {
    $('.clsGridRow').each(function () {
        if ($(this).attr('class').includes(selectedRowColor)) {
            EditFixedExpenditureGridRow();
        }
    });
});

$gridSearch.off('input').on('input', function () {
    const grid = new MvcGrid(document.querySelector('.mvc-grid'));
    grid.url.searchParams.set('wholeSearch', this.value);
    grid.reload();
});

function CreateFormShowFixedExpenditureSubClassBySelectedFixedExpenditureMainClass(createFixedExpenditureMainClass) {

    if (createFixedExpenditureMainClass.value === "RegularSavings") {
        $createFixedExpenditureSubClass.find('option[value=Deposit]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=Investment]').prop('disabled', true).prop('hidden', true);

        $createFixedExpenditureSubClass.find('option[value=PublicPension]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=DebtRepayment]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=Tax]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=SocialInsurance]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=InterHouseholdTranserExpenses]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=NonProfitOrganizationTransfer]').prop('disabled', true).prop('hidden', true);

        $createFixedExpenditureSubClass.find('option[value=MealOrEatOutExpenses]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=HousingOrSuppliesCost]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=EducationExpenses]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=MedicalExpenses]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=TransportationCost]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=CommunicationCost]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=LeisureOrCulture]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=ClothingOrShoes]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=PinMoney]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=ProtectionTypeInsurance]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=OtherExpenses]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=UnknownExpenditure]').prop('disabled', true).prop('hidden', true);

        $createFixedExpenditureSubClass.find('option:selected').removeAttr("selected");

        $createFixedExpenditureSubClass.find('option[value=Deposit]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $createFixedExpenditureSubClass.find('option[value=Investment]').prop('disabled', false).prop('hidden', false);

        $createFixedExpenditureSubClass.val("Deposit").change();

        $divCreateFixedExpenditureMyDepositAsset.show();
    }
    else if (createFixedExpenditureMainClass.value === "NonConsumerSpending") {
        $createFixedExpenditureSubClass.find('option[value=Deposit]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=Investment]').prop('disabled', true).prop('hidden', true);

        $createFixedExpenditureSubClass.find('option[value=PublicPension]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=DebtRepayment]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=Tax]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=SocialInsurance]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=InterHouseholdTranserExpenses]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=NonProfitOrganizationTransfer]').prop('disabled', true).prop('hidden', true);

        $createFixedExpenditureSubClass.find('option[value=MealOrEatOutExpenses]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=HousingOrSuppliesCost]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=EducationExpenses]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=MedicalExpenses]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=TransportationCost]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=CommunicationCost]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=LeisureOrCulture]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=ClothingOrShoes]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=PinMoney]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=ProtectionTypeInsurance]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=OtherExpenses]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=UnknownExpenditure]').prop('disabled', true).prop('hidden', true);

        $createFixedExpenditureSubClass.find('option:selected').removeAttr("selected");

        $createFixedExpenditureSubClass.find('option[value=PublicPension]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $createFixedExpenditureSubClass.find('option[value=DebtRepayment]').prop('disabled', false).prop('hidden', false);
        $createFixedExpenditureSubClass.find('option[value=Tax]').prop('disabled', false).prop('hidden', false);
        $createFixedExpenditureSubClass.find('option[value=SocialInsurance]').prop('disabled', false).prop('hidden', false);
        $createFixedExpenditureSubClass.find('option[value=InterHouseholdTranserExpenses]').prop('disabled', false).prop('hidden', false);
        $createFixedExpenditureSubClass.find('option[value=NonProfitOrganizationTransfer]').prop('disabled', false).prop('hidden', false);

        $createFixedExpenditureSubClass.val("PublicPension").change();

        $divCreateFixedExpenditureMyDepositAsset.show();
    }
    else if (createFixedExpenditureMainClass.value === "ConsumerSpending") {
        $createFixedExpenditureSubClass.find('option[value=Deposit]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=Investment]').prop('disabled', true).prop('hidden', true);

        $createFixedExpenditureSubClass.find('option[value=PublicPension]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=DebtRepayment]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=Tax]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=SocialInsurance]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=InterHouseholdTranserExpenses]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=NonProfitOrganizationTransfer]').prop('disabled', true).prop('hidden', true);

        $createFixedExpenditureSubClass.find('option[value=MealOrEatOutExpenses]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=HousingOrSuppliesCost]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=EducationExpenses]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=MedicalExpenses]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=TransportationCost]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=CommunicationCost]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=LeisureOrCulture]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=ClothingOrShoes]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=PinMoney]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=ProtectionTypeInsurance]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=OtherExpenses]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureSubClass.find('option[value=UnknownExpenditure]').prop('disabled', true).prop('hidden', true);

        $createFixedExpenditureSubClass.find('option:selected').removeAttr("selected");

        $createFixedExpenditureSubClass.find('option[value=MealOrEatOutExpenses]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $createFixedExpenditureSubClass.find('option[value=HousingOrSuppliesCost]').prop('disabled', false).prop('hidden', false);
        $createFixedExpenditureSubClass.find('option[value=EducationExpenses]').prop('disabled', false).prop('hidden', false);
        $createFixedExpenditureSubClass.find('option[value=MedicalExpenses]').prop('disabled', false).prop('hidden', false);
        $createFixedExpenditureSubClass.find('option[value=TransportationCost]').prop('disabled', false).prop('hidden', false);
        $createFixedExpenditureSubClass.find('option[value=CommunicationCost]').prop('disabled', false).prop('hidden', false);
        $createFixedExpenditureSubClass.find('option[value=LeisureOrCulture]').prop('disabled', false).prop('hidden', false);
        $createFixedExpenditureSubClass.find('option[value=ClothingOrShoes]').prop('disabled', false).prop('hidden', false);
        $createFixedExpenditureSubClass.find('option[value=PinMoney]').prop('disabled', false).prop('hidden', false);
        $createFixedExpenditureSubClass.find('option[value=ProtectionTypeInsurance]').prop('disabled', false).prop('hidden', false);
        $createFixedExpenditureSubClass.find('option[value=OtherExpenses]').prop('disabled', false).prop('hidden', false);
        $createFixedExpenditureSubClass.find('option[value=UnknownExpenditure]').prop('disabled', false).prop('hidden', false);

        $createFixedExpenditureSubClass.val("MealOrEatOutExpenses").change();

        $divCreateFixedExpenditureMyDepositAsset.hide();
    }
}

function CreateFormShowFixedExpenditureDivCreateFixedExpenditureMyDepositAssetBySelectedFixedExpenditureSubClass(createFixedExpenditureSubClass) {

    if (createFixedExpenditureSubClass.value === "Deposit" ||
        createFixedExpenditureSubClass.value === "Investment" ||
        createFixedExpenditureSubClass.value === "PublicPension" ||
        createFixedExpenditureSubClass.value === "DebtRepayment") {
        $divCreateFixedExpenditureMyDepositAsset.show();
    }
    else {
        $divCreateFixedExpenditureMyDepositAsset.hide();
    }
}

function CreateFormShowFixedExpenditureDepositDayBySelectedFixedExpenditureDepositMonth(createFixedExpenditureDepositMonth) {

    if (createFixedExpenditureDepositMonth.value === "1" ||
        createFixedExpenditureDepositMonth.value === "3" ||
        createFixedExpenditureDepositMonth.value === "5" ||
        createFixedExpenditureDepositMonth.value === "7" ||
        createFixedExpenditureDepositMonth.value === "8" ||
        createFixedExpenditureDepositMonth.value === "10" ||
        createFixedExpenditureDepositMonth.value === "12"
    ) {
        $createFixedExpenditureDepositDay.find('option[value=29]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureDepositDay.find('option[value=30]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureDepositDay.find('option[value=31]').prop('disabled', true).prop('hidden', true);

        $createFixedExpenditureDepositDay.find('option:selected').removeAttr("selected");

        $createFixedExpenditureDepositDay.find('option[value=1]').attr('selected', 'selected');
        $createFixedExpenditureDepositDay.find('option[value=29]').prop('disabled', false).prop('hidden', false);
        $createFixedExpenditureDepositDay.find('option[value=30]').prop('disabled', false).prop('hidden', false);
        $createFixedExpenditureDepositDay.find('option[value=31]').prop('disabled', false).prop('hidden', false);

        $createFixedExpenditureDepositDay.val("1").change();
    }
    else if (createFixedExpenditureDepositMonth.value === "2") {
        $createFixedExpenditureDepositDay.find('option[value=29]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureDepositDay.find('option[value=30]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureDepositDay.find('option[value=31]').prop('disabled', true).prop('hidden', true);

        $createFixedExpenditureDepositDay.find('option:selected').removeAttr("selected");

        $createFixedExpenditureDepositDay.find('option[value=1]').attr('selected', 'selected');
        $createFixedExpenditureDepositDay.find('option[value=29]').prop('disabled', false).prop('hidden', false);

        $createFixedExpenditureDepositDay.val("1").change();
    }
    else if (createFixedExpenditureDepositMonth.value === "4" ||
        createFixedExpenditureDepositMonth.value === "6" ||
        createFixedExpenditureDepositMonth.value === "9" ||
        createFixedExpenditureDepositMonth.value === "11"
    ) {
        $createFixedExpenditureDepositDay.find('option[value=29]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureDepositDay.find('option[value=30]').prop('disabled', true).prop('hidden', true);
        $createFixedExpenditureDepositDay.find('option[value=31]').prop('disabled', true).prop('hidden', true);

        $createFixedExpenditureDepositDay.find('option:selected').removeAttr("selected");

        $createFixedExpenditureDepositDay.find('option[value=1]').attr('selected', 'selected');
        $createFixedExpenditureDepositDay.find('option[value=29]').prop('disabled', false).prop('hidden', false);
        $createFixedExpenditureDepositDay.find('option[value=30]').prop('disabled', false).prop('hidden', false);

        $createFixedExpenditureDepositDay.val("1").change();
    }
}

function EditFormShowFixedExpenditureSubClassBySelectedFixedExpenditureMainClass(editFixedExpenditureMainClass) {

    if (editFixedExpenditureMainClass.value === "RegularSavings") {
        $editFixedExpenditureSubClass.find('option[value=Deposit]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=Investment]').prop('disabled', true).prop('hidden', true);

        $editFixedExpenditureSubClass.find('option[value=PublicPension]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=DebtRepayment]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=Tax]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=SocialInsurance]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=InterHouseholdTranserExpenses]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=NonProfitOrganizationTransfer]').prop('disabled', true).prop('hidden', true);

        $editFixedExpenditureSubClass.find('option[value=MealOrEatOutExpenses]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=HousingOrSuppliesCost]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=EducationExpenses]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=MedicalExpenses]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=TransportationCost]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=CommunicationCost]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=LeisureOrCulture]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=ClothingOrShoes]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=PinMoney]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=ProtectionTypeInsurance]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=OtherExpenses]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=UnknownExpenditure]').prop('disabled', true).prop('hidden', true);

        $editFixedExpenditureSubClass.find('option:selected').removeAttr("selected");

        $editFixedExpenditureSubClass.find('option[value=Deposit]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $editFixedExpenditureSubClass.find('option[value=Investment]').prop('disabled', false).prop('hidden', false);

        $editFixedExpenditureSubClass.val("Deposit").change();

        $divEditFixedExpenditureMyDepositAsset.show();
    }
    else if (editFixedExpenditureMainClass.value === "NonConsumerSpending") {
        $editFixedExpenditureSubClass.find('option[value=Deposit]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=Investment]').prop('disabled', true).prop('hidden', true);

        $editFixedExpenditureSubClass.find('option[value=PublicPension]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=DebtRepayment]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=Tax]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=SocialInsurance]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=InterHouseholdTranserExpenses]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=NonProfitOrganizationTransfer]').prop('disabled', true).prop('hidden', true);

        $editFixedExpenditureSubClass.find('option[value=MealOrEatOutExpenses]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=HousingOrSuppliesCost]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=EducationExpenses]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=MedicalExpenses]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=TransportationCost]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=CommunicationCost]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=LeisureOrCulture]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=ClothingOrShoes]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=PinMoney]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=ProtectionTypeInsurance]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=OtherExpenses]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=UnknownExpenditure]').prop('disabled', true).prop('hidden', true);

        $editFixedExpenditureSubClass.find('option:selected').removeAttr("selected");

        $editFixedExpenditureSubClass.find('option[value=PublicPension]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $editFixedExpenditureSubClass.find('option[value=DebtRepayment]').prop('disabled', false).prop('hidden', false);
        $editFixedExpenditureSubClass.find('option[value=Tax]').prop('disabled', false).prop('hidden', false);
        $editFixedExpenditureSubClass.find('option[value=SocialInsurance]').prop('disabled', false).prop('hidden', false);
        $editFixedExpenditureSubClass.find('option[value=InterHouseholdTranserExpenses]').prop('disabled', false).prop('hidden', false);
        $editFixedExpenditureSubClass.find('option[value=NonProfitOrganizationTransfer]').prop('disabled', false).prop('hidden', false);

        $editFixedExpenditureSubClass.val("PublicPension").change();

        $divEditFixedExpenditureMyDepositAsset.show();
    }
    else if (editFixedExpenditureMainClass.value === "ConsumerSpending") {
        $editFixedExpenditureSubClass.find('option[value=Deposit]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=Investment]').prop('disabled', true).prop('hidden', true);

        $editFixedExpenditureSubClass.find('option[value=PublicPension]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=DebtRepayment]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=Tax]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=SocialInsurance]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=InterHouseholdTranserExpenses]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=NonProfitOrganizationTransfer]').prop('disabled', true).prop('hidden', true);

        $editFixedExpenditureSubClass.find('option[value=MealOrEatOutExpenses]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=HousingOrSuppliesCost]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=EducationExpenses]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=MedicalExpenses]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=TransportationCost]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=CommunicationCost]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=LeisureOrCulture]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=ClothingOrShoes]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=PinMoney]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=ProtectionTypeInsurance]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=OtherExpenses]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureSubClass.find('option[value=UnknownExpenditure]').prop('disabled', true).prop('hidden', true);

        $editFixedExpenditureSubClass.find('option:selected').removeAttr("selected");

        $editFixedExpenditureSubClass.find('option[value=MealOrEatOutExpenses]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $editFixedExpenditureSubClass.find('option[value=HousingOrSuppliesCost]').prop('disabled', false).prop('hidden', false);
        $editFixedExpenditureSubClass.find('option[value=EducationExpenses]').prop('disabled', false).prop('hidden', false);
        $editFixedExpenditureSubClass.find('option[value=MedicalExpenses]').prop('disabled', false).prop('hidden', false);
        $editFixedExpenditureSubClass.find('option[value=TransportationCost]').prop('disabled', false).prop('hidden', false);
        $editFixedExpenditureSubClass.find('option[value=CommunicationCost]').prop('disabled', false).prop('hidden', false);
        $editFixedExpenditureSubClass.find('option[value=LeisureOrCulture]').prop('disabled', false).prop('hidden', false);
        $editFixedExpenditureSubClass.find('option[value=ClothingOrShoes]').prop('disabled', false).prop('hidden', false);
        $editFixedExpenditureSubClass.find('option[value=PinMoney]').prop('disabled', false).prop('hidden', false);
        $editFixedExpenditureSubClass.find('option[value=ProtectionTypeInsurance]').prop('disabled', false).prop('hidden', false);
        $editFixedExpenditureSubClass.find('option[value=OtherExpenses]').prop('disabled', false).prop('hidden', false);
        $editFixedExpenditureSubClass.find('option[value=UnknownExpenditure]').prop('disabled', false).prop('hidden', false);

        $editFixedExpenditureSubClass.val("MealOrEatOutExpenses").change();

        $divEditFixedExpenditureMyDepositAsset.hide();
    }
}

function EditFormShowFixedExpenditureDivCreateFixedExpenditureMyDepositAssetBySelectedFixedExpenditureSubClass(editFixedExpenditureSubClass) {

    if (editFixedExpenditureSubClass.value === "Deposit" ||
        editFixedExpenditureSubClass.value === "Investment" ||
        editFixedExpenditureSubClass.value === "PublicPension" ||
        editFixedExpenditureSubClass.value === "DebtRepayment") {
        $divEditFixedExpenditureMyDepositAsset.show();
    }
    else {
        $divEditFixedExpenditureMyDepositAsset.hide();
    }
}

function EditFormShowFixedExpenditureDepositDayBySelectedFixedExpenditureDepositMonth(editFixedExpenditureDepositMonth) {

    if (editFixedExpenditureDepositMonth.value === "1" ||
        editFixedExpenditureDepositMonth.value === "3" ||
        editFixedExpenditureDepositMonth.value === "5" ||
        editFixedExpenditureDepositMonth.value === "7" ||
        editFixedExpenditureDepositMonth.value === "8" ||
        editFixedExpenditureDepositMonth.value === "10" ||
        editFixedExpenditureDepositMonth.value === "12"
    ) {
        $editFixedExpenditureDepositDay.find('option[value=29]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureDepositDay.find('option[value=30]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureDepositDay.find('option[value=31]').prop('disabled', true).prop('hidden', true);

        $editFixedExpenditureDepositDay.find('option:selected').removeAttr("selected");

        $editFixedExpenditureDepositDay.find('option[value=1]').attr('selected', 'selected');
        $editFixedExpenditureDepositDay.find('option[value=29]').prop('disabled', false).prop('hidden', false);
        $editFixedExpenditureDepositDay.find('option[value=30]').prop('disabled', false).prop('hidden', false);
        $editFixedExpenditureDepositDay.find('option[value=31]').prop('disabled', false).prop('hidden', false);

        $editFixedExpenditureDepositDay.val("1").change();
    }
    else if (editFixedExpenditureDepositMonth.value === "2") {
        $editFixedExpenditureDepositDay.find('option[value=29]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureDepositDay.find('option[value=30]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureDepositDay.find('option[value=31]').prop('disabled', true).prop('hidden', true);

        $editFixedExpenditureDepositDay.find('option:selected').removeAttr("selected");

        $editFixedExpenditureDepositDay.find('option[value=1]').attr('selected', 'selected');
        $editFixedExpenditureDepositDay.find('option[value=29]').prop('disabled', false).prop('hidden', false);

        $editFixedExpenditureDepositDay.val("1").change();
    }
    else if (editFixedExpenditureDepositMonth.value === "4" ||
        editFixedExpenditureDepositMonth.value === "6" ||
        editFixedExpenditureDepositMonth.value === "9" ||
        editFixedExpenditureDepositMonth.value === "11"
    ) {
        $editFixedExpenditureDepositDay.find('option[value=29]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureDepositDay.find('option[value=30]').prop('disabled', true).prop('hidden', true);
        $editFixedExpenditureDepositDay.find('option[value=31]').prop('disabled', true).prop('hidden', true);

        $editFixedExpenditureDepositDay.find('option:selected').removeAttr("selected");

        $editFixedExpenditureDepositDay.find('option[value=1]').attr('selected', 'selected');
        $editFixedExpenditureDepositDay.find('option[value=29]').prop('disabled', false).prop('hidden', false);
        $editFixedExpenditureDepositDay.find('option[value=30]').prop('disabled', false).prop('hidden', false);

        $editFixedExpenditureDepositDay.val("1").change();
    }
}

function CreateFixedExpenditure() {

    if (!$formCreateFixedExpenditure.valid()) {
        return false;
    }

    let mainClass = $createFixedExpenditureMainClass.val();
    let subClass = $createFixedExpenditureSubClass.val();
    let content = $createFixedExpenditureContent.val();
    let amount = $createFixedExpenditureAmount.val();
    let depositMonth = $createFixedExpenditureDepositMonth.val();
    let depositDay = $createFixedExpenditureDepositDay.val();
    let maturityDate = $createFixedExpenditureMaturityDate.val();
    let note = $createFixedExpenditureNote.val();
    let paymentMethod = $createFixedExpenditurePaymentMethod.val();
    let myDepositAsset = $createFixedExpenditureMyDepositAsset.val();

    let paramValue = JSON.stringify({
        MainClass: mainClass,
        SubClass: subClass,
        Content: content,
        Amount: amount,
        DepositMonth: depositMonth,
        DepositDay: depositDay,
        MaturityDate: maturityDate,
        Note: note,
        PaymentMethod: paymentMethod,
        MyDepositAsset: myDepositAsset
    });

    $.ajax({
        url: '/Notice/CreateFixedExpenditure',
        type: 'POST',
        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
        dataType: 'json',
        data: paramValue,
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            if (data.result) {
                $createFixedExpenditureDialogModal.modal('hide');

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

function EditFixedExpenditureGridRow(errorMessageSelectGridRow) {

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
        url: '/Notice/IsFixedExpenditureExists' + '?id=' + selectedRowId,
        type: 'POST',
        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            if (data.result) {

                $editFixedExpenditureId.val(data.fixedExpenditure.id);
                $editFixedExpenditureMainClass.val(data.fixedExpenditure.mainClass).change();

                if (data.fixedExpenditure.mainClass === "RegularSavings") {
                    $editFixedExpenditureSubClass.find('option[value=Deposit]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=Investment]').prop('disabled', true).prop('hidden', true);

                    $editFixedExpenditureSubClass.find('option[value=PublicPension]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=DebtRepayment]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=Tax]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=SocialInsurance]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=InterHouseholdTranserExpenses]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=NonProfitOrganizationTransfer]').prop('disabled', true).prop('hidden', true);

                    $editFixedExpenditureSubClass.find('option[value=MealOrEatOutExpenses]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=HousingOrSuppliesCost]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=EducationExpenses]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=MedicalExpenses]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=TransportationCost]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=CommunicationCost]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=LeisureOrCulture]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=ClothingOrShoes]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=PinMoney]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=ProtectionTypeInsurance]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=OtherExpenses]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=UnknownExpenditure]').prop('disabled', true).prop('hidden', true);

                    $editFixedExpenditureSubClass.find('option:selected').removeAttr("selected");

                    $editFixedExpenditureSubClass.find('option[value=Deposit]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureSubClass.find('option[value=Investment]').prop('disabled', false).prop('hidden', false);

                    $editFixedExpenditureSubClass.val(data.fixedExpenditure.subClass).change();
                }
                else if (data.fixedExpenditure.mainClass === "NonConsumerSpending") {
                    $editFixedExpenditureSubClass.find('option[value=Deposit]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=Investment]').prop('disabled', true).prop('hidden', true);

                    $editFixedExpenditureSubClass.find('option[value=PublicPension]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=DebtRepayment]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=Tax]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=SocialInsurance]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=InterHouseholdTranserExpenses]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=NonProfitOrganizationTransfer]').prop('disabled', true).prop('hidden', true);

                    $editFixedExpenditureSubClass.find('option[value=MealOrEatOutExpenses]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=HousingOrSuppliesCost]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=EducationExpenses]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=MedicalExpenses]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=TransportationCost]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=CommunicationCost]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=LeisureOrCulture]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=ClothingOrShoes]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=PinMoney]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=ProtectionTypeInsurance]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=OtherExpenses]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=UnknownExpenditure]').prop('disabled', true).prop('hidden', true);

                    $editFixedExpenditureSubClass.find('option:selected').removeAttr("selected");

                    $editFixedExpenditureSubClass.find('option[value=PublicPension]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureSubClass.find('option[value=DebtRepayment]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureSubClass.find('option[value=Tax]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureSubClass.find('option[value=SocialInsurance]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureSubClass.find('option[value=InterHouseholdTranserExpenses]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureSubClass.find('option[value=NonProfitOrganizationTransfer]').prop('disabled', false).prop('hidden', false);

                    $editFixedExpenditureSubClass.val(data.fixedExpenditure.subClass).change();
                }
                else if (data.fixedExpenditure.mainClass === "ConsumerSpending") {
                    $editFixedExpenditureSubClass.find('option[value=Deposit]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=Investment]').prop('disabled', true).prop('hidden', true);

                    $editFixedExpenditureSubClass.find('option[value=PublicPension]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=DebtRepayment]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=Tax]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=SocialInsurance]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=InterHouseholdTranserExpenses]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=NonProfitOrganizationTransfer]').prop('disabled', true).prop('hidden', true);

                    $editFixedExpenditureSubClass.find('option[value=MealOrEatOutExpenses]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=HousingOrSuppliesCost]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=EducationExpenses]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=MedicalExpenses]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=TransportationCost]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=CommunicationCost]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=LeisureOrCulture]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=ClothingOrShoes]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=PinMoney]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=ProtectionTypeInsurance]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=OtherExpenses]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureSubClass.find('option[value=UnknownExpenditure]').prop('disabled', true).prop('hidden', true);

                    $editFixedExpenditureSubClass.find('option:selected').removeAttr("selected");

                    $editFixedExpenditureSubClass.find('option[value=MealOrEatOutExpenses]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureSubClass.find('option[value=HousingOrSuppliesCost]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureSubClass.find('option[value=EducationExpenses]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureSubClass.find('option[value=MedicalExpenses]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureSubClass.find('option[value=TransportationCost]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureSubClass.find('option[value=CommunicationCost]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureSubClass.find('option[value=LeisureOrCulture]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureSubClass.find('option[value=ClothingOrShoes]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureSubClass.find('option[value=PinMoney]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureSubClass.find('option[value=ProtectionTypeInsurance]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureSubClass.find('option[value=OtherExpenses]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureSubClass.find('option[value=UnknownExpenditure]').prop('disabled', false).prop('hidden', false);

                    $editFixedExpenditureSubClass.val(data.fixedExpenditure.subClass).change();
                }

                $editFixedExpenditureContent.val(data.fixedExpenditure.content);
                $editFixedExpenditureAmount.val(data.fixedExpenditure.amount);

                $editFixedExpenditureDepositMonth.val(data.fixedExpenditure.depositMonth.toString()).change();

                if (data.fixedExpenditure.depositMonth.toString() === "1" ||
                    data.fixedExpenditure.depositMonth.toString() === "3" ||
                    data.fixedExpenditure.depositMonth.toString() === "5" ||
                    data.fixedExpenditure.depositMonth.toString() === "7" ||
                    data.fixedExpenditure.depositMonth.toString() === "8" ||
                    data.fixedExpenditure.depositMonth.toString() === "10" ||
                    data.fixedExpenditure.depositMonth.toString() === "12"
                ) {
                    $editFixedExpenditureDepositDay.find('option[value=29]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureDepositDay.find('option[value=30]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureDepositDay.find('option[value=31]').prop('disabled', true).prop('hidden', true);

                    $editFixedExpenditureDepositDay.find('option:selected').removeAttr("selected");

                    $editFixedExpenditureDepositDay.find('option[value=29]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureDepositDay.find('option[value=30]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureDepositDay.find('option[value=31]').prop('disabled', false).prop('hidden', false);

                    $editFixedExpenditureDepositDay.val(data.fixedExpenditure.depositDay.toString()).change();
                }
                else if (data.fixedExpenditure.depositMonth.toString() === "2") {
                    $editFixedExpenditureDepositDay.find('option[value=29]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureDepositDay.find('option[value=30]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureDepositDay.find('option[value=31]').prop('disabled', true).prop('hidden', true);

                    $editFixedExpenditureDepositDay.find('option:selected').removeAttr("selected");

                    $editFixedExpenditureDepositDay.find('option[value=29]').prop('disabled', false).prop('hidden', false);

                    $editFixedExpenditureDepositDay.val(data.fixedExpenditure.depositDay.toString()).change();
                }
                else if (data.fixedExpenditure.depositMonth.toString() === "4" ||
                    data.fixedExpenditure.depositMonth.toString() === "6" ||
                    data.fixedExpenditure.depositMonth.toString() === "9" ||
                    data.fixedExpenditure.depositMonth.toString() === "11"
                ) {
                    $editFixedExpenditureDepositDay.find('option[value=29]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureDepositDay.find('option[value=30]').prop('disabled', true).prop('hidden', true);
                    $editFixedExpenditureDepositDay.find('option[value=31]').prop('disabled', true).prop('hidden', true);

                    $editFixedExpenditureDepositDay.find('option:selected').removeAttr("selected");

                    $editFixedExpenditureDepositDay.find('option[value=29]').prop('disabled', false).prop('hidden', false);
                    $editFixedExpenditureDepositDay.find('option[value=30]').prop('disabled', false).prop('hidden', false);

                    $editFixedExpenditureDepositDay.val(data.fixedExpenditure.depositDay.toString()).change();
                }

                $editFixedExpenditureMaturityDate.val(data.fixedExpenditure.maturityDate);
                $editFixedExpenditureNote.val(data.fixedExpenditure.note);
                $editFixedExpenditurePaymentMethod.val(data.fixedExpenditure.paymentMethod).change();

                if (data.fixedExpenditure.myDepositAsset) {
                    $editFixedExpenditureMyDepositAsset.val(data.fixedExpenditure.myDepositAsset).change();
                }
                else {
                    $editFixedExpenditureMyDepositAsset.val(data.fixedExpenditure.paymentMethod).change();
                }

                if (data.fixedExpenditure.subClass === "Deposit" ||
                    data.fixedExpenditure.subClass === "Investment" ||
                    data.fixedExpenditure.subClass === "PublicPension" ||
                    data.fixedExpenditure.subClass === "DebtRepayment") {
                    $divEditFixedExpenditureMyDepositAsset.show();
                }
                else {
                    $divEditFixedExpenditureMyDepositAsset.hide();
                }

                $editFixedExpenditureUnpunctuality.prop("checked", data.fixedExpenditure.unpunctuality);

                $editFixedExpenditureDialogModal.modal({
                    keyboard: false,
                    backdrop: "static"
                });

                $editFixedExpenditureDialogModal.modal('toggle');
                $editFixedExpenditureDialogModal.modal('show');
            }
            else {
                toastr.error(data.error);
            }
        }
    });
}

function UpdateFixedExpenditure() {

    if (!$formEditFixedExpenditure.valid()) {
        return false;
    }

    let id = $editFixedExpenditureId.val();
    let mainClass = $editFixedExpenditureMainClass.val();
    let subClass = $editFixedExpenditureSubClass.val();
    let content = $editFixedExpenditureContent.val();
    let amount = $editFixedExpenditureAmount.val();
    let depositMonth = $editFixedExpenditureDepositMonth.val();
    let depositDay = $editFixedExpenditureDepositDay.val();
    let maturityDate = $editFixedExpenditureMaturityDate.val();
    let note = $editFixedExpenditureNote.val();
    let paymentMethod = $editFixedExpenditurePaymentMethod.val();
    let myDepositAsset = $editFixedExpenditureMyDepositAsset.val();
    let unpunctuality = $editFixedExpenditureUnpunctuality.is(":checked");

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
        PaymentMethod: paymentMethod,
        MyDepositAsset: myDepositAsset,
        Unpunctuality: unpunctuality
    });

    $.ajax({
        url: '/Notice/UpdateFixedExpenditure',
        type: 'POST',
        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
        dataType: 'json',
        data: paramValue,
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            if (data.result) {
                $editFixedExpenditureDialogModal.modal('hide');

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

function ConfirmDeleteFixedExpenditure(errorMessageSelectGridRow) {

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

    $confirmDeleteFixedExpenditureDialogModal.modal({
        keyboard: false,
        backdrop: "static"
    });

    $confirmDeleteFixedExpenditureDialogModal.modal('toggle');
    $confirmDeleteFixedExpenditureDialogModal.modal('show');
}

function DeleteFixedExpenditure(errorMessageSelectGridRow) {

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
        url: '/Notice/IsFixedExpenditureExists' + '?id=' + selectedRowId,
        type: 'POST',
        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            if (data.result) {

                let paramValue = JSON.stringify({
                    Id: data.fixedExpenditure.id
                });

                $.ajax({
                    url: '/Notice/DeleteFixedExpenditure',
                    type: 'POST',
                    headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
                    dataType: 'json',
                    data: paramValue,
                    contentType: 'application/json; charset=utf-8',
                    success: function (data) {
                        if (data.result) {
                            $confirmDeleteFixedExpenditureDialogModal.modal('hide');

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

function ExportExcelFixedExpenditure() {
    let form = document.createElement("form");
    let element1 = document.createElement("input");
    let element2 = document.createElement("input");

    form.method = "POST";
    form.action = "/Notice/ExportExcelFixedExpenditure";

    element1.name = "__RequestVerificationToken";
    element1.value = $__RequestVerificationToken.val();
    form.appendChild(element1);

    element2.name = "fileName";
    element2.value = "FixedExpenditure";
    form.appendChild(element2);

    document.body.appendChild(form);

    form.submit();
}

function ChangeCreateFixedExpenditureAmountLabel(productName) {
    $.ajax({
        url: '/Notice/GetFixedExpenditureAmountLabel' + '?productName=' + productName,
        type: 'POST',
        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            if (data.result) {
                $labelCreateFixedExpenditureAmount.text(data.label);
            }
            else {
                $labelCreateFixedExpenditureAmount.text(data.label);
            }
        }
    });
}

function ChangeEditFixedExpenditureAmountLabel(productName) {
    $.ajax({
        url: '/Notice/GetFixedExpenditureAmountLabel' + '?productName=' + productName,
        type: 'POST',
        headers: { "RequestVerificationToken": $__RequestVerificationToken.val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            if (data.result) {
                $labelEditFixedExpenditureAmount.text(data.label);
            }
            else {
                $labelEditFixedExpenditureAmount.text(data.label);
            }
        }
    });
}

$btnEditFixedExpenditureGridRow.off('click').on('click', function () {
    EditFixedExpenditureGridRow($(this).attr('data-errorMessageSelectGridRow'));
});

$btnConfirmDeleteFixedExpenditure.off('click').on('click', function () {
    ConfirmDeleteFixedExpenditure($(this).attr('data-errorMessageSelectGridRow'));
});

$btnExportExcelFixedExpenditure.off('click').on('click', function () {
    ExportExcelFixedExpenditure();
});

$btnDeleteFixedExpenditure.off('click').on('click', function () {
    DeleteFixedExpenditure($(this).attr('data-errorMessageSelectGridRow'));
});

$formCreateFixedExpenditure.off('submit').on('submit', function () {
    return CreateFixedExpenditure();
});

$formEditFixedExpenditure.off('submit').on('submit', function () {
    return UpdateFixedExpenditure();
});

$createFixedExpenditureMainClass.off('change').on('change', function () {
    return CreateFormShowFixedExpenditureSubClassBySelectedFixedExpenditureMainClass(this);
});

$createFixedExpenditureSubClass.off('change').on('change', function () {
    return CreateFormShowFixedExpenditureDivCreateFixedExpenditureMyDepositAssetBySelectedFixedExpenditureSubClass(this);
});

$createFixedExpenditureDepositMonth.off('change').on('change', function () {
    return CreateFormShowFixedExpenditureDepositDayBySelectedFixedExpenditureDepositMonth(this);
});

$editFixedExpenditureMainClass.off('change').on('change', function () {
    return EditFormShowFixedExpenditureSubClassBySelectedFixedExpenditureMainClass(this);
});

$editFixedExpenditureSubClass.off('change').on('change', function () {
    return EditFormShowFixedExpenditureDivCreateFixedExpenditureMyDepositAssetBySelectedFixedExpenditureSubClass(this);
});

$editFixedExpenditureDepositMonth.off('change').on('change', function () {
    return EditFormShowFixedExpenditureDepositDayBySelectedFixedExpenditureDepositMonth(this);
});

$createFixedExpenditurePaymentMethod.off('change').on('change', function () {
    ChangeCreateFixedExpenditureAmountLabel(this.value);
});

$editFixedExpenditurePaymentMethod.off('change').on('change', function () {
    ChangeEditFixedExpenditureAmountLabel(this.value);
});

$(function () {
    ChangeCreateFixedExpenditureAmountLabel($createFixedExpenditurePaymentMethod.find('option:selected').val());
})