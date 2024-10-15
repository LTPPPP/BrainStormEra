var quill = new Quill('#editor-container', {
    theme: 'snow'
});







document.getElementById("formFile").addEventListener("change", function(event) {
    const file = event.target.files[0];
    if (file) {
        const reader = new FileReader();
        reader.onload = function(e) {
            document.getElementById("previewImage").src = e.target.result;
        }
        reader.readAsDataURL(file);
    }
});