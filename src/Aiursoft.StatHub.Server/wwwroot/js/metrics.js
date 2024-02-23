'use strict';
const wsChartCtx = document.getElementById('wsChart').getContext('2d');
const wsChartData = {
    labels: Array(30).fill(''),
    datasets: [{
        label: "CPU",
        borderColor: '#2980b9',
        backgroundColor: '#2980b988',
        fill: true,
        data: Array(30).fill(''),
    }]
};

window.myWsLine = new Chart(wsChartCtx, {
    type: 'line',
    data: wsChartData,
    options: {
        responsive: true,
        plugins: {
            legend: false,
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

const getWSAddress = function () {
    const isHttps = 'https:' === document.location.protocol;
    const host = window.location.host;
    const head = isHttps ? "wss://" : "ws://";
    return head + host;
};

const updateWebSocketChart = function (evt) {
    wsChartData.labels.shift();
    wsChartData.labels.push('');
    wsChartData.datasets[0].data.shift();
    wsChartData.datasets[0].data.push(evt);
    window.myWsLine.update();
};

const startWebSocketClient = function (machineId) {
    const webSocket = new WebSocket(getWSAddress() + "/metrics/" + machineId + "/cpu.ws");
    webSocket.onmessage = function (evt) {
        setTimeout(function () {
            updateWebSocketChart(evt.data);
        }, 0);
    };
};
