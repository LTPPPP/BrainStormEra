document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('send-button').addEventListener('click', sendMessage);
    document.getElementById('user-input').addEventListener('keypress', function (e) {
        if (e.key === 'Enter') {
            sendMessage();
        }
    });

    document.getElementById('new-chat-button').addEventListener('click', resetChat);
});

// Function to get cookie value by name
function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
}

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
    document.getElementById('user-input').value = '';

    // Get the userId from the cookies
    var userId = getCookie('user_id');  // Ensure 'user_id' matches your cookie name

    // Construct the correct request body as expected by the server
    var conversationData = {
        ConversationContent: message,
        UserId: userId,  // UserId retrieved from cookies
        ConversationTime: new Date().toISOString()  // Optional: server may handle this
    };

    fetch('/Chatbot/SendMessage', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(conversationData) // Send the correct object structure
    })
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();  // Parse JSON directly
        })
        .then(data => {
            const botReply = data.reply;
            appendMessage('Bot: ' + botReply, true);
        })
        .catch(error => {
            console.error('Error:', error);
            appendMessage('Bot: Sorry, I encountered an error. ' + error.message);
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
    chatMessages.scrollTop = chatMessages.scrollHeight;
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

// Reset chat by clearing chat history and localStorage
function resetChat() {
    localStorage.removeItem('chatHistory');
    document.getElementById('chat-messages').innerHTML = '';
}
