document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.achievement-card').forEach(function (card) {
        card.addEventListener('click', function () {
            const achievementName = this.getAttribute('data-name');
            const achievementDescription = this.getAttribute('data-description');
            const achievementIcon = this.getAttribute('data-icon');
            const achievementDate = this.getAttribute('data-date');

            // Populate modal with achievement details
            document.getElementById('achievement-name').textContent = achievementName;
            document.getElementById('achievement-description').textContent = achievementDescription;
            document.getElementById('achievement-icon').setAttribute('src', achievementIcon);
            document.getElementById('achievement-date').textContent = achievementDate;
        });
    });
});
