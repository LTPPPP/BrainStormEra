$(document).on('click', '.btn-edit', function () {
    const achievementId = $(this).data('id');
    $.get('/Achievement/GetAchievement', { achievementId }, function (response) {
        if (response.success) {
            $('#achievementId').val(response.data.achievementId).prop('readonly', true);
            $('#achievementName').val(response.data.achievementName);
            $('#achievementDescription').val(response.data.achievementDescription);
            $('#achievementIcon').val(response.data.achievementIcon);
            $('#achievementCreatedAt').val(response.data.achievementCreatedAt.split('T')[0]);
            $('#achievementForm').attr('action', '/Achievement/EditAchievement');
            $('#achievementModal').modal('show');
        } else {
            alert(response.message);
        }
    });
});

$('.btn-add').on('click', function () {
    // Clear previous values in the form
    $('#achievementId').val('');
    $('#achievementName').val('');
    $('#achievementDescription').val('');
    $('#achievementIcon').val('');

    // Set today's date in the 'achievementCreatedAt' field
    const today = new Date().toISOString().split('T')[0]; // Get current date in yyyy-MM-dd format
    $('#achievementCreatedAt').val(today);

    // Switch form action to AddAchievement
    $('#achievementForm').attr('action', '/Achievement/AddAchievement');
});


$(document).on('click', '.btn-delete', function () {
    const achievementId = $(this).data('id');

    if (confirm('Are you sure you want to delete this achievement?')) {
        $.post('/Achievement/DeleteAchievement', { achievementId: achievementId }, function (response) {
            if (response.success) {
                // Remove the row from the table
                $('#achievement-row-' + achievementId).remove();
                alert(response.message);
            } else {
                alert(response.message);
            }
        });
    }
});
