function toggleSidebar() {
    $('#sidebar').toggleClass('open');
    $('#sidebarOverlay').toggleClass('show');
}
$(document).click(function(event) {
    if (window.innerWidth <= 768) {
        if (!$(event.target).closest('#sidebar, .menu-toggle').length) {
            $('#sidebar').removeClass('open');
            $('#sidebarOverlay').removeClass('show');
        }
    }
});
document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".mvc-grid").forEach(el => new MvcGrid(el));
    toggleTable();
});
function toggleTable() {
    if (window.innerWidth < 768) {
        if (!$('.mvc-grid table').hasClass('footable-loaded')) {
            $('.mvc-grid table').footable();
            $('.mvc-grid table').addClass('footable-loaded');
        }
    } else {
        if ($('.mvc-grid table').hasClass('footable-loaded')) {
            $('.mvc-grid table').footable('destroy');
            $('.mvc-grid table').removeClass('footable-loaded');
        }
    }
}
$(window).on('resize', toggleTable);
