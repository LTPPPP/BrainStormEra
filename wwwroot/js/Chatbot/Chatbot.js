document.addEventListener('DOMContentLoaded', function () {
    loadChatHistory(); // Load chat history from localStorage on page load

    document.getElementById('send-button').addEventListener('click', sendMessage);
    document.getElementById('user-input').addEventListener('keypress', function (e) {
        if (e.key === 'Enter') {
            sendMessage();
        }
    });

    // Event listener for the "New Chat" button (you need to add a button in your HTML)
    document.getElementById('new-chat-button').addEventListener('click', resetChat);
});

function toggleChatbot() {
    var chatContainer = document.getElementById('chat-container');
    if (chatContainer.style.display === 'none' || chatContainer.style.display === '') {
        chatContainer.style.display = 'block';
    } else {
        chatContainer.style.display = 'none';
    }
}

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
            return response.json();  // Parse JSON directly
        })
        .then(data => {
            const botReply = data.reply;
            appendMessage('Bot: ' + botReply, true); // Bot response
            logChatHistory('Bot', botReply);
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

    // Add a class based on whether the message is from the bot or the user
    if (isBot) {
        messageElement.classList.add('bot-message');
        messageElement.innerHTML = ''; // Start with an empty message for the typewriter effect
        typeWriterEffect(messageElement, message); // Initiate the typewriter effect for the bot
    } else {
        messageElement.classList.add('user-message');
        messageElement.textContent = message; // Show user message immediately
    }

    chatMessages.appendChild(messageElement);
    chatMessages.scrollTop = chatMessages.scrollHeight; // Scroll to the bottom of the chat
    saveChatHistory(); // Save chat history after each message
}

// Function to implement the typewriter effect for the bot's messages
function typeWriterEffect(element, message) {
    let index = 0;
    function type() {
        if (index < message.length) {
            element.innerHTML += message.charAt(index); // Add the next character
            index++;
            setTimeout(type, 50); // Control the speed of the typing effect (adjust the delay as needed)
        }
    }
    type();
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

// Load chat history from localStorage
function loadChatHistory() {
    const savedHistory = localStorage.getItem('chatHistory');
    if (savedHistory) {
        const chatMessages = document.getElementById('chat-messages');
        chatMessages.innerHTML = savedHistory;
    }
}

// Save chat history to localStorage
function saveChatHistory() {
    const chatMessages = document.getElementById('chat-messages').innerHTML;
    localStorage.setItem('chatHistory', chatMessages);
}

// Reset chat by clearing chat history and localStorage
function resetChat() {
    localStorage.removeItem('chatHistory');
    document.getElementById('chat-messages').innerHTML = ''; 
}

