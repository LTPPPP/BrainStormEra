// Lấy các phần tử
const form = document.getElementById('notificationForm');
const dialogOverlay = document.getElementById('dialogOverlay');
const sendToUsers = document.getElementById('sendToUsers');

// Khi nhấn nút "Send" trong form, hiển thị dialog
form.addEventListener('submit', function(event) {
    event.preventDefault(); // Ngăn không cho form submit thật sự
    dialogOverlay.style.display = 'flex'; // Hiển thị dialog
});

// Khi nhấn nút "Send" trong dialog, lấy danh sách user được chọn
sendToUsers.addEventListener('click', function() {
    const selectedUsers = [];
    const checkboxes = dialogOverlay.querySelectorAll('input[type="checkbox"]:checked');

    // Lặp qua các checkbox đã chọn và lấy giá trị
    checkboxes.forEach(function(checkbox) {
        selectedUsers.push(checkbox.value);
    });

    if (selectedUsers.length > 0) {
        alert("Notification sent to: " + selectedUsers.join(", "));
    } else {
        alert("No users selected");
    }

    // Ẩn dialog sau khi gửi
    dialogOverlay.style.display = 'none';
});
