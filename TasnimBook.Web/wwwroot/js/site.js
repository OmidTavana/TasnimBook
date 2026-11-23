document.addEventListener("DOMContentLoaded", () => {
    const button = document.querySelector("[data-nav-toggle]");
    const nav = document.querySelector("[data-nav]");
    button?.addEventListener("click", () => { const open = button.getAttribute("aria-expanded") === "true"; button.setAttribute("aria-expanded", String(!open)); nav.classList.toggle("is-open", !open); });
    nav?.querySelectorAll("a").forEach(link => link.addEventListener("click", () => { button?.setAttribute("aria-expanded", "false"); nav.classList.remove("is-open"); }));
    const observer = new IntersectionObserver(entries => entries.forEach(entry => { if (entry.isIntersecting) { entry.target.classList.add("is-visible"); observer.unobserve(entry.target); } }), { threshold: .12 });
    document.querySelectorAll(".reveal").forEach(element => observer.observe(element));
    const header = document.querySelector("[data-header]");
    addEventListener("scroll", () => header?.classList.toggle("is-scrolled", scrollY > 20), { passive: true });
});
