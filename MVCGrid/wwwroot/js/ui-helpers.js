/**
 * UI Helpers for Dental Care Management System
 * Provides AJAX form handling, grid refresh, and alert management
 */

// Configure toastr globally
toastr.options = {
    "closeButton": true,
    "progressBar": true,
    "positionClass": "toast-top-right",
    "timeOut": "3000"
};

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
     * Confirm action with SweetAlert2
     * @param {string} title - The title of the confirmation
     * @param {string} message - Confirmation message
     * @param {function} onConfirm - Callback if user confirms
     * @param {string} confirmButtonClass - CSS class for confirm button (not used in Swal, kept for compatibility)
     */
    showConfirmation: function(title, message, onConfirm, confirmButtonClass) {
        Swal.fire({
            title: title || 'Confirm Action',
            html: message,
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Yes, proceed',
            cancelButtonText: 'Cancel',
            reverseButtons: true
        }).then((result) => {
            if (result.isConfirmed) {
                if (onConfirm) {
                    onConfirm();
                }
            }
        });
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
     * Shows a toastr confirmation dialog.
     * @param {string} title - The title of the confirmation.
     * @param {string} message - The message/question to ask.
     * @param {function} onConfirm - The function to call when the user clicks 'Yes'.
     * @param {string} confirmButtonClass - CSS class for the confirm button.
     * @param {string} confirmButtonText - Optional text for the confirm button.
     * @param {string} cancelButtonText - Optional text for the cancel button.
     */
    showConfirmation: function(title, message, onConfirm, confirmButtonClass, confirmButtonText, cancelButtonText) {
        confirmButtonText = confirmButtonText || 'Yes';
        cancelButtonText = cancelButtonText || 'Cancel';
        confirmButtonClass = confirmButtonClass || 'btn-primary';

        const buttons = `
            <div class="mt-3 text-center">
                <button type="button" class="btn ${confirmButtonClass} btn-sm me-2" id="toastr-confirm-btn">${confirmButtonText}</button>
                <button type="button" class="btn btn-secondary btn-sm" id="toastr-cancel-btn">${cancelButtonText}</button>
            </div>`;

        // Show the toast
        const toastrInstance = toastr.warning(message + buttons, title, {
            closeButton: false,
            timeOut: 0,
            extendedTimeOut: 0,
            tapToDismiss: false,
            allowHtml: true,
            preventDuplicates: true,
            positionClass: "toast-top-center",
            toastClass: "toastr-confirm",
            onShown: function(toast) {
                const confirmBtn = toast.find('#toastr-confirm-btn');
                const cancelBtn = toast.find('#toastr-cancel-btn');

                confirmBtn.on('click', function() {
                    if (onConfirm) {
                        onConfirm();
                    }
                    toastr.clear(toast);
                });

                cancelBtn.on('click', function() {
                    toastr.clear(toast);
                });
            }
        });
    },

    /**
     * Shows a standardized delete confirmation dialog using SweetAlert2.
     * @param {string} title - The title of the confirmation.
     * @param {string} message - The message/question to ask.
     * @param {function} onConfirm - The function to call when the user clicks 'Yes, Delete'.
     */
    showDeleteConfirmation: function(title, message, onConfirm) {
        Swal.fire({
            title: title || 'Are you sure?',
            html: message || "You won't be able to revert this!",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#3085d6',
            confirmButtonText: 'Yes, delete it!',
            cancelButtonText: 'Cancel',
            reverseButtons: true,
            focusCancel: true
        }).then((result) => {
            if (result.isConfirmed) {
                if (onConfirm) {
                    onConfirm();
                }
            }
        });
    },

    /**
     * Standardized delete function with toastr confirmation
     * @param {string} url - The URL to post the delete request to
     * @param {string} rowId - The ID of the row/item to delete (optional, for fadeOut)
     * @param {string} gridName - The name of the MVC Grid to reload (optional)
     * @param {string} itemName - The name of the item being deleted (for messages)
     */
    deleteItem: function(url, rowId, gridName, itemName) {
        itemName = itemName || 'item';
        
        if (!confirm(`Are you sure you want to delete this ${itemName}?`)) {
            return;
        }

        $.ajax({
            url: url,
            type: 'POST',
            data: {
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function(response) {
                if (response.success) {
                    toastr.success(response.message || 'Deleted successfully!');

                    // Remove row with fade effect if rowId provided
                    if (rowId) {
                        $("#row-" + rowId).fadeOut(300);
                    }

                    // Reload MVC Grid if gridName provided
                    if (gridName && typeof MvcGrid !== 'undefined') {
                        const gridElement = document.getElementById(gridName);
                        if (gridElement) {
                            new MvcGrid(gridElement).reload();
                        }
                    }
                } else {
                    toastr.error(response.message || 'Delete failed! Please try again.');
                }
            },
            error: function() {
                toastr.error('Delete failed! Please try again.');
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
