'use strict';
let webSocket;
const wsChartCtx = document.getElementById('wsChart').getContext('2d');
const wsChartData = {
    labels: [],
    datasets: [{
        label: "WebSocket Connection",
        backgroundColor: 'rgb(2, 232, 99)',
        borderColor: 'rgb(2, 232, 99)',
        fill: false,
        data: []
    }]
};

const chartOption = {
    responsive: true,
    tooltips: {
        mode: 'index',
        intersect: false
    },
    hover: {
        mode: 'nearest',
        intersect: true
    },
    scales: {
        xAxes: [{
            display: true,
            scaleLabel: {
                display: true,
                labelString: 'Time'
            }
        }],
        yAxes: [{
            display: true,
            scaleLabel: {
                display: true,
                labelString: 'Value'
            }
        }]
    }
};

window.myWsLine = new Chart(wsChartCtx, {
    type: 'line',
    data: wsChartData,
    options: chartOption
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
