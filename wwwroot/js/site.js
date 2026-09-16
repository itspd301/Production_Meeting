// Shared client-side helpers for Production Meeting.

(function () {
    "use strict";

    // ---- Toastr defaults ----
    if (window.toastr) {
        toastr.options = {
            closeButton: true,
            progressBar: true,
            positionClass: "toast-top-right",
            timeOut: 4000
        };
    }

    // ---- Attach anti-forgery token to every AJAX/fetch call ----
    var tokenInput = document.querySelector('#antiForgeryForm input[name="__RequestVerificationToken"]');
    var csrfToken = tokenInput ? tokenInput.value : null;

    if (window.jQuery && csrfToken) {
        jQuery.ajaxSetup({
            beforeSend: function (xhr, settings) {
                if (!/^(GET|HEAD|OPTIONS|TRACE)$/i.test(settings.type)) {
                    xhr.setRequestHeader("X-CSRF-TOKEN", csrfToken);
                }
            }
        });
    }

    window.pmFetch = function (url, options) {
        options = options || {};
        options.headers = Object.assign({}, options.headers);

        var method = (options.method || "GET").toUpperCase();
        if (csrfToken && method !== "GET" && method !== "HEAD") {
            options.headers["X-CSRF-TOKEN"] = csrfToken;
        }

        return fetch(url, options);
    };

    // ---- Sidebar toggle (mobile/tablet) ----
    var toggleBtn = document.getElementById("pmSidebarToggle");
    var sidebar = document.getElementById("pmSidebar");
    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener("click", function () {
            sidebar.classList.toggle("pm-sidebar-open");
        });
    }

    // ---- Global AJAX loader ----
    window.pmShowLoader = function () {
        var loader = document.getElementById("pmLoader");
        if (loader) loader.hidden = false;
    };
    window.pmHideLoader = function () {
        var loader = document.getElementById("pmLoader");
        if (loader) loader.hidden = true;
    };

    // ---- Reusable confirm-delete modal helper ----
    // Usage: pmConfirm("Delete this record?", function () { ...do delete... });
    window.pmConfirm = function (message, onConfirm) {
        var modalEl = document.getElementById("pmConfirmModal");
        if (!modalEl) {
            if (confirm(message)) onConfirm();
            return;
        }

        modalEl.querySelector(".pm-confirm-message").textContent = message;
        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);

        var confirmBtn = modalEl.querySelector(".pm-confirm-ok");
        var handler = function () {
            modal.hide();
            confirmBtn.removeEventListener("click", handler);
            onConfirm();
        };
        confirmBtn.addEventListener("click", handler);

        modal.show();
    };
})();
