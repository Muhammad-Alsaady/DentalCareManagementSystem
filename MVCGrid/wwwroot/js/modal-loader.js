/**
 * Global Modal Loader - Unified modal system for entire application
 * FIXED: All syntax errors, duplicate variables, and modal conflicts resolved
 */

var ModalLoader = (function () {
    'use strict';

    // Private variables
    var modal;
    var modalElement;
    var titleElement;
    var titleTextElement;
    var titleLoaderElement;
    var bodyElement;
    var footerElement;
    var loadingSpinner;

    /**
     * Initialize modal elements
     */
    function init() {
        modalElement = document.getElementById('globalModal');
        titleElement = document.getElementById('globalModalTitle');
        titleTextElement = document.getElementById('modalTitleText');
        titleLoaderElement = document.getElementById('modalTitleLoader');
        bodyElement = document.getElementById('globalModalBody');
        footerElement = document.getElementById('globalModalFooter');
        loadingSpinner = document.getElementById('modalLoadingSpinner');

        if (modalElement) {
            // Create modal with correct settings
            modal = new bootstrap.Modal(modalElement, {
                backdrop: true,  // Allow clicking outside to close
                keyboard: true,  // Allow ESC key to close
                focus: true
            });

            // Listen for modal hidden event to clean up
            modalElement.addEventListener('hidden.bs.modal', function () {
                cleanupModal();
            });

            // Listen for modal shown event
            modalElement.addEventListener('shown.bs.modal', function() {
                // Focus first input after modal is fully shown
                setTimeout(function() {
                    var firstInput = bodyElement.querySelector('input:not([type=hidden]), select, textarea');
                    if (firstInput) {
                        firstInput.focus();
                    }
                }, 150);
            });
        }
    }

    /**
     * Complete cleanup of modal and backdrop
     */
    function cleanupModal() {
        // Reset body content
        if (bodyElement) {
            bodyElement.innerHTML = `
                <div class="text-center py-5" id="modalLoadingSpinner">
                    <div class="spinner-border text-primary" role="status" style="width: 3rem; height: 3rem;">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                    <p class="mt-3 text-muted">Loading content...</p>
                </div>
            `;
        }

        // Reset footer
        if (footerElement) {
            footerElement.style.display = 'none';
            footerElement.innerHTML = '';
        }

        // Reset title
        if (titleTextElement) {
            titleTextElement.textContent = 'Loading...';
        }

        // Show loader
        if (titleLoaderElement) {
            titleLoaderElement.style.display = 'inline-block';
        }
        
        // Remove any stray backdrops after Bootstrap finishes its animation
        setTimeout(function() {
            removeAllBackdrops();
        }, 350);
        
        // Re-enable body scroll
        document.body.classList.remove('modal-open');
        document.body.style.overflow = '';
        document.body.style.paddingRight = '';
    }

    /**
     * Remove all modal backdrops (fix for multiple backdrops)
     */
    function removeAllBackdrops() {
        var backdrops = document.querySelectorAll('.modal-backdrop');
        backdrops.forEach(function (backdrop) {
            backdrop.remove();
        });
    }

    /**
     * Reset modal to initial state (without closing)
     */
    function resetModal() {
        if (bodyElement) {
            bodyElement.innerHTML = `
                <div class="text-center py-5" id="modalLoadingSpinner">
                    <div class="spinner-border text-primary" role="status" style="width: 3rem; height: 3rem;">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                    <p class="mt-3 text-muted">Loading content...</p>
                </div>
            `;
        }
        if (footerElement) {
            footerElement.style.display = 'none';
            footerElement.innerHTML = '';
        }
        if (titleTextElement) {
            titleTextElement.textContent = 'Loading...';
        }
        if (titleLoaderElement) {
            titleLoaderElement.style.display = 'inline-block';
        }
    }

    /**
     * Load content into modal via AJAX
     * @param {string} url - URL to load content from
     * @param {string} title - Modal title (optional)
     * @param {object} options - Additional options (optional)
     */
    function loadModal(url, title, options) {
        if (!modal) {
            init();
        }

        // Set default options
        options = options || {};
        var method = options.method || 'GET';
        var data = options.data || null;
        var onSuccess = options.onSuccess || null;
        var onError = options.onError || null;

        // Ensure any previous modal is fully closed
        if (modalElement && modalElement.classList.contains('show')) {
            closeModal();
            // Wait for modal to fully close before opening new one
            setTimeout(function () {
                proceedWithLoad();
            }, 400);
        } else {
            proceedWithLoad();
        }

        function proceedWithLoad() {
            // Clean up any stray backdrops
            removeAllBackdrops();

            // Set title
            if (title) {
                titleTextElement.textContent = title;
            }

            // Reset modal content
            resetModal();

            // Open modal
            modal.show();

            // Load content via AJAX
            $.ajax({
                url: url,
                type: method,
                data: data,
                success: function (response) {
                    // Hide loader in title
                    if (titleLoaderElement) {
                        titleLoaderElement.style.display = 'none';
                    }

                    // Inject content
                    if (bodyElement) {
                        bodyElement.innerHTML = response;
                    }

                    // Initialize any forms in the modal
                    initializeModalForms();

                    // Call success callback
                    if (onSuccess && typeof onSuccess === 'function') {
                        onSuccess(response);
                    }

                    // Reinitialize any plugins (datepickers, selects, etc.)
                    reinitializePlugins();
                },
                error: function (xhr, status, error) {
                    // Hide loader
                    if (titleLoaderElement) {
                        titleLoaderElement.style.display = 'none';
                    }

                    // Show error message
                    var errorHtml = `
                        <div class="alert alert-danger">
                            <i class="fas fa-exclamation-triangle"></i>
                            <strong>Error loading content</strong>
                            <p class="mb-0 mt-2">${error || 'Unable to load the requested content. Please try again.'}</p>
                        </div>
                    `;
                    if (bodyElement) {
                        bodyElement.innerHTML = errorHtml;
                    }

                    // Call error callback
                    if (onError && typeof onError === 'function') {
                        onError(xhr, status, error);
                    }
                }
            });
        }
    }

    /**
     * Initialize forms inside modal for AJAX submission
     */
    function initializeModalForms() {
        // Find all forms in modal
        var forms = bodyElement.querySelectorAll('form');

        forms.forEach(function (form) {
            // Remove any existing submit handlers
            $(form).off('submit');
            
            // Parse validation for the form (only if validator is available)
            if (typeof $.validator !== 'undefined' && $.validator.unobtrusive) {
                $.validator.unobtrusive.parse(form);
            }
            
            // Add AJAX submit handler
            $(form).on('submit', function (e) {
                e.preventDefault();
                
                var $form = $(this);
                
                // Validate form first (only if validator is available)
                if (typeof $form.valid === 'function' && !$form.valid()) {
                    return false;
                }
                
                var formData = new FormData(this);
                var submitUrl = $form.attr('action');
                var submitBtn = $form.find('button[type=submit]');
                var originalBtnText = submitBtn.html();
                
                // Disable submit button
                submitBtn.prop('disabled', true);
                submitBtn.html('<i class="fas fa-spinner fa-spin"></i> Saving...');

                $.ajax({
                    url: submitUrl,
                    type: 'POST',
                    data: formData,
                    processData: false,
                    contentType: false,
                    success: function (response) {
                        // Re-enable button
                        submitBtn.prop('disabled', false);
                        submitBtn.html(originalBtnText);
                        
                        // Handle JSON response
                        if (typeof response === 'object' && response.success !== undefined) {
                            if (response.success) {
                                // Close modal
                                closeModal();
                                
                                // Show success message
                                if (response.message && typeof UIHelpers !== 'undefined') {
                                    UIHelpers.showAlert(response.message, 'success');
                                }
                                
                                // Trigger custom event with response data
                                $(document).trigger('modalSuccess', [response]);
                            } else {
                                // Show error message
                                if (response.message && typeof UIHelpers !== 'undefined') {
                                    UIHelpers.showAlert(response.message, 'danger');
                                }
                            }
                        } 
                        // Handle partial view response (validation errors)
                        else if (typeof response === 'string') {
                            // Replace modal content with updated form (showing validation errors)
                            if (bodyElement) {
                                bodyElement.innerHTML = response;
                            }
                            initializeModalForms();
                        }
                    },
                    error: function (xhr) {
                        // Re-enable button
                        submitBtn.prop('disabled', false);
                        submitBtn.html(originalBtnText);
                        
                        var errorMsg = 'An error occurred. Please try again.';
                        
                        if (xhr.responseJSON && xhr.responseJSON.message) {
                            errorMsg = xhr.responseJSON.message;
                        } else if (xhr.responseText) {
                            // Check if it's HTML (validation errors)
                            if (xhr.responseText.indexOf('<') !== -1) {
                                if (bodyElement) {
                                    bodyElement.innerHTML = xhr.responseText;
                                }
                                initializeModalForms();
                                return;
                            }
                        }
                        
                        if (typeof UIHelpers !== 'undefined') {
                            UIHelpers.showAlert(errorMsg, 'danger');
                        }
                    }
                });
            });
        });
    }

    /**
     * Reinitialize plugins after content load
     */
    function reinitializePlugins() {
        // Reinitialize datepickers
        if (typeof $.fn.datepicker !== 'undefined') {
            $('.datepicker').datepicker();
        }

        // Reinitialize select2
        if (typeof $.fn.select2 !== 'undefined') {
            $('.select2').select2({
                dropdownParent: $('#globalModal')
            });
        }

        // Reinitialize tooltips
        if (bodyElement) {
            var tooltipTriggerList = [].slice.call(bodyElement.querySelectorAll('[data-bs-toggle="tooltip"]'));
            tooltipTriggerList.map(function(tooltipTriggerEl) {
                return new bootstrap.Tooltip(tooltipTriggerEl);
            });
        }
    }

    /**
     * Close the modal
     */
    function closeModal() {
        if (modal) {
            modal.hide();
        }
    }

    /**
     * Show modal footer
     * @param {string} html - Footer HTML content
     */
    function showFooter(html) {
        if (footerElement) {
            footerElement.innerHTML = html;
            footerElement.style.display = 'block';
        }
    }

    /**
     * Hide modal footer
     */
    function hideFooter() {
        if (footerElement) {
            footerElement.style.display = 'none';
            footerElement.innerHTML = '';
        }
    }

    // Initialize on DOM ready
    $(document).ready(function () {
        init();
    });

    // Public API
    return {
        load: loadModal,
        close: closeModal,
        showFooter: showFooter,
        hideFooter: hideFooter,
        reset: resetModal,
        cleanup: cleanupModal
    };
})();

// Expose to global scope
window.ModalLoader = ModalLoader;