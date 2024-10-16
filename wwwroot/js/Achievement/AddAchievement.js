// JavaScript Code
document.addEventListener('DOMContentLoaded', function() {
    const modalOverlay = document.getElementById('modal-overlay');

    // Function to open the modal with a specific context
    window.openPopup = function(context) {
        console.log("Opening popup for: ", context); // Debugging line to see the context
        modalOverlay.style.display = 'flex';
    };

    // Function to close the modal
    function closePopup() {
        modalOverlay.style.display = 'none';
    }

    // Event listener for closing the modal when clicking outside
    modalOverlay.addEventListener('click', function(event) {
        if (event.target === modalOverlay) {
            closePopup();
        }
    });

    // Event listeners for close button inside modal might go here
    const closeButton = modalOverlay.querySelector('.cancel-btn');
    if (closeButton) {
        closeButton.addEventListener('click', closePopup);
    }
});
