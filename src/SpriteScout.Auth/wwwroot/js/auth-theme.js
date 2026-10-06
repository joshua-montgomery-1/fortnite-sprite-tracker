(() => {
    let preference = 'system';
    try {
        preference = JSON.parse(localStorage.getItem('sprite-scout-theme-preference')) || preference;
    } catch {
        // Storage may be unavailable in privacy-restricted browsing contexts.
    }
    if (!['system', 'light', 'dark'].includes(preference)) preference = 'system';

    const media = matchMedia('(prefers-color-scheme: dark)');
    const apply = () => {
        document.documentElement.dataset.theme = preference === 'system'
            ? (media.matches ? 'dark' : 'light')
            : preference;
    };
    apply();
    media.addEventListener('change', apply);
})();
