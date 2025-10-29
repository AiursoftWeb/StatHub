'use strict';

/**
 * 获取当前页面的 WebSocket (wss/ws) 地址头。
 */
const getWSAddress = function () {
    const isHttps = 'https:' === document.location.protocol;
    const host = window.location.host;
    const head = isHttps ? "wss://" : "ws://";
    return head + host;
};

/**
 * 将字节转换为易读的字符串 (B, KB, MB, GB)。
 * @param {number} bytes - 字节数。
 */
function humanReadableByteText(bytes) {
    if (bytes === 0) {
        return '0 B';
    }
    if (!bytes || bytes < 0) {
        return 'N/A';
    }

    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    return `${(bytes / Math.pow(1024, i)).toFixed(2)} ${sizes[i]}`;
}

/**
 * [新] 创建一个健壮的 WebSocket 连接，支持自动重连。
 * @param {string} url - WebSocket URL。
 * @param {function} onMessageCallback - 处理 'onmessage' 事件的回调。
 */
function createRobustWebSocket(url, onMessageCallback) {
    let ws;

    function connect() {
        ws = new WebSocket(url);
        ws.onmessage = onMessageCallback;

        ws.onclose = function () {
            console.warn(`WebSocket at ${url} closed. Retrying in 3 seconds...`);
            // 发生 close 时，3秒后自动重连
            setTimeout(connect, 3000);
        };

        ws.onerror = function (err) {
            console.error(`WebSocket error at ${url}:`, err);
            // 发生 error 时，关闭连接，这将触发 onclose 逻辑
            ws.close();
        };
    }

    connect();
}

// --- 1. CPU Chart (已重构为使用 new helper) ---

const cpuChartCtx = document.getElementById('cpuChart').getContext('2d');
const cpuChartData = {
    labels: Array(30).fill(''),
    datasets: [{
        label: "CPU",
        borderColor: '#2980b9',
        backgroundColor: '#2980b988',
        fill: true,
        data: Array(30).fill(NaN),
        tension: 0.1 // 使线条平滑
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
                    label: t => `${t.parsed.y} %`
                }
            }
        },
        scales: {
            y: {
                title: { display: false },
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
    cpuChart.update('none'); // [优化] 禁用动画
};

const startCpuClient = function (machineId) {
    const socketUrl = getWSAddress() + "/metrics/" + machineId + "/cpu.ws";
    // [重构] 使用健壮的 WebSocket helper
    createRobustWebSocket(socketUrl, function (evt) {
        setTimeout(function () {
            updateCpuChart(evt.data);
        }, 0);
    });
};

// --- 2. RAM Chart (已重构为使用 new helper) ---

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
            data: Array(30).fill(NaN),
            tension: 0.1
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
                        label: t => `${humanReadableByteText(t.parsed.y)} (${(t.parsed.y / ramBytes * 100).toFixed(0)}%)`
                    }
                }
            },
            scales: {
                y: {
                    title: { display: false },
                    ticks: {
                        callback: t => humanReadableByteText(t),
                    },
                    min: 0,
                    max: ramBytes
                }
            }
        }
    });

    const socketUrl = getWSAddress() + "/metrics/" + machineId + "/ram.ws";
    // [重构] 使用健壮的 WebSocket helper
    createRobustWebSocket(socketUrl, function (evt) {
        setTimeout(function () {
            ramChartData.labels.shift();
            ramChartData.labels.push('');
            ramChartData.datasets[0].data.shift();
            ramChartData.datasets[0].data.push(evt.data);
            ramChart.update('none'); // [优化] 禁用动画
        }, 0);
    });
};

// --- 3. [新] Load Chart (三合一) ---

const startLoadChart = function (machineId) {
    const ctx = document.getElementById('loadChart').getContext('2d');
    const chartData = {
        labels: Array(30).fill(''),
        datasets: [
            {
                label: "Load 1m",
                borderColor: '#3498db', // Blue
                backgroundColor: '#3498db88',
                fill: false,
                data: Array(30).fill(NaN),
                tension: 0.1
            },
            {
                label: "Load 5m",
                borderColor: '#2ecc71', // Green
                backgroundColor: '#2ecc7188',
                fill: false,
                data: Array(30).fill(NaN),
                tension: 0.1
            },
            {
                label: "Load 15m",
                borderColor: '#f1c40f', // Yellow
                backgroundColor: '#f1c40f88',
                fill: false,
                data: Array(30).fill(NaN),
                tension: 0.1
            }]
    };

    const chart = new Chart(ctx, {
        type: 'line',
        data: chartData,
        options: {
            responsive: true,
            plugins: {
                legend: { display: true }, // 显示图例
                tooltip: {
                    enabled: true,
                    callbacks: {
                        label: t => `${t.dataset.label}: ${t.parsed.y.toFixed(2)}`
                    }
                }
            },
            scales: {
                y: {
                    title: { display: false },
                    suggestedMin: 0
                }
            }
        }
    });

    // "主时钟" 逻辑: load1m 驱动 X 轴
    createRobustWebSocket(getWSAddress() + "/metrics/" + machineId + "/load1m.ws", evt => {
        setTimeout(() => {
            chartData.labels.shift();
            chartData.labels.push('');

            // 更新 line 0 (1m)
            chartData.datasets[0].data.shift();
            chartData.datasets[0].data.push(evt.data);

            // 移动 line 1 (5m), 重复最后一个值
            chartData.datasets[1].data.shift();
            chartData.datasets[1].data.push(chartData.datasets[1].data[chartData.datasets[1].data.length - 1]);

            // 移动 line 2 (15m), 重复最后一个值
            chartData.datasets[2].data.shift();
            chartData.datasets[2].data.push(chartData.datasets[2].data[chartData.datasets[2].data.length - 1]);

            chart.update('none');
        }, 0);
    });

    // load5m 只更新它自己的数据点
    createRobustWebSocket(getWSAddress() + "/metrics/" + machineId + "/load5m.ws", evt => {
        chartData.datasets[1].data[chartData.datasets[1].data.length - 1] = evt.data;
    });

    // load15m 只更新它自己的数据点
    createRobustWebSocket(getWSAddress() + "/metrics/" + machineId + "/load15m.ws", evt => {
        chartData.datasets[2].data[chartData.datasets[2].data.length - 1] = evt.data;
    });
};

// --- 4. [新] Network I/O Chart (二合一) ---

const startNetworkChart = function (machineId) {
    const ctx = document.getElementById('networkChart').getContext('2d');
    const chartData = {
        labels: Array(30).fill(''),
        datasets: [
            {
                label: "Receiving",
                borderColor: '#1abc9c', // Turquoise
                backgroundColor: '#1abc9c88',
                fill: true,
                data: Array(30).fill(NaN),
                tension: 0.1
            },
            {
                label: "Sending",
                borderColor: '#e67e22', // Orange
                backgroundColor: '#e67e2288',
                fill: true,
                data: Array(30).fill(NaN),
                tension: 0.1
            }]
    };

    const chart = new Chart(ctx, {
        type: 'line',
        data: chartData,
        options: {
            responsive: true,
            plugins: {
                legend: { display: true },
                tooltip: {
                    enabled: true,
                    callbacks: {
                        label: t => `${t.dataset.label}: ${humanReadableByteText(t.parsed.y)}/s`
                    }
                }
            },
            scales: {
                y: {
                    title: { display: false },
                    ticks: {
                        callback: t => humanReadableByteText(t) + '/s'
                    },
                    min: 0
                }
            }
        }
    });

    // "主时钟" 逻辑: net-recv 驱动 X 轴
    createRobustWebSocket(getWSAddress() + "/metrics/" + machineId + "/net-recv.ws", evt => {
        setTimeout(() => {
            chartData.labels.shift();
            chartData.labels.push('');

            // 更新 line 0 (Recv)
            chartData.datasets[0].data.shift();
            chartData.datasets[0].data.push(evt.data);

            // 移动 line 1 (Send), 重复最后一个值
            chartData.datasets[1].data.shift();
            chartData.datasets[1].data.push(chartData.datasets[1].data[chartData.datasets[1].data.length - 1]);

            chart.update('none');
        }, 0);
    });

    // net-send 只更新它自己的数据点
    createRobustWebSocket(getWSAddress() + "/metrics/" + machineId + "/net-send.ws", evt => {
        chartData.datasets[1].data[chartData.datasets[1].data.length - 1] = evt.data;
    });
};

// --- 5. [新] Disk I/O Chart (二合一) ---

const startDiskChart = function (machineId) {
    const ctx = document.getElementById('diskChart').getContext('2d');
    const chartData = {
        labels: Array(30).fill(''),
        datasets: [
            {
                label: "Reading",
                borderColor: '#9b59b6', // Purple
                backgroundColor: '#9b59b688',
                fill: true,
                data: Array(30).fill(NaN),
                tension: 0.1
            },
            {
                label: "Writing",
                borderColor: '#e74c3c', // Red
                backgroundColor: '#e74c3c88',
                fill: true,
                data: Array(30).fill(NaN),
                tension: 0.1
            }]
    };

    const chart = new Chart(ctx, {
        type: 'line',
        data: chartData,
        options: {
            responsive: true,
            plugins: {
                legend: { display: true },
                tooltip: {
                    enabled: true,
                    callbacks: {
                        label: t => `${t.dataset.label}: ${humanReadableByteText(t.parsed.y)}/s`
                    }
                }
            },
            scales: {
                y: {
                    title: { display: false },
                    ticks: {
                        callback: t => humanReadableByteText(t) + '/s'
                    },
                    min: 0
                }
            }
        }
    });

    // "主时钟" 逻辑: disk-read 驱动 X 轴
    createRobustWebSocket(getWSAddress() + "/metrics/" + machineId + "/disk-read.ws", evt => {
        setTimeout(() => {
            chartData.labels.shift();
            chartData.labels.push('');

            // 更新 line 0 (Read)
            chartData.datasets[0].data.shift();
            chartData.datasets[0].data.push(evt.data);

            // 移动 line 1 (Writ), 重复最后一个值
            chartData.datasets[1].data.shift();
            chartData.datasets[1].data.push(chartData.datasets[1].data[chartData.datasets[1].data.length - 1]);

            chart.update('none');
        }, 0);
    });

    // disk-writ 只更新它自己的数据点
    createRobustWebSocket(getWSAddress() + "/metrics/" + machineId + "/disk-writ.ws", evt => {
        chartData.datasets[1].data[chartData.datasets[1].data.length - 1] = evt.data;
    });
};
