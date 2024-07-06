// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

let selectedRowColor = "table-primary";

// Triggered when grid's row is clicked. It's recommended to use event delegation in ajax scenarios.
document.addEventListener("rowclick", e => {
    //e.detail.data - clicked row's data from columns.
    //e.detail.grid - grid's instance.
    //e.detail.originalEvent - original tr click event which triggered the rowclick.

    let selectedRow = e.detail;
    let selectedRowId = selectedRow.data.Id;

    $('#expenditureGrid > table > tbody  > tr').each(function (index, tr) {
        $(this).removeClass(selectedRowColor);
    }); // remove all selected row color

    $('#expenditureGrid > table > tbody  > tr').each(function (index, tr) {
        if ((new String(selectedRowId).valueOf() === new String($(this).attr('data-id')).valueOf())) {
            $(this).addClass(selectedRowColor);
        }
    }); // set selected row color class
});

// Triggered before grid starts loading. It's recommended to use event delegation in ajax scenarios.
document.addEventListener("reloadstart", e => {
    //e.detail.grid - grid's instance.
});

// Triggered after grid stop loading. It's recommended to use event delegation in ajax scenarios.
document.addEventListener("reloadend", e => {
    //e.detail.grid - grid's instance.
});

// Triggered after grid reload fails. It's recommended to use event delegation in ajax scenarios.
document.addEventListener("reloadfail", e => {
    //e.detail.reason - failed response promise reason.
    //e.detail.grid - grid's instance.

    // Preventing default stops failed promise from bubbling out.
});

// Triggered after grid configuration changes. It's recommended to use event delegation in ajax scenarios.
document.addEventListener("gridconfigure", e => {
    //e.detail.grid - grid's instance.
});


$(document).on("dblclick", "#expenditureGrid > table > tbody  > tr", function () {
    //console.log('dblclick-start');
    $('#expenditureGrid > table > tbody  > tr').each(function (index, tr) {

        if ($(this).attr("class").includes(selectedRowColor)) { // Get SelectedRow by Color
            EditExpenditureGridRow();
        }
    });
    //console.log('dblclick-end');
});
$('#gridSearch').on('input', function () {
    const grid = new MvcGrid(document.querySelector(".mvc-grid"));

    grid.url.searchParams.set("wholeSearch", this.value);

    grid.reload();
});

function CreateFormShowExpenditureSubClassBySelectedExpenditureMainClass(createExpenditureMainClass) {

    if (createExpenditureMainClass.value === "RegularSavings") {
        $('#createExpenditureSubClass option[value=Deposit]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=Investment]').prop('disabled', true).prop('hidden', true);

        $('#createExpenditureSubClass option[value=PublicPension]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=DebtRepayment]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=Tax]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=SocialInsurance]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=InterHouseholdTranserExpenses]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=NonProfitOrganizationTransfer]').prop('disabled', true).prop('hidden', true);

        $('#createExpenditureSubClass option[value=MealOrEatOutExpenses]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=HousingOrSuppliesCost]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=EducationExpenses]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=MedicalExpenses]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=TransportationCost]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=CommunicationCost]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=LeisureOrCulture]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=ClothingOrShoes]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=PinMoney]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=ProtectionTypeInsurance]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=OtherExpenses]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=UnknownExpenditure]').prop('disabled', true).prop('hidden', true);

        $("#createExpenditureSubClass option:selected").removeAttr("selected");

        $('#createExpenditureSubClass option[value=Deposit]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $('#createExpenditureSubClass option[value=Investment]').prop('disabled', false).prop('hidden', false);

        $("#createExpenditureSubClass").val("Deposit").change();

        $("#divCreateExpenditureMyDepositAsset").show();
    }
    else if (createExpenditureMainClass.value === "NonConsumerSpending") {
        $('#createExpenditureSubClass option[value=Deposit]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=Investment]').prop('disabled', true).prop('hidden', true);

        $('#createExpenditureSubClass option[value=PublicPension]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=DebtRepayment]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=Tax]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=SocialInsurance]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=InterHouseholdTranserExpenses]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=NonProfitOrganizationTransfer]').prop('disabled', true).prop('hidden', true);

        $('#createExpenditureSubClass option[value=MealOrEatOutExpenses]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=HousingOrSuppliesCost]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=EducationExpenses]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=MedicalExpenses]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=TransportationCost]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=CommunicationCost]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=LeisureOrCulture]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=ClothingOrShoes]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=PinMoney]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=ProtectionTypeInsurance]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=OtherExpenses]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=UnknownExpenditure]').prop('disabled', true).prop('hidden', true);

        $("#createExpenditureSubClass option:selected").removeAttr("selected");

        $('#createExpenditureSubClass option[value=PublicPension]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $('#createExpenditureSubClass option[value=DebtRepayment]').prop('disabled', false).prop('hidden', false);
        $('#createExpenditureSubClass option[value=Tax]').prop('disabled', false).prop('hidden', false);
        $('#createExpenditureSubClass option[value=SocialInsurance]').prop('disabled', false).prop('hidden', false);
        $('#createExpenditureSubClass option[value=InterHouseholdTranserExpenses]').prop('disabled', false).prop('hidden', false);
        $('#createExpenditureSubClass option[value=NonProfitOrganizationTransfer]').prop('disabled', false).prop('hidden', false);

        $("#createExpenditureSubClass").val("PublicPension").change();

        $("#divCreateExpenditureMyDepositAsset").show();
    }
    else if (createExpenditureMainClass.value === "ConsumerSpending") {
        $('#createExpenditureSubClass option[value=Deposit]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=Investment]').prop('disabled', true).prop('hidden', true);

        $('#createExpenditureSubClass option[value=PublicPension]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=DebtRepayment]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=Tax]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=SocialInsurance]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=InterHouseholdTranserExpenses]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=NonProfitOrganizationTransfer]').prop('disabled', true).prop('hidden', true);

        $('#createExpenditureSubClass option[value=MealOrEatOutExpenses]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=HousingOrSuppliesCost]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=EducationExpenses]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=MedicalExpenses]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=TransportationCost]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=CommunicationCost]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=LeisureOrCulture]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=ClothingOrShoes]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=PinMoney]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=ProtectionTypeInsurance]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=OtherExpenses]').prop('disabled', true).prop('hidden', true);
        $('#createExpenditureSubClass option[value=UnknownExpenditure]').prop('disabled', true).prop('hidden', true);

        $("#createExpenditureSubClass option:selected").removeAttr("selected");

        $('#createExpenditureSubClass option[value=MealOrEatOutExpenses]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $('#createExpenditureSubClass option[value=HousingOrSuppliesCost]').prop('disabled', false).prop('hidden', false);
        $('#createExpenditureSubClass option[value=EducationExpenses]').prop('disabled', false).prop('hidden', false);
        $('#createExpenditureSubClass option[value=MedicalExpenses]').prop('disabled', false).prop('hidden', false);
        $('#createExpenditureSubClass option[value=TransportationCost]').prop('disabled', false).prop('hidden', false);
        $('#createExpenditureSubClass option[value=CommunicationCost]').prop('disabled', false).prop('hidden', false);
        $('#createExpenditureSubClass option[value=LeisureOrCulture]').prop('disabled', false).prop('hidden', false);
        $('#createExpenditureSubClass option[value=ClothingOrShoes]').prop('disabled', false).prop('hidden', false);
        $('#createExpenditureSubClass option[value=PinMoney]').prop('disabled', false).prop('hidden', false);
        $('#createExpenditureSubClass option[value=ProtectionTypeInsurance]').prop('disabled', false).prop('hidden', false);
        $('#createExpenditureSubClass option[value=OtherExpenses]').prop('disabled', false).prop('hidden', false);
        $('#createExpenditureSubClass option[value=UnknownExpenditure]').prop('disabled', false).prop('hidden', false);

        $("#createExpenditureSubClass").val("MealOrEatOutExpenses").change();

        $("#divCreateExpenditureMyDepositAsset").hide();
    }
}

function CreateFormShowExpenditureDivCreateExpenditureMyDepositAssetBySelectedExpenditureSubClass(createExpenditureSubClass) {

    if (createExpenditureSubClass.value === "Deposit" ||
        createExpenditureSubClass.value === "Investment" ||
        createExpenditureSubClass.value === "PublicPension" ||
        createExpenditureSubClass.value === "DebtRepayment") {
        $("#divCreateExpenditureMyDepositAsset").show();
    }
    else {
        $("#divCreateExpenditureMyDepositAsset").hide();
    }
}

function CreateExpenditure() {

    if (!$('#formCreateExpenditure').valid()) {
        return false;
    }

    let createForm = $('#formCreateExpenditure');

    let mainClass = createForm.find('select[id="createExpenditureMainClass"]').val();
    let subClass = createForm.find('select[id="createExpenditureSubClass"]').val();
    let content = createForm.find('input[id="createExpenditureContent"]').val();
    let amount = createForm.find('input[id="createExpenditureAmount"]').val();

    let year = parseInt(createForm.find('input[id="createExpenditureDate"]').val().substring(0, 4));
    let month = parseInt(createForm.find('input[id="createExpenditureDate"]').val().substring(5, 7));
    let day = parseInt(createForm.find('input[id="createExpenditureDate"]').val().substring(8, 10));
    let hour = parseInt(createForm.find('select[id="createExpenditureHour"]').val());
    let minute = parseInt(createForm.find('select[id="createExpenditureMinute"]').val());
    let second = parseInt(createForm.find('select[id="createExpenditureSecond"]').val());

    let created = new Date(year, month - 1, day, hour, minute, second).toISOString();

    let paymentMethod = createForm.find('select[id="createExpenditurePaymentMethod"]').val();
    let note = createForm.find('input[id="createExpenditureNote"]').val();
    let myDepositAsset = createForm.find('select[id="createExpenditureMyDepositAsset"]').val();

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
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: paramValue,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {
                $('#createExpenditureDialogModal').modal('hide'); // hide Modal

                const grid = new MvcGrid(document.querySelector(".mvc-grid")); //reload grid
                grid.reload(); //reload grid

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

    $('#expenditureGrid > table > tbody  > tr').each(function (index, tr) {
        if ($(this).attr("class").includes(selectedRowColor)) { // Get SelectedRow by Color
            selectedRowId = $(this).attr("data-id");
        }
    }); // Search Selected Row

    if (selectedRowId === "") {
        toastr.error(errorMessageSelectGridRow);
        return false;
    } // check whether selected row exist or not

    $.ajax({
        url: '/AccountBook/IsExpenditureExists' + '?id=' + selectedRowId,
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {

                let formEditExpenditure = $('#formEditExpenditure');

                formEditExpenditure.find('input[id="editExpenditureId"]').val(data.expenditure.id);
                formEditExpenditure.find('select[id="editExpenditureMainClass"]').val(data.expenditure.mainClass).change();

                if (data.expenditure.mainClass === "RegularSavings") {
                    $('#editExpenditureSubClass option[value=Deposit]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=Investment]').prop('disabled', true).prop('hidden', true);

                    $('#editExpenditureSubClass option[value=PublicPension]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=DebtRepayment]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=Tax]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=SocialInsurance]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=InterHouseholdTranserExpenses]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=NonProfitOrganizationTransfer]').prop('disabled', true).prop('hidden', true);

                    $('#editExpenditureSubClass option[value=MealOrEatOutExpenses]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=HousingOrSuppliesCost]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=EducationExpenses]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=MedicalExpenses]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=TransportationCost]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=CommunicationCost]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=LeisureOrCulture]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=ClothingOrShoes]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=PinMoney]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=ProtectionTypeInsurance]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=OtherExpenses]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=UnknownExpenditure]').prop('disabled', true).prop('hidden', true);

                    $("#editExpenditureSubClass option:selected").removeAttr("selected");

                    $('#editExpenditureSubClass option[value=Deposit]').prop('disabled', false).prop('hidden', false);
                    $('#editExpenditureSubClass option[value=Investment]').prop('disabled', false).prop('hidden', false);

                    formEditExpenditure.find('select[id="editExpenditureSubClass"]').val(data.expenditure.subClass).change();
                }
                else if (data.expenditure.mainClass === "NonConsumerSpending") {
                    $('#editExpenditureSubClass option[value=Deposit]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=Investment]').prop('disabled', true).prop('hidden', true);

                    $('#editExpenditureSubClass option[value=PublicPension]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=DebtRepayment]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=Tax]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=SocialInsurance]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=InterHouseholdTranserExpenses]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=NonProfitOrganizationTransfer]').prop('disabled', true).prop('hidden', true);

                    $('#editExpenditureSubClass option[value=MealOrEatOutExpenses]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=HousingOrSuppliesCost]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=EducationExpenses]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=MedicalExpenses]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=TransportationCost]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=CommunicationCost]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=LeisureOrCulture]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=ClothingOrShoes]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=PinMoney]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=ProtectionTypeInsurance]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=OtherExpenses]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=UnknownExpenditure]').prop('disabled', true).prop('hidden', true);

                    $("#editExpenditureSubClass option:selected").removeAttr("selected");

                    $('#editExpenditureSubClass option[value=PublicPension]').prop('disabled', false).prop('hidden', false);
                    $('#editExpenditureSubClass option[value=DebtRepayment]').prop('disabled', false).prop('hidden', false);
                    $('#editExpenditureSubClass option[value=Tax]').prop('disabled', false).prop('hidden', false);
                    $('#editExpenditureSubClass option[value=SocialInsurance]').prop('disabled', false).prop('hidden', false);
                    $('#editExpenditureSubClass option[value=InterHouseholdTranserExpenses]').prop('disabled', false).prop('hidden', false);
                    $('#editExpenditureSubClass option[value=NonProfitOrganizationTransfer]').prop('disabled', false).prop('hidden', false);

                    formEditExpenditure.find('select[id="editExpenditureSubClass"]').val(data.expenditure.subClass).change();
                }
                else if (data.expenditure.mainClass === "ConsumerSpending") {
                    $('#editExpenditureSubClass option[value=Deposit]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=Investment]').prop('disabled', true).prop('hidden', true);

                    $('#editExpenditureSubClass option[value=PublicPension]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=DebtRepayment]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=Tax]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=SocialInsurance]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=InterHouseholdTranserExpenses]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=NonProfitOrganizationTransfer]').prop('disabled', true).prop('hidden', true);

                    $('#editExpenditureSubClass option[value=MealOrEatOutExpenses]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=HousingOrSuppliesCost]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=EducationExpenses]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=MedicalExpenses]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=TransportationCost]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=CommunicationCost]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=LeisureOrCulture]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=ClothingOrShoes]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=PinMoney]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=ProtectionTypeInsurance]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=OtherExpenses]').prop('disabled', true).prop('hidden', true);
                    $('#editExpenditureSubClass option[value=UnknownExpenditure]').prop('disabled', true).prop('hidden', true);

                    $("#editExpenditureSubClass option:selected").removeAttr("selected");

                    $('#editExpenditureSubClass option[value=MealOrEatOutExpenses]').prop('disabled', false).prop('hidden', false);
                    $('#editExpenditureSubClass option[value=HousingOrSuppliesCost]').prop('disabled', false).prop('hidden', false);
                    $('#editExpenditureSubClass option[value=EducationExpenses]').prop('disabled', false).prop('hidden', false);
                    $('#editExpenditureSubClass option[value=MedicalExpenses]').prop('disabled', false).prop('hidden', false);
                    $('#editExpenditureSubClass option[value=TransportationCost]').prop('disabled', false).prop('hidden', false);
                    $('#editExpenditureSubClass option[value=CommunicationCost]').prop('disabled', false).prop('hidden', false);
                    $('#editExpenditureSubClass option[value=LeisureOrCulture]').prop('disabled', false).prop('hidden', false);
                    $('#editExpenditureSubClass option[value=ClothingOrShoes]').prop('disabled', false).prop('hidden', false);
                    $('#editExpenditureSubClass option[value=PinMoney]').prop('disabled', false).prop('hidden', false);
                    $('#editExpenditureSubClass option[value=ProtectionTypeInsurance]').prop('disabled', false).prop('hidden', false);
                    $('#editExpenditureSubClass option[value=OtherExpenses]').prop('disabled', false).prop('hidden', false);
                    $('#editExpenditureSubClass option[value=UnknownExpenditure]').prop('disabled', false).prop('hidden', false);

                    formEditExpenditure.find('select[id="editExpenditureSubClass"]').val(data.expenditure.subClass).change();
                }

                if (data.expenditure.subClass === "Deposit" ||
                    data.expenditure.subClass === "Investment" ||
                    data.expenditure.subClass === "PublicPension" ||
                    data.expenditure.subClass === "DebtRepayment") {
                    $("#divEditExpenditureMyDepositAsset").show();
                }
                else {
                    $("#divEditExpenditureMyDepositAsset").hide();
                }


                formEditExpenditure.find('input[id="editExpenditureContent"]').val(data.expenditure.content);
                formEditExpenditure.find('input[id="editExpenditureAmount"]').val(data.expenditure.amount);
                formEditExpenditure.find('input[id="editExpenditureDate"]').val(data.expenditure.created.split('T')[0]);
                formEditExpenditure.find('select[id="editExpenditureHour"]').val(parseInt(data.expenditure.created.split('T')[1].substring(0, 2))).change();
                formEditExpenditure.find('select[id="editExpenditureMinute"]').val(parseInt(data.expenditure.created.split('T')[1].substring(3, 5))).change();
                formEditExpenditure.find('select[id="editExpenditureSecond"]').val(parseInt(data.expenditure.created.split('T')[1].substring(6, 8))).change();
                formEditExpenditure.find('select[id="editExpenditurePaymentMethod"]').val(data.expenditure.paymentMethod).change();
                formEditExpenditure.find('input[id="editExpenditureNote"]').val(data.expenditure.note);
                if (data.expenditure.myDepositAsset) {
                    formEditExpenditure.find('select[id="editExpenditureMyDepositAsset"]').val(data.expenditure.myDepositAsset).change();
                }
                else {
                    formEditExpenditure.find('select[id="editExpenditureMyDepositAsset"]').val(data.expenditure.paymentMethod).change();
                }
                

                $('#editExpenditureDialogModal').modal({
                    keyboard: false,
                    backdrop: "static"
                });

                $('#editExpenditureDialogModal').modal('toggle');
                $('#editExpenditureDialogModal').modal('show');
            }
            else {
                toastr.error(data.error);
            }
        }
    });
}

function EditFormShowExpenditureSubClassBySelectedExpenditureMainClass(editExpenditureMainClass) {

    if (editExpenditureMainClass.value === "RegularSavings") {
        $('#editExpenditureSubClass option[value=Deposit]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=Investment]').prop('disabled', true).prop('hidden', true);

        $('#editExpenditureSubClass option[value=PublicPension]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=DebtRepayment]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=Tax]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=SocialInsurance]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=InterHouseholdTranserExpenses]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=NonProfitOrganizationTransfer]').prop('disabled', true).prop('hidden', true);

        $('#editExpenditureSubClass option[value=MealOrEatOutExpenses]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=HousingOrSuppliesCost]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=EducationExpenses]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=MedicalExpenses]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=TransportationCost]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=CommunicationCost]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=LeisureOrCulture]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=ClothingOrShoes]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=PinMoney]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=ProtectionTypeInsurance]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=OtherExpenses]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=UnknownExpenditure]').prop('disabled', true).prop('hidden', true);

        $("#editExpenditureSubClass option:selected").removeAttr("selected");

        $('#editExpenditureSubClass option[value=Deposit]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $('#editExpenditureSubClass option[value=Investment]').prop('disabled', false).prop('hidden', false);

        $("#editExpenditureSubClass").val("Deposit").change();

        $("#divEditExpenditureMyDepositAsset").show();
    }
    else if (editExpenditureMainClass.value === "NonConsumerSpending") {
        $('#editExpenditureSubClass option[value=Deposit]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=Investment]').prop('disabled', true).prop('hidden', true);

        $('#editExpenditureSubClass option[value=PublicPension]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=DebtRepayment]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=Tax]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=SocialInsurance]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=InterHouseholdTranserExpenses]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=NonProfitOrganizationTransfer]').prop('disabled', true).prop('hidden', true);

        $('#editExpenditureSubClass option[value=MealOrEatOutExpenses]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=HousingOrSuppliesCost]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=EducationExpenses]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=MedicalExpenses]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=TransportationCost]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=CommunicationCost]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=LeisureOrCulture]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=ClothingOrShoes]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=PinMoney]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=ProtectionTypeInsurance]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=OtherExpenses]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=UnknownExpenditure]').prop('disabled', true).prop('hidden', true);

        $("#editExpenditureSubClass option:selected").removeAttr("selected");

        $('#editExpenditureSubClass option[value=PublicPension]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $('#editExpenditureSubClass option[value=DebtRepayment]').prop('disabled', false).prop('hidden', false);
        $('#editExpenditureSubClass option[value=Tax]').prop('disabled', false).prop('hidden', false);
        $('#editExpenditureSubClass option[value=SocialInsurance]').prop('disabled', false).prop('hidden', false);
        $('#editExpenditureSubClass option[value=InterHouseholdTranserExpenses]').prop('disabled', false).prop('hidden', false);
        $('#editExpenditureSubClass option[value=NonProfitOrganizationTransfer]').prop('disabled', false).prop('hidden', false);

        $("#editExpenditureSubClass").val("PublicPension").change();

        $("#divEditExpenditureMyDepositAsset").show();
    }
    else if (editExpenditureMainClass.value === "ConsumerSpending") {
        $('#editExpenditureSubClass option[value=Deposit]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=Investment]').prop('disabled', true).prop('hidden', true);

        $('#editExpenditureSubClass option[value=PublicPension]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=DebtRepayment]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=Tax]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=SocialInsurance]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=InterHouseholdTranserExpenses]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=NonProfitOrganizationTransfer]').prop('disabled', true).prop('hidden', true);

        $('#editExpenditureSubClass option[value=MealOrEatOutExpenses]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=HousingOrSuppliesCost]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=EducationExpenses]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=MedicalExpenses]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=TransportationCost]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=CommunicationCost]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=LeisureOrCulture]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=ClothingOrShoes]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=PinMoney]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=ProtectionTypeInsurance]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=OtherExpenses]').prop('disabled', true).prop('hidden', true);
        $('#editExpenditureSubClass option[value=UnknownExpenditure]').prop('disabled', true).prop('hidden', true);

        $("#editExpenditureSubClass option:selected").removeAttr("selected");

        $('#editExpenditureSubClass option[value=MealOrEatOutExpenses]').prop('disabled', false).prop('hidden', false).attr('selected', 'selected');
        $('#editExpenditureSubClass option[value=HousingOrSuppliesCost]').prop('disabled', false).prop('hidden', false);
        $('#editExpenditureSubClass option[value=EducationExpenses]').prop('disabled', false).prop('hidden', false);
        $('#editExpenditureSubClass option[value=MedicalExpenses]').prop('disabled', false).prop('hidden', false);
        $('#editExpenditureSubClass option[value=TransportationCost]').prop('disabled', false).prop('hidden', false);
        $('#editExpenditureSubClass option[value=CommunicationCost]').prop('disabled', false).prop('hidden', false);
        $('#editExpenditureSubClass option[value=LeisureOrCulture]').prop('disabled', false).prop('hidden', false);
        $('#editExpenditureSubClass option[value=ClothingOrShoes]').prop('disabled', false).prop('hidden', false);
        $('#editExpenditureSubClass option[value=PinMoney]').prop('disabled', false).prop('hidden', false);
        $('#editExpenditureSubClass option[value=ProtectionTypeInsurance]').prop('disabled', false).prop('hidden', false);
        $('#editExpenditureSubClass option[value=OtherExpenses]').prop('disabled', false).prop('hidden', false);
        $('#editExpenditureSubClass option[value=UnknownExpenditure]').prop('disabled', false).prop('hidden', false);

        $("#editExpenditureSubClass").val("MealOrEatOutExpenses").change();

        $("#divEditExpenditureMyDepositAsset").hide();
    }
}

function EditFormShowExpenditureDivCreateExpenditureMyDepositAssetBySelectedExpenditureSubClass(editExpenditureSubClass) {

    if (editExpenditureSubClass.value === "Deposit" ||
        editExpenditureSubClass.value === "Investment" ||
        editExpenditureSubClass.value === "PublicPension" ||
        editExpenditureSubClass.value === "DebtRepayment") {
        $("#divEditExpenditureMyDepositAsset").show();
    }
    else {
        $("#divEditExpenditureMyDepositAsset").hide();
    }
}

function UpdateExpenditure() {

    if (!$('#formEditExpenditure').valid()) {
        return false;
    }

    let createForm = $('#formEditExpenditure');

    let id = createForm.find('input[id="editExpenditureId"]').val();
    let mainClass = createForm.find('select[id="editExpenditureMainClass"]').val();
    let subClass = createForm.find('select[id="editExpenditureSubClass"]').val();
    let content = createForm.find('input[id="editExpenditureContent"]').val();
    let amount = createForm.find('input[id="editExpenditureAmount"]').val();

    let year = parseInt(createForm.find('input[id="editExpenditureDate"]').val().substring(0, 4));
    let month = parseInt(createForm.find('input[id="editExpenditureDate"]').val().substring(5, 7));
    let day = parseInt(createForm.find('input[id="editExpenditureDate"]').val().substring(8, 10));
    let hour = parseInt(createForm.find('select[id="editExpenditureHour"]').val());
    let minute = parseInt(createForm.find('select[id="editExpenditureMinute"]').val());
    let second = parseInt(createForm.find('select[id="editExpenditureSecond"]').val());

    let created = new Date(year, month - 1, day, hour, minute, second).toISOString();
    let paymentMethod = createForm.find('select[id="editExpenditurePaymentMethod"]').val();
    let note = createForm.find('input[id="editExpenditureNote"]').val();
    let myDepositAsset = createForm.find('select[id="editExpenditureMyDepositAsset"]').val();

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
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: paramValue,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {
                $('#editExpenditureDialogModal').modal('hide'); // hide Modal

                const grid = new MvcGrid(document.querySelector(".mvc-grid")); //reload grid
                grid.reload(); //reload grid

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

    $('#expenditureGrid > table > tbody  > tr').each(function (index, tr) {
        if ($(this).attr("class").includes(selectedRowColor)) { // Get SelectedRow by Color
            selectedRowId = $(this).attr("data-id");
        }
    }); // Search Selected Row

    if (selectedRowId === "") {
        toastr.error(errorMessageSelectGridRow);
        return false;
    } // check whether selected row exist or not

    $('#confirmDeleteExpenditureDialogModal').modal({
        keyboard: false,
        backdrop: "static"
    });

    $('#confirmDeleteExpenditureDialogModal').modal('toggle');
    $('#confirmDeleteExpenditureDialogModal').modal('show');
}

function DeleteExpenditure(errorMessageSelectGridRow) {

    let selectedRowId = "";

    $('#expenditureGrid > table > tbody  > tr').each(function (index, tr) {
        if ($(this).attr("class").includes(selectedRowColor)) { // Get SelectedRow by Color
            selectedRowId = $(this).attr("data-id");
        }
    }); // Search Selected Row

    if (selectedRowId === "") {
        toastr.error(errorMessageSelectGridRow);
        return false;
    } // check whether selected row exist or not

    $.ajax({
        url: '/AccountBook/IsExpenditureExists' + '?id=' + selectedRowId,
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {

                let paramValue = JSON.stringify({
                    Id: data.expenditure.id
                });

                $.ajax({
                    url: '/AccountBook/DeleteExpenditure',
                    type: 'POST',
                    headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
                    dataType: 'json',
                    data: paramValue,
                    contentType: 'application/json; charset=utf-8',
                    //async: false,
                    //cache: false,
                    success: function (data) {
                        if (data.result) {
                            $('#confirmDeleteExpenditureDialogModal').modal('hide'); // hide Modal

                            const grid = new MvcGrid(document.querySelector(".mvc-grid")); //reload grid
                            grid.reload(); //reload grid

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
    element1.value = $('input[name="__RequestVerificationToken"]').val();
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
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {
                $('#labelCreateExpenditureAmount').text(data.label);
            }
            else {
                $('#labelCreateExpenditureAmount').text(data.label);
            }
        }
    });
}

function ChangeEditExpenditureAmountLabel(productName) {
    $.ajax({
        url: '/AccountBook/GetExpenditureAmountLabel' + '?productName=' + productName,
        type: 'POST',
        headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: null,
        contentType: 'application/json; charset=utf-8',
        //async: false,
        //cache: false,
        success: function (data) {
            if (data.result) {
                $('#labelEditExpenditureAmount').text(data.label);
            }
            else {
                $('#labelEditExpenditureAmount').text(data.label);
            }
        }
    });
}

$('#btnEditExpenditureGridRow').off('click').on('click', function () {
    EditExpenditureGridRow($(this).attr("data-errorMessageSelectGridRow"));
});

$('#btnConfirmDeleteExpenditure').off('click').on('click', function () {
    ConfirmDeleteExpenditure($(this).attr("data-errorMessageSelectGridRow"));
});

$('#btnExportExcelExpenditure').off('click').on('click', function () {
    ExportExcelExpenditure();
});

$('#btnDeleteExpenditure').off('click').on('click', function () {
    DeleteExpenditure($(this).attr("data-errorMessageSelectGridRow"));
});

$('#formCreateExpenditure').off('submit').on('submit', function () {
    return CreateExpenditure();
});

$('#formEditExpenditure').off('submit').on('submit', function () {
    return UpdateExpenditure();
});

$('#createExpenditureMainClass').off('change').on('change', function () {
    CreateFormShowExpenditureSubClassBySelectedExpenditureMainClass(this);
});

$('#createExpenditureSubClass').off('change').on('change', function () {
    CreateFormShowExpenditureDivCreateExpenditureMyDepositAssetBySelectedExpenditureSubClass(this);
});

$('#editExpenditureMainClass').off('change').on('change', function () {
    EditFormShowExpenditureSubClassBySelectedExpenditureMainClass(this);
});

$('#editExpenditureSubClass').off('change').on('change', function () {
    EditFormShowExpenditureDivCreateExpenditureMyDepositAssetBySelectedExpenditureSubClass(this);
});

$('#createExpenditurePaymentMethod').off('change').on('change', function () {
    ChangeCreateExpenditureAmountLabel(this.value);
});

$('#editExpenditurePaymentMethod').off('change').on('change', function () {
    ChangeEditExpenditureAmountLabel(this.value);
});

$(function () {
    ChangeCreateExpenditureAmountLabel($('#createExpenditurePaymentMethod option:selected').val());
})