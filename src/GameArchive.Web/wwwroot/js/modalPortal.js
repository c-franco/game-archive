window.modalPortal = {
    move: function (id) {
        const el = document.getElementById(id);
        if (el && el.parentElement !== document.body) {
            document.body.appendChild(el);
        }
    },
    cleanup: function (id) {
        const el = document.getElementById(id);
        if (el) el.remove();
    }
};
