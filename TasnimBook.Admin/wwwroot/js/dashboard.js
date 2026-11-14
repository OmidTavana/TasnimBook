(() => {
    const dashboard = document.querySelector("[data-dashboard]");

    if (!dashboard || typeof Chart === "undefined") {
        return;
    }

    const colors = {
        violet: "#8b5cf6",
        cyan: "#22d3ee",
        emerald: "#34d399",
        amber: "#fbbf24",
        rose: "#fb7185",
        grid: "rgba(148, 163, 184, 0.12)",
        text: "#94a3b8"
    };

    Chart.defaults.font.family = "Vazirmatn, Segoe UI, sans-serif";
    Chart.defaults.color = colors.text;
    Chart.defaults.borderColor = colors.grid;

    const contentData = JSON.parse(dashboard.dataset.contentStats || "[]");
    const engagementData = JSON.parse(dashboard.dataset.engagementStats || "[]");
    const popularBooks = JSON.parse(dashboard.dataset.popularBooks || "[]");

    createContentChart(contentData);
    createEngagementChart(engagementData);
    createPopularBooksChart(popularBooks);

    function createContentChart(items) {
        const canvas = document.getElementById("content-chart");

        if (!canvas) {
            return;
        }

        new Chart(canvas, {
            type: "doughnut",
            data: {
                labels: items.map((item) => item.label),
                datasets: [{
                    data: items.map((item) => item.value),
                    backgroundColor: [colors.violet, colors.cyan, colors.emerald],
                    borderWidth: 0,
                    hoverOffset: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: "72%",
                plugins: {
                    legend: {
                        position: "bottom",
                        labels: {
                            usePointStyle: true,
                            pointStyle: "circle",
                            padding: 18
                        }
                    }
                }
            }
        });
    }

    function createEngagementChart(items) {
        const canvas = document.getElementById("engagement-chart");

        if (!canvas) {
            return;
        }

        new Chart(canvas, {
            type: "bar",
            data: {
                labels: items.map((item) => item.label),
                datasets: [{
                    data: items.map((item) => item.value),
                    backgroundColor: [colors.cyan, colors.rose, colors.amber],
                    borderRadius: 10,
                    borderSkipped: false,
                    maxBarThickness: 48
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: { rtl: true }
                },
                scales: {
                    x: { grid: { display: false } },
                    y: {
                        beginAtZero: true,
                        ticks: { precision: 0 }
                    }
                }
            }
        });
    }

    function createPopularBooksChart(items) {
        const canvas = document.getElementById("popular-books-chart");

        if (!canvas || items.length === 0) {
            return;
        }

        new Chart(canvas, {
            type: "bar",
            data: {
                labels: items.map((item) => item.title),
                datasets: [{
                    label: "امتیاز محبوبیت",
                    data: items.map((item) => item.value),
                    backgroundColor: "rgba(139, 92, 246, 0.72)",
                    hoverBackgroundColor: colors.violet,
                    borderRadius: 8,
                    borderSkipped: false
                }]
            },
            options: {
                indexAxis: "y",
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: { rtl: true }
                },
                scales: {
                    x: {
                        beginAtZero: true,
                        grid: { color: colors.grid }
                    },
                    y: { grid: { display: false } }
                }
            }
        });
    }
})();
