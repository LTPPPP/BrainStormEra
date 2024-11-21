$(document).ready(function () {
    // Đảm bảo dialog overlay được ẩn ngay khi trang tải
    $('#dialogOverlay').hide();

    // Khi nhấn nút "Add Notification", mở modal thêm thông báo
    $('.add-notification').click(function () {
        $('#addNotificationModal').modal('show');
    });

    // Khi nhấn nút "Send" trong form Add Notification, kiểm tra dữ liệu trước khi hiển thị dialog chọn user
    $('#notificationForm').on('submit', function (event) {
        event.preventDefault(); // Ngăn form submit thật sự

        // Lấy các giá trị từ form Add Notification
        var notificationTitle = $('#subject').val();
        var notificationContent = $('#content').val();
        var notificationType = $('#notificationType').val(); // Lấy giá trị của thẻ select loại thông báo

        // Kiểm tra xem các trường cần thiết đã được điền chưa
        if (!notificationTitle || !notificationContent || !notificationType) {
            alert("Please fill in all required fields before selecting users.");
            return;
        }

        $('#dialogOverlay').fadeIn();

        $.get('/Notification/GetUsers', function (data) {
            var userList = '';
            $.each(data, function (index, user) {
                userList += '<li><input type="checkbox" class="user-checkbox" value="' + user.user_id + '">' + user.full_name + '</li>';
            });
            $('#userList').html(userList); // Thêm danh sách user vào dialog
        }).fail(function () {
            alert('Failed to load users'); // Thông báo nếu việc tải dữ liệu thất bại
        });
    });

    // Thêm sự kiện cho checkbox "Select All" để chọn hoặc bỏ chọn tất cả người dùng
    $('#selectAllUsers').on('change', function () {
        var isChecked = $(this).is(':checked');
        $('#userList input[type="checkbox"]').prop('checked', isChecked);

        var selectedUsers = [];
        if (isChecked) {
            $('#userList input[type="checkbox"]').each(function () {
                selectedUsers.push($(this).val());
            });
        } else {
            selectedUsers = [];
        }
    });

    $('#sendToUsers').click(function () {
        var selectedUsers = [];
        $('#userList input[type="checkbox"]:checked').each(function () {
            selectedUsers.push($(this).val());
        });

        if (selectedUsers.length === 0) {
            alert("No users selected");
            return;
        }

        var notificationTitle = $('#subject').val();
        var notificationContent = $('#content').val();
        var notificationType = $('#notificationType').val();

        if (!notificationTitle || !notificationContent || !notificationType) {
            alert("Please fill in all required fields.");
            return;
        }

        var notificationData = {
            UserIds: selectedUsers,
            NotificationTitle: notificationTitle,
            NotificationContent: notificationContent,
            NotificationType: notificationType
        };

        console.log("Sending notification data: ", notificationData); // In ra console để kiểm tra dữ liệu

        // Gửi dữ liệu lên server qua AJAX để lưu thông báo
        $.ajax({
            url: '/Notification/CreateNotification', // Đường dẫn đến action CreateNotification
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(notificationData),
            success: function (response) {
                if (response.success) {
                    alert("Notification created successfully.");
                    $('#dialogOverlay').fadeOut(); // Ẩn overlay sau khi gửi
                    $('#addNotificationModal').modal('hide'); // Đóng modal sau khi tạo thông báo
                } else {
                    alert("Failed to create notification.");
                }
            },
            error: function (error) {
                alert("Error creating notification.");
                console.error("Error:", error.responseText); // In ra lỗi để debug
            }
        });
    });

    // Đảm bảo khi nhấn vào bất kỳ vùng nào ngoài dialog, dialog sẽ bị đóng
    $('#dialogOverlay').click(function (e) {
        if (e.target === this) {
            $(this).fadeOut(); // Ẩn overlay khi nhấn ra ngoài
        }
    });
});

// Edit
$(document).on('click', '.edit', function () {
    $('#notificationsModal').modal('hide');
    var notificationId = $(this).closest('.notification-item').data('notification-id');

    // Gọi AJAX để lấy dữ liệu của Notification
    $.get('/Notification/GetNotificationById', { id: notificationId }, function (response) {
        if (response.success) {
            // Đổ dữ liệu vào form chỉnh sửa
            $('#editNotificationId').val(response.data.notificationId);
            $('#editSubject').val(response.data.notificationTitle);
            $('#editContent').val(response.data.notificationContent);
            console.log(response.data.notificationType);

            // Gán giá trị của notification type vào select và đảm bảo nó hiển thị đúng
            $('#editType').val(response.data.notificationType).trigger('change');

            // Hiển thị Modal Edit
            $('#editNotificationModal').modal('show');
        } else {
            alert('Failed to load notification data.');
        }
    }).fail(function () {
        alert('Error fetching notification data.');
    });
});

$('#editNotificationForm').on('submit', function (event) {
    event.preventDefault();

    // Lấy các giá trị từ form
    var notificationId = $('#editNotificationId').val();
    var notificationTitle = $('#editSubject').val();
    var notificationContent = $('#editContent').val();
    var notificationType = $('#editType').val();

    // Kiểm tra xem các trường cần thiết đã được điền chưa
    if (!notificationTitle || !notificationContent || !notificationType) {
        alert("Please fill in all required fields.");
        return;
    }

    // Tạo object chứa dữ liệu để gửi lên server
    var updatedNotificationData = {
        NotificationId: notificationId,
        NotificationTitle: notificationTitle,
        NotificationContent: notificationContent,
        NotificationType: notificationType
    };

    // Gửi dữ liệu lên server qua AJAX để cập nhật thông báo
    $.ajax({
        url: '/Notification/EditNotification',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(updatedNotificationData),
        success: function (response) {
            if (response.success) {
                alert("Notification updated successfully.");
                $('#editNotificationModal').modal('hide'); // Đóng modal sau khi cập nhật
                location.reload(); // Tải lại trang để hiển thị thông tin cập nhật
            } else {
                alert("Failed to update notification.");
            }
        },
        error: function (error) {
            alert("Error updating notification.");
            console.error("Error:", error.responseText);
        }
    });
});

//Delete
$(document).on('click', '.delete', function () {
    var notificationId = $(this).closest('.notification-item').data('notification-id');

    if (confirm("Are you sure you want to delete this notification?")) {
        // Gửi yêu cầu AJAX để xóa notification
        $.ajax({
            url: '/Notification/DeleteNotification', // Đường dẫn đến action DeleteNotification
            method: 'POST',
            data: { id: notificationId },
            success: function (response) {
                if (response.success) {
                    alert("Notification deleted successfully.");
                    location.reload(); // Tải lại trang sau khi xóa thành công
                } else {
                    alert("Failed to delete notification: " + response.message);
                }
            },
            error: function (error) {
                alert("Error deleting notification.");
                console.error("Error:", error.responseText);
            }
        });
    }
});

$(document).on('click', '#deleteSelected', function () {
    var selectedIds = [];

    // Lấy tất cả các checkbox đã được chọn
    $('.select-notification:checked').each(function () {
        selectedIds.push($(this).data('notification-id'));
    });

    if (selectedIds.length > 0) {
        // Gửi yêu cầu xóa các notification đã chọn
        $.ajax({
            url: '/Notification/DeleteSelectedNotifications',
            method: 'POST',
            data: { ids: selectedIds },
            traditional: true, // Sử dụng để truyền mảng đơn giản
            success: function (response) {
                if (response.success) {
                    alert("Notifications deleted successfully.");
                    location.reload(); // Tải lại trang sau khi xóa thành công
                } else {
                    alert("Failed to delete selected notifications: " + response.message);
                }
            },
            error: function (error) {
                alert("Error deleting selected notifications.");
                console.error("Error:", error.responseText);
            }
        });
    } else {
        alert("No notifications selected for deletion.");
    }
});
