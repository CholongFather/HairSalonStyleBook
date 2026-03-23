// 매출 차트 (Chart.js) JS 인터롭
window.revenueChart = {
    _instance: null,

    // 연도별 색상 팔레트
    _colors: [
        { bg: 'rgba(123, 163, 131, 0.8)', border: 'rgba(123, 163, 131, 1)' },   // 살롱 그린
        { bg: 'rgba(44, 44, 44, 0.3)', border: 'rgba(44, 44, 44, 0.6)' },        // 살롱 블랙
        { bg: 'rgba(91, 143, 185, 0.6)', border: 'rgba(91, 143, 185, 1)' },      // 블루
        { bg: 'rgba(200, 150, 80, 0.6)', border: 'rgba(200, 150, 80, 1)' },      // 골드
        { bg: 'rgba(180, 100, 130, 0.6)', border: 'rgba(180, 100, 130, 1)' },    // 로즈
    ],

    // 차트 생성 (동적 연도 수 지원)
    // datasets: [{ label: "2026년", data: [0,0,...12개] }, ...]
    render: function (canvasId, labels, datasets) {
        var canvas = document.getElementById(canvasId);
        if (!canvas) return;

        if (this._instance) {
            this._instance.destroy();
            this._instance = null;
        }

        var colors = this._colors;
        var chartDatasets = datasets.map(function (ds, i) {
            var c = colors[i % colors.length];
            return {
                label: ds.label,
                data: ds.data,
                backgroundColor: c.bg,
                borderColor: c.border,
                borderWidth: 1,
                borderRadius: 4
            };
        });

        var ctx = canvas.getContext('2d');
        this._instance = new Chart(ctx, {
            type: 'bar',
            data: { labels: labels, datasets: chartDatasets },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top',
                        labels: { font: { family: "'Noto Serif KR', serif", size: 12 } }
                    },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                return ctx.dataset.label + ': ' + ctx.parsed.y.toLocaleString('ko-KR') + '원';
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function (value) {
                                if (value >= 10000) return (value / 10000).toFixed(0) + '만';
                                return value.toLocaleString('ko-KR');
                            },
                            font: { family: "'Noto Serif KR', serif", size: 11 }
                        }
                    },
                    x: {
                        ticks: { font: { family: "'Noto Serif KR', serif", size: 11 } }
                    }
                }
            }
        });
    },

    dispose: function () {
        if (this._instance) {
            this._instance.destroy();
            this._instance = null;
        }
    }
};
