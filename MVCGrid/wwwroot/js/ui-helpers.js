/**
 * UI Helpers for Dental Care Management System
 * Provides AJAX form handling, grid refresh, and alert management
 */

// Global configuration
const UIHelpers = {
    autoRefreshInterval: 15000, // 15 seconds
    autoRefreshTimers: {},

    /**
     * Initialize AJAX form handler for a specific form selector
     * @param {string} formSelector - jQuery selector for the form
     * @param {function} onSuccess - Callback function on successful submission
     */
    ajaxFormHandler: function(formSelector, onSuccess) {
        $(document).on('submit', formSelector, function(e) {
            e.preventDefault();
            
            const $form = $(this);
            const $submitBtn = $form.find('button[type="submit"]');
            const originalBtnText = $submitBtn.html();
            
            // Disable button and show loading
            $submitBtn.prop('disabled', true)
                .html('<i class="fas fa-spinner fa-spin"></i> Processing...');
            
            $.ajax({
                url: $form.attr('action'),
                type: $form.attr('method') || 'POST',
                data: $form.serialize(),
                success: function(response) {
                    // Handle JSON response
                    if (typeof response === 'object') {
                        if (response.success) {
                            UIHelpers.showAlert(response.message, 'success');
                            
                            // Close modal if exists
                            const modal = bootstrap.Modal.getInstance($form.closest('.modal')[0]);
                            if (modal) {
                                modal.hide();
                            }
                            
                            // Execute success callback
                            if (onSuccess) {
                                onSuccess(response);
                            }
                        } else {
                            UIHelpers.showAlert(response.message, 'danger');
                        }
                    }
                    // Handle partial view response (validation errors)
                    else {
                        const $modalBody = $form.closest('.modal-body');
                        if ($modalBody.length > 0) {
                            $modalBody.html(response);
                        }
                    }
                },
                error: function(xhr, status, error) {
                    console.error('AJAX Error:', error);
                    UIHelpers.showAlert('An error occurred while processing your request.', 'danger');
                },
                complete: function() {
                    // Re-enable button
                    $submitBtn.prop('disabled', false).html(originalBtnText);
                }
            });
            
            return false;
        });
    },

    /**
     * Show alert message using toastr
     * @param {string} message - The message to display
     * @param {string} type - Alert type (success, danger, warning, info)
     */
    showAlert: function(message, type) {
        type = type || 'info';
        
        // Map Bootstrap alert types to toastr types
        const toastrType = {
            'success': 'success',
            'danger': 'error',
            'warning': 'warning',
            'info': 'info'
        }[type] || 'info';
        
        // Use toastr
        toastr[toastrType](message);
    },

    /**
     * Get icon for alert type
     */
    getAlertIcon: function(type) {
        const icons = {
            'success': 'check-circle',
            'danger': 'exclamation-circle',
            'warning': 'exclamation-triangle',
            'info': 'info-circle'
        };
        return icons[type] || 'info-circle';
    },

    /**
     * Reload one or more MVC grids
     * @param {string|array} gridNames - Grid name(s) to reload
     */
    reloadGrids: function(gridNames) {
        if (!Array.isArray(gridNames)) {
            gridNames = [gridNames];
        }
        
        gridNames.forEach(function(gridName) {
            const gridElement = document.getElementById(gridName);
            if (gridElement && typeof MvcGrid !== 'undefined') {
                new MvcGrid(gridElement).reload();
            }
        });
    },

    /**
     * Reload grid via AJAX by fetching partial view
     * @param {string} containerSelector - jQuery selector for container
     * @param {string} url - URL to fetch the partial view
     * @param {object} data - Additional data to send
     * @param {function} callback - Callback after reload
     */
    reloadGridPartial: function(containerSelector, url, data, callback) {
        $.ajax({
            url: url,
            type: 'GET',
            data: data || {},
            beforeSend: function() {
                $(containerSelector).css('opacity', '0.5');
            },
            success: function(html) {
                $(containerSelector).html(html);
                
                // Reinitialize MVC Grid if the library is loaded
                if (typeof MvcGrid !== 'undefined') {
                    $(containerSelector).find('.mvc-grid').each(function() {
                        try {
                            new MvcGrid(this);
                        } catch (e) {
                            console.warn('MvcGrid initialization skipped:', e.message);
                        }
                    });
                }
                
                $(containerSelector).css('opacity', '1');
                
                if (callback) {
                    callback();
                }
            },
            error: function(xhr, status, error) {
                console.error('Grid reload error:', error);
                $(containerSelector).css('opacity', '1');
                UIHelpers.showAlert('Failed to refresh data.', 'danger');
            }
        });
    },

    /**
     * Enable auto-refresh for a grid
     * @param {string} gridName - Name of the grid
     * @param {string} containerSelector - jQuery selector for container
     * @param {string} url - URL to fetch the partial
     * @param {object} data - Data to send with refresh
     */
    enableAutoRefresh: function(gridName, containerSelector, url, data) {
        // Clear existing timer if any
        if (UIHelpers.autoRefreshTimers[gridName]) {
            clearInterval(UIHelpers.autoRefreshTimers[gridName]);
        }
        
        // Set up new timer
        UIHelpers.autoRefreshTimers[gridName] = setInterval(function() {
            UIHelpers.reloadGridPartial(containerSelector, url, data);
        }, UIHelpers.autoRefreshInterval);
        
        console.log(`Auto-refresh enabled for ${gridName} (every ${UIHelpers.autoRefreshInterval/1000}s)`);
    },

    /**
     * Disable auto-refresh for a grid
     * @param {string} gridName - Name of the grid
     */
    disableAutoRefresh: function(gridName) {
        if (UIHelpers.autoRefreshTimers[gridName]) {
            clearInterval(UIHelpers.autoRefreshTimers[gridName]);
            delete UIHelpers.autoRefreshTimers[gridName];
            console.log(`Auto-refresh disabled for ${gridName}`);
        }
    },

    /**
     * Load content into modal
     * @param {string} url - URL to load
     * @param {string} modalTitle - Title for the modal
     */
    loadModal: function(url, modalTitle) {
        $.ajax({
            url: url,
            type: 'GET',
            success: function(html) {
                const modalId = 'actionModal';
                let $modal = $(`#${modalId}`);
                
                // Create modal if it doesn't exist
                if ($modal.length === 0) {
                    const modalHtml = `
                        <div class="modal fade" id="${modalId}" tabindex="-1">
                            <div class="modal-dialog modal-lg">
                                <div class="modal-content" id="modalContainer">
                                </div>
                            </div>
                        </div>
                    `;
                    $('body').append(modalHtml);
                    $modal = $(`#${modalId}`);
                }
                
                // Set content
                $('#modalContainer').html(html);
                
                // Update title if provided
                if (modalTitle) {
                    $('#modalContainer .modal-title').text(modalTitle);
                }
                
                // Show modal
                const modal = new bootstrap.Modal($modal[0]);
                modal.show();
            },
            error: function() {
                UIHelpers.showAlert('Failed to load content.', 'danger');
            }
        });
    },

    /**
     * Confirm action with custom message
     * @param {string} message - Confirmation message
     * @param {function} onConfirm - Callback if user confirms
     */
    confirm: function(message, onConfirm) {
        if (confirm(message)) {
            onConfirm();
        }
    },

    /**
     * Perform AJAX action (e.g., status update, delete)
     * @param {string} url - URL to post to
     * @param {object} data - Data to send
     * @param {function} onSuccess - Success callback
     */
    ajaxAction: function(url, data, onSuccess) {
        // Add anti-forgery token
        data.__RequestVerificationToken = $('input[name="__RequestVerificationToken"]').val();
        
        $.ajax({
            url: url,
            type: 'POST',
            data: data,
            success: function(response) {
                if (response.success) {
                    UIHelpers.showAlert(response.message, 'success');
                    if (onSuccess) {
                        onSuccess(response);
                    }
                } else {
                    UIHelpers.showAlert(response.message, 'danger');
                }
            },
            error: function() {
                UIHelpers.showAlert('Action failed.', 'danger');
            }
        });
    },

    /**
     * Initialize MVC Grid instances
     */
    initGrids: function() {
        if (typeof MvcGrid !== 'undefined') {
            document.querySelectorAll('.mvc-grid').forEach(function(el) {
                try {
                    new MvcGrid(el);
                } catch (e) {
                    console.warn('MvcGrid initialization skipped for element:', el, e.message);
                }
            });
            console.log('MVCGrid instances initialized');
        } else {
            console.warn('MvcGrid library not loaded');
        }
    },

    /**
     * Refresh a partial view container
     * @param {string} containerSelector - jQuery selector
     * @param {string} url - URL to fetch partial
     * @param {object} data - Data to send
     */
    refreshPartial: function(containerSelector, url, data) {
        $.ajax({
            url: url,
            type: 'GET',
            data: data || {},
            success: function(html) {
                $(containerSelector).html(html);
                // Reinitialize grids after refresh
                UIHelpers.initGrids();
            },
            error: function() {
                console.error('Failed to refresh partial:', containerSelector);
            }
        });
    }
};

// Export to global scope
window.UIHelpers = UIHelpers;

// Initialize grids on document ready
$(document).ready(function() {
    UIHelpers.initGrids();
    
    // Reinitialize grids after any AJAX complete
    $(document).ajaxComplete(function() {
        UIHelpers.initGrids();
    });
});
