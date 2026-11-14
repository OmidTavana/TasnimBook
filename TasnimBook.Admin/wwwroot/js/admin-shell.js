(() => {
    const root = document.documentElement;
    const body = document.body;
    const storedTheme = window.localStorage.getItem("tasnim-admin-theme");

    if (storedTheme === "light" || storedTheme === "dark") {
        root.dataset.theme = storedTheme;
    }

    document.querySelector("[data-theme-toggle]")?.addEventListener("click", () => {
        const nextTheme = root.dataset.theme === "dark" ? "light" : "dark";

        root.dataset.theme = nextTheme;
        window.localStorage.setItem("tasnim-admin-theme", nextTheme);
    });

    const closeSidebar = () => body.classList.remove("sidebar-is-open");

    document.querySelector("[data-sidebar-toggle]")?.addEventListener("click", () => {
        body.classList.toggle("sidebar-is-open");
    });

    document.querySelector("[data-sidebar-close]")?.addEventListener("click", closeSidebar);

    document.querySelectorAll("[data-dismiss]").forEach((button) => {
        button.addEventListener("click", () => {
            button.closest("[data-dismissible]")?.remove();
        });
    });

    document.querySelectorAll("[data-confirm]").forEach((form) => {
        form.addEventListener("submit", (event) => {
            const message = form.dataset.confirm;

            if (message && !window.confirm(message)) {
                event.preventDefault();
            }
        });
    });

    document.querySelectorAll("[data-password-toggle]").forEach((button) => {
        button.addEventListener("click", () => {
            const input = document.querySelector(button.dataset.passwordToggle);

            if (!(input instanceof HTMLInputElement)) {
                return;
            }

            const passwordIsVisible = input.type === "text";
            input.type = passwordIsVisible ? "password" : "text";
            button.setAttribute("aria-pressed", String(!passwordIsVisible));
        });
    });

    document.querySelectorAll("[data-upload-zone]").forEach((zone) => {
        const input = zone.querySelector("input[type='file']");
        const fileName = zone.querySelector("[data-file-name]");
        const fileSize = zone.querySelector("[data-file-size]");
        const image = zone.querySelector("[data-image-preview]");

        if (!(input instanceof HTMLInputElement)) {
            return;
        }

        const showFile = (file) => {
            if (!file) {
                return;
            }

            if (fileName) {
                fileName.textContent = file.name;
            }

            if (fileSize) {
                fileSize.textContent = formatFileSize(file.size);
            }

            if (image instanceof HTMLImageElement && file.type.startsWith("image/")) {
                image.src = URL.createObjectURL(file);
                image.hidden = false;
                zone.classList.add("has-preview");
            }
        };

        input.addEventListener("change", () => showFile(input.files?.[0]));

        ["dragenter", "dragover"].forEach((eventName) => {
            zone.addEventListener(eventName, (event) => {
                event.preventDefault();
                zone.classList.add("is-dragging");
            });
        });

        ["dragleave", "drop"].forEach((eventName) => {
            zone.addEventListener(eventName, (event) => {
                event.preventDefault();
                zone.classList.remove("is-dragging");
            });
        });

        zone.addEventListener("drop", (event) => {
            const file = event.dataTransfer?.files?.[0];

            if (!file) {
                return;
            }

            const transfer = new DataTransfer();
            transfer.items.add(file);
            input.files = transfer.files;
            showFile(file);
        });
    });

    function formatFileSize(bytes) {
        if (bytes < 1024 * 1024) {
            return `${Math.max(1, Math.round(bytes / 1024))} کیلوبایت`;
        }

        return `${(bytes / 1024 / 1024).toFixed(1)} مگابایت`;
    }
})();
