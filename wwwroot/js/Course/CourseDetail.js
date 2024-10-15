function toggleLesson(part) {
    part.classList.toggle('active');
}

function selectStar(star) {
    const stars = star.parentElement.querySelectorAll('i');
    let index = Array.from(stars).indexOf(star);

    if (star.classList.contains('selected')) {
        stars.forEach(s => s.classList.remove('selected'));
        console.log(0); // No star selected
    } else {
        stars.forEach((s, i) => {
            if (i <= index) {
                s.classList.add('selected');
            } else {
                s.classList.remove('selected');
            }
        });
        console.log(index + 1); // Log number of stars selected
    }
}

function toggleOptionsMenu(icon) {
    const menu = icon.nextElementSibling;
    menu.style.display = menu.style.display === 'block' ? 'none' : 'block';
}