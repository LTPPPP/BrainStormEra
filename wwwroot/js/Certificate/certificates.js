  // Script for handling delete button
  document.querySelectorAll('.delete-btn').forEach(button => {
    button.addEventListener('click', function () {
        if (confirm('Are you sure you want to delete this certificate?')) {
            this.closest('.certificate-item').remove();
        }
    });
});