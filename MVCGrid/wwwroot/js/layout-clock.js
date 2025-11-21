/**
 * Layout Clock - Live Date and Time Display
 * Updates every second in the topbar
 */

(function() {
    'use strict';

    function updateClock() {
        const now = new Date();
        
        // Format options
        const options = {
            weekday: 'short',
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            hour12: true
        };
        
        // Format the date and time
        const formattedDateTime = now.toLocaleString('en-US', options);
        
        // Update the clock element
        const clockElement = document.getElementById('liveClock');
        if (clockElement) {
            clockElement.textContent = formattedDateTime;
        }
    }

    // Initialize clock when DOM is ready
    document.addEventListener('DOMContentLoaded', function() {
        // Update immediately
        updateClock();
        
        // Update every second
        setInterval(updateClock, 1000);
        
        console.log('Live clock initialized');
    });
})();
