function switchToAddLesson() {
  setButtonActive("add-lesson-btn");
  window.location.href = "AddLesson.html";
  document.querySelector(".content-header h5").innerText =
    "ADD LESSONS FOR CHAPTER 1";
}

function switchToDeleteLesson() {
  setButtonActive("delete-lesson-btn");
  window.location.href = "DeleteLesson.html";
  document.querySelector(".content-header h5").innerText = "DELETE LESSON";
}

function switchToViewLesson() {
  setButtonActive("view-lesson-btn");
  window.location.href = "ViewLesson.html";
  document.querySelector(".content-header h5").innerText = "VIEW LESSON";
}

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
