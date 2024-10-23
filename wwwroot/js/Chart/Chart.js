
$(document).ready(function () {
    $.get('@Url.Action("GetConversationStatistics", "Chatbot")', function (data) {
        var chartElement = document.getElementById('conversationChart');
        if (chartElement) {
            var ctx = chartElement.getContext('2d');
            var chartData = {
                labels: data.map(item => new Date(item.date).toLocaleDateString()), // Date labels
                datasets: [{
                    label: 'Chatbot Requests',
                    data: data.map(item => item.count), // Number of conversations per day
                    fill: false,
                    borderColor: 'rgba(75, 192, 192, 1)',
                    tension: 0.1
                }]
            };

            new Chart(ctx, {
                type: 'line',
                data: chartData,
                options: {
                    scales: {
                        y: {
                            beginAtZero: true
                        }
                    }
                }
            });
        }
    });

    // Fetching and rendering the user statistics
    $.get('@Url.Action("GetUserStatistics", "HomePageAdmin")', function (data) {
        var chartElement = document.getElementById('userChart');
        if (chartElement) {
            var ctx = chartElement.getContext('2d');
            var chartData = {
                labels: data.map(item => new Date(item.date).toLocaleDateString()), // Date labels
                datasets: [{
                    label: 'New Users',
                    data: data.map(item => item.count), // Number of new users per day
                    fill: false,
                    borderColor: 'rgba(54, 162, 235, 1)',
                    tension: 0.1
                }]
            };

            new Chart(ctx, {
                type: 'line',
                data: chartData,
                options: {
                    scales: {
                        y: {
                            beginAtZero: true
                        }
                    }
                }
            });
        }
    });
});