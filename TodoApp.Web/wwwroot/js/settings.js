// Settings page live preview functionality
(function () {
    'use strict';

    const html = document.documentElement;

    // Map preference keys to data attribute names
    const preferenceToAttribute = {
        'fontsize': 'data-font-size',
        'linespacing': 'data-line-spacing',
        'highcontrastmode': 'data-high-contrast',
        'reducedmotion': 'data-reduced-motion'
    };

    // Apply a preference to the HTML element for live preview
    function applyPreference(key, value) {
        const attrName = preferenceToAttribute[key];
        if (!attrName) return;

        if (key === 'highcontrastmode' || key === 'reducedmotion') {
            // Boolean preferences
            if (value === 'true' || value === true) {
                html.setAttribute(attrName, 'true');
            } else {
                html.removeAttribute(attrName);
            }
        } else {
            // String preferences (fontsize, linespacing)
            html.setAttribute(attrName, value);
        }
    }

    // Save preference to server via AJAX
    function savePreference(key, value) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        if (!token) return;

        fetch('/Settings/UpdatePreferenceAjax', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token.value
            },
            body: JSON.stringify({ key: key, value: String(value) })
        })
        .then(response => response.json())
        .then(data => {
            if (!data.success) {
                console.error('Failed to save preference:', data.error);
            }
        })
        .catch(error => {
            console.error('Error saving preference:', error);
        });
    }

    // Initialize event listeners
    function init() {
        // Handle radio button changes (font size, line spacing)
        document.querySelectorAll('input[type="radio"][data-preference]').forEach(radio => {
            radio.addEventListener('change', function () {
                const key = this.dataset.preference;
                const value = this.value;
                applyPreference(key, value);
                savePreference(key, value);
            });
        });

        // Handle checkbox/switch changes (high contrast, reduced motion)
        document.querySelectorAll('input[type="checkbox"][data-preference]').forEach(checkbox => {
            checkbox.addEventListener('change', function () {
                const key = this.dataset.preference;
                const value = this.checked;
                applyPreference(key, value);
                savePreference(key, value);
            });
        });
    }

    // Run initialization when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
