document.addEventListener('DOMContentLoaded', function () {
    const starElements = document.querySelectorAll('.stars');
    starElements.forEach(starElement => {
        const rating = parseInt(starElement.getAttribute('data-rating'));
        starElement.innerHTML = '★★★★★'.split('').map((star, index) => {
            return `<span style="color: ${index < rating ? '#ff9800' : '#ccc'}">${star}</span>`;
        }).join('');
    });

    const removeButtons = document.querySelectorAll('.remove');
    removeButtons.forEach(button => {
        button.addEventListener('click', function () {
            const courseCard = button.closest('.course-card');
            courseCard.remove();
        });
    });
});