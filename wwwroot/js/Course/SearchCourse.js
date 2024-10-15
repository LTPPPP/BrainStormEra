document.addEventListener('DOMContentLoaded', function () {
    const starElements = document.querySelectorAll('.stars');
    starElements.forEach(starElement => {
        const rating = parseInt(starElement.getAttribute('data-rating'));
        starElement.innerHTML = '★★★★★'.split('').map((star, index) => {
            return `<span style="color: ${index < rating ? '#ff9800' : '#ccc'}">${star}</span>`;
        }).join('');
    });

   
});