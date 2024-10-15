document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('deleteForm').addEventListener('submit', function (event) {
        let confirmDelete = confirm('Are you sure you want to delete the selected chapters? This action cannot be undone.');
        if (!confirmDelete) {
            event.preventDefault();  // Prevent form submission if the user cancels
        }
    });
});