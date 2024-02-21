'use strict';
let webSocket;
const wsChartCtx = document.getElementById('wsChart').getContext('2d');
const wsChartData = {
    labels: [],
    datasets: [{
        label: "CPU",
        borderColor: Utils.CHART_COLORS.blue,
        backgroundColor: Utils.transparentize(Utils.CHART_COLORS.blue, 0.5),
        fill: true,
        data: []
    }]
};

window.myWsLine = new Chart(wsChartCtx, {
    type: 'line',
    data: wsChartData,
    options: {
        responsive: true,
        scales: {
            y: {
                title: {
                    display: false,
                },
                min: 0,
                max: 100,
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
    if (wsChartData.labels.length > 30) {
        wsChartData.labels.shift();
        wsChartData.datasets[0].data.shift();
    }
    wsChartData.labels.push('');
    wsChartData.datasets[0].data.push(evt);
    window.myWsLine.update();
};

const startWebSocketClient = function (machineId) {
    webSocket = new WebSocket(getWSAddress() + "/metrics/" + machineId + "/cpu.ws");
    webSocket.onmessage = function (evt) {
        setTimeout(function () {
            updateWebSocketChart(evt.data);
        }, 0);
    };
};
