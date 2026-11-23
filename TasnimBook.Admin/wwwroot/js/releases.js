document.addEventListener("DOMContentLoaded", () => {
    const form = document.querySelector("[data-release-upload]");
    if (!form) return;
    const submit = form.querySelector("[data-upload-submit]");
    const box = form.querySelector("[data-upload-progress]");
    const bar = box.querySelector("progress");
    const percent = box.querySelector("[data-upload-percent]");
    const label = box.querySelector("[data-upload-label]");
    const feedback = form.querySelector("[data-upload-feedback]");
    form.addEventListener("submit", event => {
        event.preventDefault();
        if (!form.reportValidity() || submit.disabled) return;
        submit.disabled = true; box.hidden = false; feedback.textContent = ""; bar.value = 0;
        const request = new XMLHttpRequest();
        request.open("POST", form.action);
        request.setRequestHeader("X-Requested-With", "XMLHttpRequest");
        request.upload.addEventListener("progress", progress => {
            if (!progress.lengthComputable) return;
            const value = Math.round(progress.loaded / progress.total * 100);
            bar.value = value; percent.textContent = `${value}٪`;
            if (value === 100) label.textContent = "در حال اعتبارسنجی و ذخیره امن فایل…";
        });
        request.addEventListener("load", () => {
            let payload = {}; try { payload = JSON.parse(request.responseText); } catch { }
            if (request.status >= 200 && request.status < 300) { feedback.className = "form-feedback is-success"; feedback.textContent = payload.message || "نسخه ثبت شد."; setTimeout(() => location.reload(), 700); }
            else { feedback.className = "form-feedback is-error"; feedback.textContent = payload.message || "ثبت نسخه انجام نشد."; submit.disabled = false; }
        });
        request.addEventListener("error", () => { feedback.className = "form-feedback is-error"; feedback.textContent = "ارتباط با سرور قطع شد؛ دوباره تلاش کنید."; submit.disabled = false; });
        request.send(new FormData(form));
    });
});
