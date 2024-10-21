// Handle Edit
document.querySelectorAll('.btn-edit').forEach(button => {
    button.addEventListener('click', function () {
        const achievementId = this.getAttribute('data-id');

        fetch(`/Achievement/GetAchievement?achievementId=${achievementId}`)
            .then(response => response.json())
            .then(result => {
                if (result.success) {
                    const data = result.data;
                    // Fill the form with data for editing
                    document.getElementById('achievementId').value = data.achievementId;
                    document.getElementById('achievementName').value = data.achievementName;
                    document.getElementById('achievementDescription').value = data.achievementDescription;
                    document.getElementById('achievementIcon').value = data.achievementIcon;
                    document.getElementById('achievementCreatedAt').value = data.achievementCreatedAt.split("T")[0];

                    // Disable the ID field when editing
                    document.getElementById('achievementId').setAttribute('readonly', 'readonly');
                    document.getElementById('achievementForm').action = '/Achievement/EditAchievement';
                } else {
                    alert(result.message);
                }
            });
    });
});

// Handle Add (Auto-generate ID and clear form before showing it)
document.querySelector('.btn-add').addEventListener('click', function () {
    document.getElementById('achievementId').value = '';
    document.getElementById('achievementName').value = '';
    document.getElementById('achievementDescription').value = '';
    document.getElementById('achievementIcon').value = '';
    document.getElementById('achievementCreatedAt').value = '';

    // Enable the ID field when adding
    document.getElementById('achievementId').removeAttribute('readonly');

    // Fetch and display next sequential Achievement ID
    fetch('/Achievement/GetNextAchievementId')
        .then(response => response.json())
        .then(result => {
            if (result.success) {
                document.getElementById('achievementId').value = result.nextId;
            }
        });

    document.getElementById('achievementForm').action = '/Achievement/AddAchievement';
});

// Handle form submission for both Add and Edit using AJAX
document.getElementById('achievementForm').addEventListener('submit', function (e) {
    e.preventDefault(); // Prevent default form submission
    const form = e.target;
    const actionUrl = form.action; // Get the action URL (Add or Edit)

    // Prepare form data
    const formData = new FormData(form);

    fetch(actionUrl, {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(result => {
            if (result.success) {
                // Show pop-up for success
                alert(result.message);
                document.querySelector('.btn-close').click();  // Close the modal
                location.reload(); // Reload the page to reflect changes
            } else {
                alert(result.message); // Show error message
            }
        })
        .catch(error => console.error("Error saving achievement: ", error));
});

// Handle Delete
document.querySelectorAll('.btn-delete').forEach(button => {
    button.addEventListener('click', function () {
        const achievementId = this.getAttribute('data-id');
        if (confirm('Are you sure you want to delete this achievement?')) {
            fetch(`/Achievement/DeleteAchievement`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ achievementId })
            })
                .then(response => response.json())
                .then(result => {
                    if (result.success) {
                        // Show success pop-up
                        alert(result.message);
                        location.reload(); // Reload the page after successful deletion
                    } else {
                        alert(result.message); // Show error message
                    }
                })
                .catch(error => console.error("Error deleting achievement: ", error));
        }
    });
});
 