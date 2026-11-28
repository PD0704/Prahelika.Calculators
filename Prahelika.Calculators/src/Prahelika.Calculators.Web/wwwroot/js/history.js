// Calculation history: remembers the last results in this browser (localStorage) and lists them in a side panel.
// Everything read back from storage is treated as untrusted: it is shape-checked and only ever written with textContent.
(function () {
    "use strict";

    var STORAGE_KEY = "calculation-history";
    var MAX_ENTRIES = 20;
    var MAX_TEXT = 200;
    var SEPARATOR = " \u00b7 ";   // a middle dot

    var storageBlocked = false;

    function isValidEntry(entry) {
        return entry !== null && typeof entry === "object"
            && typeof entry.time === "number"
            && typeof entry.url === "string"
            && typeof entry.title === "string"
            && typeof entry.inputs === "string"
            && typeof entry.result === "string";
    }

    function load() {
        try {
            var parsed = JSON.parse(localStorage.getItem(STORAGE_KEY) || "[]");
            return Array.isArray(parsed) ? parsed.filter(isValidEntry) : [];
        } catch (error) {
            storageBlocked = true;     // storage is blocked, or the saved text is damaged
            return [];
        }
    }

    function save(entries) {
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(entries.slice(0, MAX_ENTRIES)));
        } catch (error) {
            storageBlocked = true;
        }
    }

    // One line of text: no line breaks or repeated spaces, and not too long.
    function clip(text) {
        var clean = (text || "").replace(/\s+/g, " ").trim();
        return clean.length > MAX_TEXT ? clean.slice(0, MAX_TEXT - 3) + "..." : clean;
    }

    // "Finance - Loan EMI": the page heading, and the selected tab if the page has tabs.
    function pageTitle() {
        var heading = document.querySelector("main h1");
        var tab = document.querySelector("main .nav-pills .nav-link.active");
        var title = heading ? heading.textContent : document.title;

        return clip(tab ? title + SEPARATOR + tab.textContent : title);
    }

    // "Amount: 100000 - Rate: 12": every field of the form, as the page shows it after the calculation.
    function describeInputs() {
        var form = document.querySelector("main form");
        if (!form) {
            return "";
        }

        var parts = [];

        form.querySelectorAll("input, select, textarea").forEach(function (field) {
            if (field.type === "hidden" || field.type === "submit" || field.type === "button") {
                return;
            }

            var value;
            if (field.tagName === "SELECT") {
                value = field.selectedOptions.length > 0 ? field.selectedOptions[0].textContent : "";
            } else if (field.type === "checkbox") {
                value = field.checked ? "Yes" : "No";
            } else {
                value = field.value;
            }

            value = clip(value);
            if (!value) {
                return;
            }

            var label = field.id ? form.querySelector('label[for="' + CSS.escape(field.id) + '"]') : null;
            parts.push((label ? clip(label.textContent) : field.name) + ": " + value);
        });

        return parts.join(SEPARATOR);
    }

    // Saves the result on this page, if there is one.
    function recordResult() {
        var card = document.getElementById("result-card");
        if (!card || !card.dataset.historyResult) {
            return;
        }

        var entry = {
            time: Date.now(),
            url: location.pathname + location.search,
            title: pageTitle(),
            inputs: describeInputs(),
            result: clip(card.dataset.historyResult)
        };

        var entries = load();
        var latest = entries[0];

        // Reloading a result page sends the same form again. Do not save the same result twice in a row.
        if (latest && latest.url === entry.url && latest.inputs === entry.inputs && latest.result === entry.result) {
            return;
        }

        entries.unshift(entry);
        save(entries);
    }

    // Only links on this site: a single "/" at the start. Anything else (for example "javascript:") becomes the home page.
    function safeUrl(url) {
        return url.charAt(0) === "/" && url.charAt(1) !== "/" && url.charAt(1) !== "\\" ? url : "/";
    }

    function textElement(tag, className, text) {
        var element = document.createElement(tag);
        if (className) {
            element.className = className;
        }
        element.textContent = text;   // text only, never HTML
        return element;
    }

    function render() {
        var list = document.getElementById("history-list");
        var empty = document.getElementById("history-empty");
        var clear = document.getElementById("history-clear");
        var count = document.getElementById("history-count");
        var unavailable = document.getElementById("history-unavailable");
        if (!list) {
            return;
        }

        var entries = load();
        list.replaceChildren();

        entries.forEach(function (entry) {
            var item = document.createElement("li");
            item.className = "list-group-item px-0";

            var link = textElement("a", "fw-semibold d-block text-break", entry.title);
            link.href = safeUrl(entry.url);
            item.appendChild(link);

            if (entry.inputs) {
                item.appendChild(textElement("div", "small text-body-secondary text-break", entry.inputs));
            }

            item.appendChild(textElement("div", "text-break", "\u2192 " + entry.result));   // an arrow, then the result
            item.appendChild(textElement("div", "small text-body-secondary",
                new Date(entry.time).toLocaleString("en-IN", { dateStyle: "medium", timeStyle: "short" })));

            list.appendChild(item);
        });

        empty.hidden = entries.length > 0;
        clear.hidden = entries.length === 0;
        count.hidden = entries.length === 0;
        count.textContent = String(entries.length);
        unavailable.hidden = !storageBlocked;
    }

    var clearButton = document.getElementById("history-clear");
    if (clearButton) {
        clearButton.addEventListener("click", function () {
            try {
                localStorage.removeItem(STORAGE_KEY);
            } catch (error) {
                storageBlocked = true;
            }
            render();
        });
    }

    // Another tab changed the history.
    window.addEventListener("storage", function (event) {
        if (event.key === STORAGE_KEY || event.key === null) {
            render();
        }
    });

    recordResult();
    render();
})();