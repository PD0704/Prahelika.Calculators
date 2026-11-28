// Light and dark theme. This runs in the <head>, so the right colours are set before the page is drawn.
//   "auto"  = follow the device setting (the default)
//   "light" or "dark" = the visitor's own choice, saved in the browser
(function () {
    "use strict";

    var STORAGE_KEY = "theme";                    // holds "light" or "dark". Nothing saved means "auto".
    var MODES = ["auto", "light", "dark"];        // the order the button cycles through
    var LABELS = { auto: "Auto", light: "Light", dark: "Dark" };

    var root = document.documentElement;
    var systemDark = window.matchMedia("(prefers-color-scheme: dark)");

    function savedMode() {
        try {
            var value = localStorage.getItem(STORAGE_KEY);
            return value === "light" || value === "dark" ? value : "auto";
        } catch (error) {
            return "auto";                        // storage can be blocked (private browsing, strict settings)
        }
    }

    function saveMode(mode) {
        try {
            if (mode === "auto") {
                localStorage.removeItem(STORAGE_KEY);
            } else {
                localStorage.setItem(STORAGE_KEY, mode);
            }
        } catch (error) {
            // The theme still changes for this visit.
        }
    }

    function themeFor(mode) {
        return mode === "auto" ? (systemDark.matches ? "dark" : "light") : mode;
    }

    var currentMode = savedMode();

    function updateButton() {
        var button = document.getElementById("theme-toggle");
        if (!button) {
            return;
        }

        var label = "Theme: " + LABELS[currentMode];
        button.textContent = label;
        // The spoken name starts with the visible text, so it matches what people see.
        button.setAttribute("aria-label",
            label + (currentMode === "auto" ? " (follows your device)" : "") + ". Click to change.");
    }

    function apply() {
        var theme = themeFor(currentMode);
        root.setAttribute("data-bs-theme", theme);   // Bootstrap switches every component from this attribute
        updateButton();
        // Lets other scripts (the loan charts) redraw in the new colours.
        document.dispatchEvent(new CustomEvent("themechange", { detail: { theme: theme } }));
    }

    function onSystemChange() {
        if (currentMode === "auto") {
            apply();
        }
    }

    // Set the theme straight away, before the page is drawn.
    apply();

    if (systemDark.addEventListener) {
        systemDark.addEventListener("change", onSystemChange);
    } else if (systemDark.addListener) {
        systemDark.addListener(onSystemChange);   // older Safari
    }

    // Another tab changed the theme.
    window.addEventListener("storage", function (event) {
        if (event.key === STORAGE_KEY || event.key === null) {
            currentMode = savedMode();
            apply();
        }
    });

    // The button is not in the page yet while the head is loading, so wire it up when the page is ready.
    document.addEventListener("DOMContentLoaded", function () {
        updateButton();

        var button = document.getElementById("theme-toggle");
        if (button) {
            button.addEventListener("click", function () {
                currentMode = MODES[(MODES.indexOf(currentMode) + 1) % MODES.length];
                saveMode(currentMode);
                apply();
            });
        }
    });
})();