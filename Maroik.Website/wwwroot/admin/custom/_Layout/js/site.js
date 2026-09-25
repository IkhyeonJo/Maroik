"use strict";
(function () {
    window.MaroikDefaultMaxAttachedFileSizeBytes = 10485760;
    window.escapeHtml = function (value) {
        return value.replace(/[&<>"']/g, function (ch) {
            switch (ch) {
                case "&":
                    return "&amp;";
                case "<":
                    return "&lt;";
                case ">":
                    return "&gt;";
                case "\"":
                    return "&quot;";
                default:
                    return "&#39;";
            }
        });
    };
    $(document).off("ajaxError._Layout").on("ajaxError._Layout", function () {
        window.location.href = "/Dashboard/AnonymousIndex";
    });
    function ChangeCulture(culture) {
        let paramValue = JSON.stringify({
            Culture: culture
        });
        $.ajax({
            url: "/Dashboard/CultureManagement",
            type: "POST",
            headers: { "RequestVerificationToken": $("input[name=\"__RequestVerificationToken\"]").val() },
            dataType: "json",
            data: paramValue,
            contentType: "application/json; charset=utf-8",
            success: function () {
                window.location.href = $("#returnUri").val();
            }
        });
    }
    $("#aChangeCultureEnUS").off("click").on("click", function () {
        ChangeCulture("en-US");
    });
    $("#aChangeCultureKoKR").off("click").on("click", function () {
        ChangeCulture("ko-KR");
    });
    let inFlightPostCount = 0;
    $(document).off("ajaxSend._Layout").on("ajaxSend._Layout", function (_event, _xhr, settings) {
        if ((settings.type || "GET").toUpperCase() !== "POST")
            return;
        inFlightPostCount++;
        $("button[type=\"submit\"], input[type=\"submit\"]").prop("disabled", true);
    });
    $(document).off("ajaxComplete._Layout").on("ajaxComplete._Layout", function (_event, _xhr, settings) {
        if ((settings.type || "GET").toUpperCase() !== "POST")
            return;
        inFlightPostCount = Math.max(0, inFlightPostCount - 1);
        if (inFlightPostCount === 0) {
            $("button[type=\"submit\"], input[type=\"submit\"]").prop("disabled", false);
        }
    });
    $(document).off("submit._Layout", "form").on("submit._Layout", "form", function () {
        $(this).find("button[type=\"submit\"], input[type=\"submit\"]").prop("disabled", true);
    });
    $(window).off("pageshow._Layout").on("pageshow._Layout", function () {
        inFlightPostCount = 0;
        $("button[type=\"submit\"], input[type=\"submit\"]").prop("disabled", false);
        $("#loading").hide();
    });
})();
