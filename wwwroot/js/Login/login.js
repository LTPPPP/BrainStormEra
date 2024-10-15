document.addEventListener("DOMContentLoaded", () => {
    const sign_in_btn = document.querySelector("#sign-in-btn");
    const sign_up_btn = document.querySelector("#sign-up-btn");
    const container = document.querySelector(".container");

    // Toggle between Sign-In and Sign-Up forms
    if (sign_up_btn && sign_in_btn) {
        sign_up_btn.addEventListener("click", () => {
            container.classList.add("sign-up-mode");
        });

        sign_in_btn.addEventListener("click", () => {
            container.classList.remove("sign-in-mode");
        });
    }

    // Toggle password visibility
    const togglePasswordVisibility = document.querySelector("#togglePassword");
    const passwordField = document.querySelector('input[name="Password"]');

    if (togglePasswordVisibility) {
        togglePasswordVisibility.addEventListener("click", () => {
            const type = passwordField.getAttribute("type") === "password" ? "text" : "password";
            passwordField.setAttribute("type", type);

            // Toggle Font Awesome icons for password visibility
            togglePasswordVisibility.classList.toggle("fa-eye");
            togglePasswordVisibility.classList.toggle("fa-eye-slash");
        });
    }

    // Handle form submission for login
    let loginForm = document.querySelector('.sign-in-form');
    if (loginForm) {
        loginForm.addEventListener("submit", (event) => {
            event.preventDefault(); // Prevent default form submission
            let username = document.querySelector('input[name="Username"]').value;
            let password = passwordField.value;

            // Perform validation
            if (!username || !password) {
                alert("Please fill in both username and password.");
                return;
            }

            console.log("Username:", username);
            console.log("Password:", password);

            loginForm.submit();
        });
    }

    // Check for error message and display alert if there is one
    const errorMessage = document.querySelector('#errorMessage').value;
    if (errorMessage) {
        alert(errorMessage); // Display error message as a pop-up alert
    }
});
