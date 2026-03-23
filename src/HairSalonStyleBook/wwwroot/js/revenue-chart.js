// 매출 차트 (Chart.js) JS 인터롭
window.revenueChart = {
    _instance: null,

    // 차트 생성/업데이트
    render: function (canvasId, labels, currentYearData, prevYearData, currentYearLabel, prevYearLabel) {
        var canvas = document.getElementById(canvasId);
        if (!canvas) return;

        // 기존 인스턴스 파괴
        if (this._instance) {
            this._instance.destroy();
            this._instance = null;
        }

        var ctx = canvas.getContext('2d');
        this._instance = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: currentYearLabel,
                        data: currentYearData,
                        backgroundColor: 'rgba(123, 163, 131, 0.8)',
                        borderColor: 'rgba(123, 163, 131, 1)',
                        borderWidth: 1,
                        borderRadius: 4
                    },
                    {
                        label: prevYearLabel,
                        data: prevYearData,
                        backgroundColor: 'rgba(44, 44, 44, 0.25)',
                        borderColor: 'rgba(44, 44, 44, 0.5)',
                        borderWidth: 1,
                        borderRadius: 4
                    }
                ]
            },
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

    // 차트 파괴
    dispose: function () {
        if (this._instance) {
            this._instance.destroy();
            this._instance = null;
        }
    }
};
