$(document).ready(function() {
    // Toggle submenu
    $('.has-submenu > .nav-link').click(function(e) {
        e.preventDefault();
        $(this).parent().toggleClass('open');
    });

    // Set active menu item
    var currentPath = window.location.pathname;
    $('.nav-link, .submenu-link').each(function() {
        var href = $(this).attr('href');
        if (href && href !== '#' && href !== 'javascript:void(0)') {
            if (currentPath.indexOf(href) > -1 || href === currentPath) {
                $(this).addClass('active');
                $(this).closest('.has-submenu').addClass('open');
            }
        }
    });

    // Close sidebar when clicking overlay (mobile)
    $('#sidebarOverlay').click(function() {
        closeSidebar();
    });
});

function closeSidebar() {
    $('#sidebar').removeClass('open');
    $('#sidebarOverlay').removeClass('show');
}
