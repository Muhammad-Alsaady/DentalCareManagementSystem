/**
 * Left Menu (Sidebar) Controls
 * Handles sidebar toggle, active states, and mobile behavior
 */

$(document).ready(function() {
    // Sidebar toggle button
    $('#sidebarToggle').click(function() {
        toggleSidebar();
    });

    // Toggle submenu
    $('.has-submenu > .nav-link').click(function(e) {
        e.preventDefault();
        $(this).parent().toggleClass('open');
    });

    // Set active menu item based on current URL
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

    // Handle window resize
    $(window).resize(function() {
        if ($(window).width() > 768) {
            // Reset mobile sidebar state on desktop
            $('#sidebar').removeClass('open');
            $('#sidebarOverlay').removeClass('show');
        }
    });
});

/**
 * Toggle sidebar open/closed
 */
function toggleSidebar() {
    const sidebar = $('#sidebar');
    const overlay = $('#sidebarOverlay');
    const mainWrapper = $('.main-wrapper');

    if ($(window).width() <= 768) {
        // Mobile: slide in/out
        sidebar.toggleClass('open');
        overlay.toggleClass('show');
    } else {
        // Desktop: collapse/expand
        if (sidebar.hasClass('collapsed')) {
            sidebar.removeClass('collapsed');
            mainWrapper.css('margin-left', '260px');
        } else {
            sidebar.addClass('collapsed');
            mainWrapper.css('margin-left', '0');
        }
    }
}

/**
 * Close sidebar
 */
function closeSidebar() {
    $('#sidebar').removeClass('open');
    $('#sidebarOverlay').removeClass('show');
}

/**
 * Open sidebar
 */
function openSidebar() {
    $('#sidebar').addClass('open');
    $('#sidebarOverlay').addClass('show');
}
