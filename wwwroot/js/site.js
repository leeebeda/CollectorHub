document.addEventListener('DOMContentLoaded', function () {
    const details = document.querySelectorAll('details');
    details.forEach(detail => {
        detail.addEventListener('toggle', function () {
            // Здесь можно добавить AJAX-запрос для загрузки дочерних коллекций
            console.log('Развернута секция:', detail.querySelector('summary').textContent);
        });
    });
});
