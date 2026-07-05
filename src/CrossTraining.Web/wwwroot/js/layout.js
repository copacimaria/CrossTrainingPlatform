// Comportament sidebar + submeniu admin (extras din _Layout inline).
document.addEventListener("DOMContentLoaded", function () {
    const sidebarToggle = document.getElementById("sidebarToggle");
    const sidebar = document.getElementById("sidebar");
    const closeSidebar = document.getElementById("closeSidebar");

    if (sidebarToggle && sidebar) {
        sidebarToggle.addEventListener("click", () => sidebar.classList.add("active"));
    }

    if (closeSidebar && sidebar) {
        closeSidebar.addEventListener("click", () => sidebar.classList.remove("active"));
    }

    const adminToggle = document.getElementById("adminToggle");
    const adminSubmenu = document.getElementById("adminSubmenu");
    if (adminToggle && adminSubmenu) {
        adminToggle.addEventListener("click", function (e) {
            e.preventDefault();
            adminSubmenu.classList.toggle("open");
        });
    }
});
