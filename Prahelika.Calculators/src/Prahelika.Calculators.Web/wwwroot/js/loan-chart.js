// Draws the two loan charts from the numbers the server wrote into the page.
(function () {
    "use strict";

    var dataElement = document.getElementById("loan-chart-data");
    if (!dataElement || typeof Chart === "undefined") {
        return;
    }

    var data = JSON.parse(dataElement.textContent);

    // Indian digit grouping where the browser supports it, for example 12,34,567.
    var wholeRupees = new Intl.NumberFormat("en-IN", { maximumFractionDigits: 0 });
    var exactRupees = new Intl.NumberFormat("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    var colors = {
        balance: "#198754",
        principal: "#0d6efd",
        interest: "#fd7e14"
    };

    // A chart is drawn on a canvas, so it does not follow the page's CSS by itself.
    // Text and grid lines take their colours from the page theme (light or dark), read when the options are built.
    function themeColors() {
        var styles = getComputedStyle(document.documentElement);
        return {
            text: styles.getPropertyValue("--bs-body-color").trim(),
            grid: styles.getPropertyValue("--bs-border-color").trim()
        };
    }

    // Settings shared by both charts.
    function baseOptions(stacked) {
        var theme = themeColors();

        return {
            responsive: true,
            maintainAspectRatio: false,
            color: theme.text,
            interaction: { mode: "index", intersect: false },
            scales: {
                x: {
                    title: { display: true, text: "Month", color: theme.text },
                    ticks: { maxTicksLimit: 12, color: theme.text },
                    grid: { color: theme.grid },
                    border: { color: theme.grid }
                },
                y: {
                    stacked: stacked,
                    beginAtZero: true,
                    ticks: {
                        color: theme.text,
                        callback: function (value) { return wholeRupees.format(value); }
                    },
                    grid: { color: theme.grid },
                    border: { color: theme.grid }
                }
            },
            plugins: {
                legend: { labels: { color: theme.text } },
                tooltip: {
                    callbacks: {
                        title: function (items) { return "Month " + items[0].label; },
                        label: function (context) {
                            return context.dataset.label + ": " + exactRupees.format(context.raw);
                        }
                    }
                }
            }
        };
    }

    // Every chart, with whether it is stacked, so its options can be rebuilt when the theme changes.
    var charts = [];

    function addChart(canvasId, config, stacked) {
        config.options = baseOptions(stacked);
        charts.push({ chart: new Chart(document.getElementById(canvasId), config), stacked: stacked });
    }

    // theme.js fires this event when the theme changes: build the options again with the new colours and redraw.
    document.addEventListener("themechange", function () {
        charts.forEach(function (entry) {
            entry.chart.options = baseOptions(entry.stacked);
            entry.chart.update();
        });
    });

    addChart("balance-chart", {
        type: "line",
        data: {
            labels: data.months,
            datasets: [{
                label: "Balance left",
                data: data.balance,
                borderColor: colors.balance,
                backgroundColor: colors.balance + "33",
                fill: true,
                pointRadius: 0,
                tension: 0.2
            }]
        }
    }, false);

    addChart("split-chart", {
        type: "line",
        data: {
            labels: data.months,
            datasets: [
                {
                    label: "Principal",
                    data: data.principal,
                    borderColor: colors.principal,
                    backgroundColor: colors.principal + "99",
                    fill: "origin",
                    pointRadius: 0
                },
                {
                    label: "Interest",
                    data: data.interest,
                    borderColor: colors.interest,
                    backgroundColor: colors.interest + "99",
                    fill: "-1",
                    pointRadius: 0
                }
            ]
        }
    }, true);
})();