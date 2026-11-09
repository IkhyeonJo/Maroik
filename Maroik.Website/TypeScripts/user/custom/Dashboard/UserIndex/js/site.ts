/**
 * Script for the user **Dashboard** (`Views/Dashboard/UserIndex.cshtml`).
 *
 * It does two things:
 *
 *   1. Period / currency pickers — changing the year, month, or monetary-unit
 *      select reloads the dashboard for that period (the monetary-unit change
 *      first POSTs the new default so the server remembers it).
 *   2. Draws **10 Chart.js doughnut charts** — five spending/income categories,
 *      each shown for the whole year and for the selected month:
 *
 *        regular income · irregular income · regular savings ·
 *        non-consumer spending · consumer spending          × { Year, YearMonth }
 *
 * All the numbers are computed on the server and rendered into hidden inputs
 * (`#dashboardCharts`, `#dashboardChartLabels`). This script only reads those
 * inputs and plots them — no maths beyond `toLocaleString()` for display — to as
 * many decimal places as the server says an amount is stored with
 * (`#amountMaxDecimalPlaces`).
 *
 * IIFE-wrapped, no `import` / `export`.
 */
(function() {
    // The runtime-check helpers the _Layout script defines (see TypeScripts/global.d.ts).
    const { check, onReply, required, byId, fieldValue, parseJson } = window;

    // The replies this page reads, mirroring the controllers' Json(...) results (see window.replies).
    const resultReply = check.object({ result: check.boolean });
    // Authoritative in FinanceAmountPolicy.MaxDecimalPlaces (server): the stored scale of an amount, so a
    // tooltip shows 1234.5678 as "1,234.5678" instead of the browser default's three decimals.
    const amountMaxDecimalPlaces = parseInt(fieldValue($("#amountMaxDecimalPlaces")), 10);
    // The charts, computed on the server (one per main class and period, in the domain's display order).
    const charts = parseJson(fieldValue($("#dashboardCharts")), check.array(check.object({
        CanvasId: check.string,
        SubClasses: check.array(check.string),
        Amounts: check.array(check.number),
        Percentages: check.array(check.number)
    })), "the dashboard charts");
    // Localized category names, rendered by the view from the resource files. Used as the chart slice labels below.
    const labels = new Map(parseJson(fieldValue($("#dashboardChartLabels")), check.array(check.object({
        Key: check.string,
        Label: check.string
    })), "the dashboard chart labels").map((label): [string, string] => [label.Key, label.Label]));
    // The slice colors: a chart of up to six slices takes the first, a longer one the second.
    const shortPalette = ["#f56954", "#00a65a", "#f39c12", "#00c0ef", "#3c8dbc", "#d2d6de"];
    const longPalette = ["#FF0000", "#FFA200", "#FBFF00", "#70FF00", "#00FFE0", "#003EFF", "#D500FF", "#813F3F", "#7D813F", "#3F815A", "#3F8180", "#3F4B81"];

    // --- Period / currency pickers -------------------------------------
    // Year / month changes just reload the dashboard for the new period.
    $("#year").off("change").on("change", function() {
        window.location.href = "/Dashboard/UserIndex?year=" + $("#year").find(":selected").val() + "&month=" + $("#month").find(":selected").val();
    });

    $("#month").off("change").on("change", function() {
        window.location.href = "/Dashboard/UserIndex?year=" + $("#year").find(":selected").val() + "&month=" + $("#month").find(":selected").val();
    });

    // Monetary-unit change first persists the choice, then reloads. Both `if`
    // branches reload, so a failed save just re-renders in the old currency.
    $("#monetaryUnit").off("change").on("change", function() {
        void $.ajax({
            url: "/Dashboard/UserUpdateDefaultMonetary",
            type: "POST",
            headers: { "RequestVerificationToken": fieldValue($("input[name=\"__RequestVerificationToken\"]")) },
            dataType: "json",
            data: JSON.stringify({
                DefaultMonetaryUnit: $("#monetaryUnit").find(":selected").val()
            }),
            contentType: "application/json; charset=utf-8",
            success: onReply(resultReply, function(data) {
                if (data.result) {
                    window.location.href = "/Dashboard/UserIndex?year=" + $("#year").find(":selected").val() + "&month=" + $("#month").find(":selected").val();
                } else {
                    window.location.href = "/Dashboard/UserIndex?year=" + $("#year").find(":selected").val() + "&month=" + $("#month").find(":selected").val();
                }
            })
        });
    });

    // --- Charts (drawn once the DOM is ready) -------------------------
    $(function() {
        for (const chart of charts) {
            const palette = chart.Amounts.length > shortPalette.length ? longPalette : shortPalette;
            new Chart(required(byId(chart.CanvasId, HTMLCanvasElement).getContext("2d"), `the 2D context of #${chart.CanvasId}`), {
                type: "doughnut",
                data: {
                    labels: chart.SubClasses.map((subClass) => required(labels.get(subClass), "the slice label")),
                    datasets: [{ data: chart.Amounts, backgroundColor: palette.slice(0, chart.Amounts.length) }]
                },
                options: {
                    maintainAspectRatio: false,
                    responsive: true,
                    legend: {
                        labels: {
                            fontColor: "#fff",
                            fontSize: 15
                        }
                    },
                    tooltips: {
                        callbacks: {
                            // Each slice as "Label: 1,234 (12.34%)", with the server-supplied percentage of that slice.
                            label: function(tooltipItem: Chart.ChartTooltipItem, data: Chart.ChartData) {
                                const index = required(tooltipItem.index, "the slice index");
                                const label = required(data.labels, "the chart labels")[index];
                                const value = required(chart.Amounts[index], "the slice value");
                                return `${label}: ${value.toLocaleString(undefined, { maximumFractionDigits: amountMaxDecimalPlaces })} (${required(chart.Percentages[index], "the slice percentage").toFixed(2)}%)`;
                            }
                        }
                    }
                }
            });
        }
    });
})();
