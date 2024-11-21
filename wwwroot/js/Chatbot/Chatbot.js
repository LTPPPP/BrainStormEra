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
        ConversationTime: new Date // Optional: server may handle this
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
            console.error('Bot: Sorry, I encountered an error. ' + error.message);
            appendMessage('Your network connection has problem, please check and try again! Thanks 😽')
        });
}

function appendMessage(message, isBot = false) {
    var chatMessages = document.getElementById('chat-messages');
    var messageElement = document.createElement('p');

    if (isBot) {
        messageElement.classList.add('bot-message');
        // Sử dụng `marked` để chuyển đổi markdown sang HTML và sử dụng typewriter effect
        const markdownHTML = marked.parse(message);
        messageElement.innerHTML = ''; // Bắt đầu với nội dung trống để thực hiện hiệu ứng typewriter
        typeWriterEffect(messageElement, markdownHTML, true); // Sử dụng typewriter effect cho markdown
    } else {
        messageElement.classList.add('user-message');
        messageElement.textContent = message; // Hiển thị tin nhắn của người dùng ngay lập tức
    }

    chatMessages.appendChild(messageElement);
    chatMessages.scrollTop = chatMessages.scrollHeight;
}

// Cập nhật hàm typewriter để chèn HTML khi có markdown
function typeWriterEffect(element, message, isHTML = false) {
    let index = 0;
    function type() {
        if (index < message.length) {
            if (isHTML) {
                element.innerHTML = message.substring(0, index + 50); // Chèn từng phần tử HTML
            } else {
                element.innerHTML += message.charAt(index); // Chèn ký tự tiếp theo cho chuỗi văn bản
            }
            index++;
            setTimeout(type, 1); // Điều chỉnh tốc độ hiệu ứng typewriter nếu cần
        }
    }
    type();
}


// Reset chat by clearing chat history and localStorage
function resetChat() {
    localStorage.removeItem('chatHistory');
    document.getElementById('chat-messages').innerHTML = '';
}