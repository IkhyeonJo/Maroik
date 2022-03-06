/* global Chart:false */

$(function () {
  'use strict'
  var intForecastResult0 = $("#intForecastResult0").val(); // load data from body
  var intForecastResult1 = $("#intForecastResult1").val(); // load data from body
  var intForecastResult2 = $("#intForecastResult2").val(); // load data from body
  var intForecastResult3 = $("#intForecastResult3").val(); // load data from body
  var intForecastResult4 = $("#intForecastResult4").val(); // load data from body
  var intForecastResult5 = $("#intForecastResult5").val(); // load data from body
  var intForecastResult6 = $("#intForecastResult6").val(); // load data from body

  var intForecastStockResult0 = $("#intForecastStockResult0").val(); // load data from body
  var intForecastStockResult1 = $("#intForecastStockResult1").val(); // load data from body
  var intForecastStockResult2 = $("#intForecastStockResult2").val(); // load data from body
  var intForecastStockResult3 = $("#intForecastStockResult3").val(); // load data from body
  var intForecastStockResult4 = $("#intForecastStockResult4").val(); // load data from body
  var intForecastStockResult5 = $("#intForecastStockResult5").val(); // load data from body
  var intForecastStockResult6 = $("#intForecastStockResult6").val(); // load data from body

  var ticksStyle = {
    fontColor: '#495057',
    fontStyle: 'bold'
  }

  var mode = 'index'
  var intersect = true

  var $salesChart = $('#sales-chart')
  // eslint-disable-next-line no-unused-vars
  var salesChart = new Chart($salesChart, {
    type: 'bar',
    data: {
      labels: ['Jan.', 'Feb.', 'Mar.', 'Apr.', 'May.', 'Jun.', 'Jul.'],
      datasets: [
        {
          backgroundColor: '#007bff',
          borderColor: '#007bff',
          data: [310930300, 294236640, 216259940, 245451930, 161232320, 111017310, 170579770]
        },
        {
          backgroundColor: '#ced4da',
          borderColor: '#ced4da',
          data: [57187310, 85108680, 103978270, 137700340, 181876020, 195331100, 244635200]
        }
      ]
    },
    options: {
      maintainAspectRatio: false,
      tooltips: {
        mode: mode,
        intersect: intersect
      },
      hover: {
        mode: mode,
        intersect: intersect
      },
      legend: {
        display: false
      },
      scales: {
        yAxes: [{
          // display: false,
          gridLines: {
            display: true,
            lineWidth: '4px',
            color: 'rgba(0, 0, 0, .2)',
            zeroLineColor: 'transparent'
          },
          ticks: $.extend({
            beginAtZero: true,

            // Include a dollar sign in the ticks
            callback: function (value) {
              if (value >= 1000) {
                value /= 1000
                value += 'k'
              }

              return value + ' Won'
            }
          }, ticksStyle)
        }],
        xAxes: [{
          display: true,
          gridLines: {
            display: false
          },
          ticks: ticksStyle
        }]
      }
    }
  })

  var $salesStockChart = $('#sales-stock-chart')
  // eslint-disable-next-line no-unused-vars
  var salesStockChart = new Chart($salesStockChart, {
    type: 'bar',
    data: {
      labels: ['Jan.', 'Feb.', 'Mar.', 'Apr.', 'May.', 'Jun.', 'Jul.'],
      datasets: [
        {
          backgroundColor: '#007bff',
          borderColor: '#007bff',
          data: [4560, 4110, 3105, 3442, 3128, 2177, 3185]
        },
        {
          backgroundColor: '#ced4da',
          borderColor: '#ced4da',
          data: [912, 1271, 1585, 2082, 2890, 3191, 4114]
        }
      ]
    },
    options: {
      maintainAspectRatio: false,
      tooltips: {
        mode: mode,
        intersect: intersect
      },
      hover: {
        mode: mode,
        intersect: intersect
      },
      legend: {
        display: false
      },
      scales: {
        yAxes: [{
          // display: false,
          gridLines: {
            display: true,
            lineWidth: '4px',
            color: 'rgba(0, 0, 0, .2)',
            zeroLineColor: 'transparent'
          },
          ticks: $.extend({
            beginAtZero: true,

            // Include a dollar sign in the ticks
            callback: function (value) {
              if (value >= 1000) {
                value /= 1000
                value += 'k'
              }

              return value + ' Won'
            }
          }, ticksStyle)
        }],
        xAxes: [{
          display: true,
          gridLines: {
            display: false
          },
          ticks: ticksStyle
        }]
      }
    }
  })

  var $visitorsChart = $('#visitors-chart')
  // eslint-disable-next-line no-unused-vars
  var visitorsChart = new Chart($visitorsChart, {
    data: {
      labels: ['8th', '9th', '10nd', '11th', '12th', '13th', '14th'],
      datasets: [{
        type: 'line',
        data: [intForecastResult0, intForecastResult1, intForecastResult2, intForecastResult3, intForecastResult4, intForecastResult5, intForecastResult6],
        backgroundColor: 'transparent',
        borderColor: '#007bff',
        pointBorderColor: '#007bff',
        pointBackgroundColor: '#007bff',
        fill: false
        // pointHoverBackgroundColor: '#007bff',
        // pointHoverBorderColor    : '#007bff'
      },
      {
        type: 'line',
        data: [4924500, 4158400, 5254290, 6519700, 5140370, 4109500, 2471600],
        backgroundColor: 'tansparent',
        borderColor: '#ced4da',
        pointBorderColor: '#ced4da',
        pointBackgroundColor: '#ced4da',
        fill: false
        // pointHoverBackgroundColor: '#ced4da',
        // pointHoverBorderColor    : '#ced4da'
       }
      ]
    },
    options: {
      maintainAspectRatio: false,
      tooltips: {
        mode: mode,
        intersect: intersect
      },
      hover: {
        mode: mode,
        intersect: intersect
      },
      legend: {
        display: false
      },
      scales: {
        yAxes: [{
          // display: false,
          gridLines: {
            display: true,
            lineWidth: '4px',
            color: 'rgba(0, 0, 0, .2)',
            zeroLineColor: 'transparent'
          },
          ticks: $.extend({
            beginAtZero: true,
            suggestedMax: 200
          }, ticksStyle)
        }],
        xAxes: [{
          display: true,
          gridLines: {
            display: false
          },
          ticks: ticksStyle
        }]
      }
    }
  })

  var $visitorsStockChart = $('#visitors-stock-chart')
  // eslint-disable-next-line no-unused-vars
  var visitorsStockChart = new Chart($visitorsStockChart, {
    data: {
      labels: ['8th', '9th', '10nd', '11th', '12th', '13th', '14th'],
      datasets: [{
        type: 'line',
        data: [intForecastStockResult0, intForecastStockResult1, intForecastStockResult2, intForecastStockResult3, intForecastStockResult4, intForecastStockResult5, intForecastStockResult6],
        backgroundColor: 'transparent',
        borderColor: '#007bff',
        pointBorderColor: '#007bff',
        pointBackgroundColor: '#007bff',
        fill: false
        // pointHoverBackgroundColor: '#007bff',
        // pointHoverBorderColor    : '#007bff'
      },
      {
        type: 'line',
        data: [80, 75, 95, 123, 93, 76, 51],
        backgroundColor: 'tansparent',
        borderColor: '#ced4da',
        pointBorderColor: '#ced4da',
        pointBackgroundColor: '#ced4da',
        fill: false
        // pointHoverBackgroundColor: '#ced4da',
        // pointHoverBorderColor    : '#ced4da'
       }
      ]
    },
    options: {
      maintainAspectRatio: false,
      tooltips: {
        mode: mode,
        intersect: intersect
      },
      hover: {
        mode: mode,
        intersect: intersect
      },
      legend: {
        display: false
      },
      scales: {
        yAxes: [{
          // display: false,
          gridLines: {
            display: true,
            lineWidth: '4px',
            color: 'rgba(0, 0, 0, .2)',
            zeroLineColor: 'transparent'
          },
          ticks: $.extend({
            beginAtZero: true,
            suggestedMax: 200
          }, ticksStyle)
        }],
        xAxes: [{
          display: true,
          gridLines: {
            display: false
          },
          ticks: ticksStyle
        }]
      }
    }
  })
})

// lgtm [js/unused-local-variable]
