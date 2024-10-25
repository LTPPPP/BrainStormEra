console.log('Full Name:', '@ViewBag.FullName');
console.log('User Picture:', '@ViewBag.UserPicture');

function confirmLogout() {
var result = confirm("Are you sure you want to logout?");
if (result) {
    window.location.href = '@Url.Action("Logout", "Login")';
    }
}