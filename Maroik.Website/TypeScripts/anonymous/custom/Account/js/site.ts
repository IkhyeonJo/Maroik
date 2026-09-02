(function () {
    function ShowRegisterLoading() {
        if (!$('#registerForm').valid()) {
            $('#loading').hide();
            return false;
        }
        else {
            $('#loading').show();
            return true;
        }
    }

    function ShowForgotPasswordLoading() {
        if (!$('#forgotPasswordForm').valid()) {
            $('#loading').hide();
            return false;
        }
        else {
            $('#loading').show();
            return true;
        }
    }

    function ShowResetPasswordLoading() {
        if (!$('#resetPasswordForm').valid()) {
            $('#loading').hide();
            return false;
        }
        else {
            $('#loading').show();
            return true;
        }
    }

    function ShowLoginLoading() {
        if (!$('#loginForm').valid()) {
            $('#loading').hide();
            return false;
        }
        else {
            $('#loading').show();
            return true;
        }
    }

    function ChangeCulture(culture: string): void {

        let paramValue = JSON.stringify({
            Culture: culture
        });

        $.ajax({
            url: '/Dashboard/CultureManagement',
            type: 'POST',
            headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() as string },
            dataType: 'json',
            data: paramValue,
            contentType: 'application/json; charset=utf-8',
            success: function () {
                window.location.href = $("#returnUri").val() as string;
            }
        });
    }

    $('#btnRequestNewPassword').off('click').on('click', function () {
        ShowForgotPasswordLoading();
    });

    $('#aChangeCultureEnUS').off('click').on('click', function () {
        ChangeCulture('en-US');
    });

    $('#aChangeCultureKoKR').off('click').on('click', function () {
        ChangeCulture('ko-KR');
    });

    $('#btnSignIn').off('click').on('click', function () {
        ShowLoginLoading();
    });

    $('#btnRegister').off('click').on('click', function () {
        ShowRegisterLoading();
    });

    $('#btnResend').off('click').on('click', function () {
        ShowRegisterLoading();
    });

    $('#btnChangePassword').off('click').on('click', function () {
        ShowResetPasswordLoading();
    });
})();
