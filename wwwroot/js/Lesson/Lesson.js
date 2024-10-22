function setButtonActive(buttonId) {
    document.querySelectorAll(".btn").forEach((button) => {
        button.classList.remove("btn-primary");
        button.classList.add("btn-outline-secondary");
    });
    document.getElementById(buttonId).classList.add("btn-primary");
    document.getElementById(buttonId).classList.remove("btn-outline-secondary");
}
var quill;
document.addEventListener("DOMContentLoaded", () => {
    quill = new Quill("#editor", {
        modules: {
            toolbar: "#toolbar-container",
        },
        theme: "snow",
    });

    document.getElementById("videoSection").style.display = "none";
    document.getElementById("readingSection").style.display = "none";
    document.getElementById("createVideoBtn").disabled = false;
    document.getElementById("createReadingBtn").disabled = false;

    document.getElementById("lessonType").addEventListener("change", function () {
        var lessonType = this.value;  // Get the selected lesson type
        var videoSection = document.getElementById('videoSection');  // Get the video section div
        if (this.value == "1") { // 1 là video
            document.getElementById("videoSection").style.display = "block";
            document.getElementById("readingSection").style.display = "none";
            document.getElementById("createVideoBtn").disabled = false;  // Bật nút "Create Video"
            document.getElementById("createReadingBtn").disabled = true; // Tắt nút "Create Reading"
        } else if (this.value == "2") { // 2 là reading
            document.getElementById("videoSection").style.display = "none";
            document.getElementById("readingSection").style.display = "block";
            document.getElementById("createVideoBtn").disabled = true;   // Tắt nút "Create Video"
            document.getElementById("createReadingBtn").disabled = false; // Bật nút "Create Reading"
        } else {
            document.getElementById("videoSection").style.display = "none";
            document.getElementById("readingSection").style.display = "none";
            document.getElementById("createVideoBtn").disabled = false;   // Bật lại cả hai nút khi không có lựa chọn
            document.getElementById("createReadingBtn").disabled = false;
        }
    });

    // Sao chép nội dung Quill vào input ẩn trước khi submit form
    document.querySelector('form').onsubmit = function () {
        var content = quill.root.innerHTML;
        document.querySelector('#lessonContent').value = content;
    };
});

function toggleSection(sectionId) {
    const section = document.getElementById(sectionId);
    if (section.style.display === "none" || section.style.display === "") {
        section.style.display = "block";
    } else {
        section.style.display = "none";
    }
}

document.addEventListener("DOMContentLoaded", () => {
    document.getElementById("videoSection").style.display = "none";
    document.getElementById("readingSection").style.display = "none";
});


const fileInput = document.getElementById("fileInput");
const fileDetails = document.getElementById("fileDetails");
const notification = document.getElementById("customNotification");


// Handle form submission
function submitForm() {
    var fileInput = document.getElementById('fileInput');
    var file = fileInput.files[0];

    if (file) {
        // Hiển thị tên file sau khi upload
        document.getElementById('uploadedFileName').innerHTML = file.name;

        // Gọi hàm showNotification để hiển thị thông báo
        showNotification('File uploaded successfully!', 'success');
        setTimeout(function () {
            document.getElementById('customNotification').style.display = 'none';
        }, 5000);
    } else {
        alert('Please select a file to upload!');
    }
}
function toggleLessonType(type) {
    var videoSection = document.getElementById("videoSection");
    var readingSection = document.getElementById("readingSection");

    if (type === "1") { // Video
        videoSection.style.display = "block";
        readingSection.style.display = "none";
    } else if (type === "2") { // Reading
        videoSection.style.display = "none";
        readingSection.style.display = "block";
    } else {
        videoSection.style.display = "none";
        readingSection.style.display = "none";
    }
}