document.addEventListener('DOMContentLoaded', function () {
    // Attach event listeners to all achievement cards
    document.querySelectorAll('.achievement-card').forEach(function (card) {
        card.addEventListener('click', function () {
            const achievementId = this.getAttribute('data-id');
            const userId = this.getAttribute('data-user-id');

            // Make an AJAX request to fetch achievement details
            fetch(`/Achievement/GetAchievementDetails?achievementId=${achievementId}&userId=${userId}`)
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        const achievement = data.data;

                        // Populate modal with achievement details
                        document.getElementById('achievement-name').textContent = achievement.AchievementName || 'N/A';
                        document.getElementById('achievement-description').textContent = achievement.AchievementDescription || 'N/A';
                        document.getElementById('achievement-icon').setAttribute('src', achievement.AchievementIcon || '/path/to/default/icon.png');
                        document.getElementById('achievement-date').textContent = achievement.ReceivedDate ? new Date(achievement.ReceivedDate).toLocaleDateString() : 'N/A';

                        // Show the modal
                        const achievementModal = new bootstrap.Modal(document.getElementById('achievementModal'));
                        achievementModal.show();
                    } else {
                        alert('Failed to load achievement details');
                    }
                })
                .catch(error => console.error('Error fetching achievement details:', error));
        });
    });
});