function goToNextLesson() {
    const currentActive = document.querySelector(".sidebar ul li a.active");
    if (currentActive) {
        currentActive.classList.remove("active");
        const nextLi = currentActive.parentElement.nextElementSibling;
        if (nextLi && nextLi.querySelector("a")) {
            nextLi.querySelector("a").classList.add("active");
            nextLi.querySelector("a").click();
        }
    }
}

function loadLesson(title, videoSrc, posterSrc, isReading = false) {
    document.getElementById("lesson-title").innerText = title;
    document.getElementById("current-lesson").innerText = title;
    if (isReading) {
        document.getElementById("lesson-content").innerHTML =
            '<div class="reading-lesson">Reading lesson content goes here...</div>';
    } else {
        document.getElementById("lesson-content").innerHTML = (
            <video
                src="${videoSrc}"
                controls
                controlslist="nodownload"
                poster="${posterSrc}"
            ></video>
        );
    }
}
