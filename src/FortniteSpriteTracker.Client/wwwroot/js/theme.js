const systemDarkQuery = window.matchMedia("(prefers-color-scheme: dark)");
let callback;

function resolveTheme(preference) {
    return preference === "system"
        ? (systemDarkQuery.matches ? "dark" : "light")
        : preference;
}

function updateDocument(preference) {
    document.documentElement.dataset.themePreference = preference;
    document.documentElement.dataset.theme = resolveTheme(preference);
}

function onSystemThemeChanged(event) {
    if (document.documentElement.dataset.themePreference !== "system") {
        return;
    }

    updateDocument("system");
    callback?.invokeMethodAsync("SystemThemeChangedAsync", event.matches);
}

export function apply(preference, dotNetReference) {
    callback = dotNetReference;
    updateDocument(preference);
    systemDarkQuery.removeEventListener("change", onSystemThemeChanged);
    if (preference === "system") {
        systemDarkQuery.addEventListener("change", onSystemThemeChanged);
    }
}

export function dispose() {
    systemDarkQuery.removeEventListener("change", onSystemThemeChanged);
    callback = undefined;
}
