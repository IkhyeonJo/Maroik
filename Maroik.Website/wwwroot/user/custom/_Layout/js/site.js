$(document).off('ajaxError._Layout').on('ajaxError._Layout', function () {
    window.location.href = "/Dashboard/AnonymousIndex";
});

function ChangeCulture(culture) {

    let paramValue = JSON.stringify({
        Culture: culture
    });

    $.ajax({
        url: '/Dashboard/CultureManagement',
        type: 'POST',
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        dataType: 'json',
        data: paramValue,
        contentType: 'application/json; charset=utf-8',
        success: function (data) {
            if (data.result) {
                window.location.href = $('#returnUri').val();
            }
            else {
                window.location.href = $('#returnUri').val();
            }
        }
    });
}

$('#aChangeCultureEnUS').off('click').on('click', function () {
    ChangeCulture('en-US');
});

$('#aChangeCultureKoKR').off('click').on('click', function () {
    ChangeCulture('ko-KR');
});

// ===== Double-submit guard =====
// While a POST request is in flight, submit buttons stay disabled so a double-click
// cannot fire the same mutation twice. Buttons are re-enabled when the last
// in-flight POST completes (success, error, or abort).
let inFlightPostCount = 0;

$(document).off('ajaxSend._DoubleSubmitGuard').on('ajaxSend._DoubleSubmitGuard', function (_event, _xhr, settings) {
    if ((settings.type || 'GET').toUpperCase() !== 'POST') return;
    inFlightPostCount++;
    $('button[type="submit"], input[type="submit"]').prop('disabled', true);
});

$(document).off('ajaxComplete._DoubleSubmitGuard').on('ajaxComplete._DoubleSubmitGuard', function (_event, _xhr, settings) {
    if ((settings.type || 'GET').toUpperCase() !== 'POST') return;
    inFlightPostCount = Math.max(0, inFlightPostCount - 1);
    if (inFlightPostCount === 0) {
        $('button[type="submit"], input[type="submit"]').prop('disabled', false);
    }
});

// Full-page form posts (e.g. login): disable the submit button once the submit event
// is actually dispatched (after client-side validation passed). AJAX forms stop
// propagation via "return false", so they never reach this delegated handler and
// are covered by the in-flight guard above instead.
$(document).off('submit._DoubleSubmitGuard', 'form').on('submit._DoubleSubmitGuard', 'form', function () {
    $(this).find('button[type="submit"], input[type="submit"]').prop('disabled', true);
});

// Back/forward cache restores the DOM snapshot including disabled buttons and the
// #loading overlay (Account/Forum/PrivateNote pages) shown at submit time — reset both.
window.addEventListener('pageshow', function () {
    inFlightPostCount = 0;
    $('button[type="submit"], input[type="submit"]').prop('disabled', false);
    $('#loading').hide();
});