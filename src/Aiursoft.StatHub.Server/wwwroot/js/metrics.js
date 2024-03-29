'use strict';

const getWSAddress = function () {
    const isHttps = 'https:' === document.location.protocol;
    const host = window.location.host;
    const head = isHttps ? "wss://" : "ws://";
    return head + host;
};

function humanReadableByteText(bytes) {
    if (bytes > 1024 * 1024 * 1024) {
        return `${(bytes / 1024 / 1024 / 1024).toFixed(2)} GB`
    } else if (bytes > 1024 * 1024) {
        return `${(bytes / 1024 / 1024).toFixed(2)} MB`
    } else if (bytes > 1024) {
        return `${(bytes / 1024).toFixed(2)} KB`
    } else {
        return `${bytes} B`
    }
}

const cpuChartCtx = document.getElementById('cpuChart').getContext('2d');
const cpuChartData = {
    labels: Array(30).fill(''),
    datasets: [{
        label: "CPU",
        borderColor: '#2980b9',
        backgroundColor: '#2980b988',
        fill: true,
        data: Array(30).fill(NaN)
    }]
};

const cpuChart = new Chart(cpuChartCtx, {
    type: 'line',
    data: cpuChartData,
    options: {
        responsive: true,
        plugins: {
            legend: false,
            tooltip: {
                enabled: true,
                callbacks: {
                    label: t => {
                        return `${t.parsed.y} %`;
                    }
                }
            }
        },
        scales: {
            y: {
                title: {
                    display: false,
                },
                suggestedMin: 0,
                suggestedMax: 100
            }
        }
    }
});

const updateCpuChart = function (evt) {
    cpuChartData.labels.shift();
    cpuChartData.labels.push('');
    cpuChartData.datasets[0].data.shift();
    cpuChartData.datasets[0].data.push(evt);
    cpuChart.update();
};

const startCpuClient = function (machineId) {
    const webSocket = new WebSocket(getWSAddress() + "/metrics/" + machineId + "/cpu.ws");
    webSocket.onmessage = function (evt) {
        setTimeout(function () {
            updateCpuChart(evt.data);
        }, 0);
    };
};

const startRamClient = function (machineId, ramInGb) {
    const ramBytes = ramInGb * 1024 * 1024 * 1024;
    const ramChartCtx = document.getElementById('ramChart').getContext('2d');
    const ramChartData = {
        labels: Array(30).fill(''),
        datasets: [{
            label: "RAM",
            borderColor: '#768DF1',
            backgroundColor: '#768DF188',
            fill: true,
            data: Array(30).fill(NaN)
        }]
    };
    const ramChart = new Chart(ramChartCtx, {
        type: 'line',
        data: ramChartData,
        options: {
            responsive: true,
            plugins: {
                legend: false,
                tooltip: {
                    enabled: true,
                    callbacks: {
                        label: t => {
                            return `${humanReadableByteText(t.parsed.y)} (${(t.parsed.y / ramBytes * 100).toFixed(0)}%)`;
                        }
                    }
                }
            },
            scales: {
                y: {
                    title: {
                        display: false,
                    },
                    ticks: {
                        callback: t => humanReadableByteText(t),
                    },
                    min: 0,
                    max: ramBytes
                }
            }
        }
    });

    const webSocket = new WebSocket(getWSAddress() + "/metrics/" + machineId + "/ram.ws");
    webSocket.onmessage = function (evt) {
        setTimeout(function () {
            ramChartData.labels.shift();
            ramChartData.labels.push('');
            ramChartData.datasets[0].data.shift();
            ramChartData.datasets[0].data.push(evt.data);
            ramChart.update();
        }, 0);
    };
};
