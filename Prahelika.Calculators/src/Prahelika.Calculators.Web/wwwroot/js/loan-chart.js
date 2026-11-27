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

    // Settings shared by both charts.
    function baseOptions(stacked) {
        return {
            responsive: true,
            maintainAspectRatio: false,
            interaction: { mode: "index", intersect: false },
            scales: {
                x: {
                    title: { display: true, text: "Month" },
                    ticks: { maxTicksLimit: 12 }
                },
                y: {
                    stacked: stacked,
                    beginAtZero: true,
                    ticks: {
                        callback: function (value) { return wholeRupees.format(value); }
                    }
                }
            },
            plugins: {
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

    new Chart(document.getElementById("balance-chart"), {
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
        },
        options: baseOptions(false)
    });

    new Chart(document.getElementById("split-chart"), {
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
        },
        options: baseOptions(true)
    });
})();