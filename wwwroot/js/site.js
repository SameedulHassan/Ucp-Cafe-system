document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.alert').forEach(function (alert) {
        setTimeout(function () {
            if (alert.classList.contains('show')) {
                const btn = alert.querySelector('.btn-close');
                if (btn) btn.click();
            }
        }, 5000);
    });
});
