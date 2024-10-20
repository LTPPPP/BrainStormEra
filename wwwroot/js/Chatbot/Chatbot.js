// chatbot.js
document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('send-button').addEventListener('click', sendMessage);
    document.getElementById('user-input').addEventListener('keypress', function (e) {
        if (e.key === 'Enter') {
            sendMessage();
        }
    });
});

function sendMessage() {
    var message = document.getElementById('user-input').value;
    if (message.trim() === '') return;

    appendMessage('You: ' + message);
    logChatHistory('User', message);
    document.getElementById('user-input').value = '';

    fetch('/Chatbot/SendMessage', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ message: message })
    })
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.text();
        })
        .then(data => {
            try {
                const jsonData = JSON.parse(data);
                appendMessage('Bot: ' + jsonData.reply, true);  // Pass 'true' for bot response
                logChatHistory('Bot', jsonData.reply);
            } catch (e) {
                appendMessage('Bot: ' + data, true);  // Pass 'true' for bot response
                logChatHistory('Bot', data);
            }
        })
        .catch(error => {
            console.error('Error:', error);
            appendMessage('Bot: Sorry, I encountered an error. ' + error.message);
            logChatHistory('Error', error.message);
        });
}


function appendMessage(message, isBot = false) {
    var chatMessages = document.getElementById('chat-messages');
    var messageElement = document.createElement('p');

    // If the message is from the bot, convert markdown to HTML
    if (isBot) {
        messageElement.innerHTML = marked.parse(message);
    } else {
        messageElement.textContent = message;
    }

    chatMessages.appendChild(messageElement);
    chatMessages.scrollTop = chatMessages.scrollHeight;
}


function logChatHistory(sender, message) {
    const timestamp = new Date().toISOString();
    const logEntry = { timestamp, sender, message };
    fetch('/Chatbot/LogChat', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(logEntry)
    }).catch(error => console.error('Error logging chat:', error));
}