window.spriteScout = {
  load(key) {
    try { return JSON.parse(localStorage.getItem(key) || "[]"); }
    catch { return []; }
  },
  save(key, values) { localStorage.setItem(key, JSON.stringify(values)); },
  async print() {
    const images = [...document.querySelectorAll(".collectionMatrix img")];
    images.forEach(image => image.loading = "eager");
    await Promise.all(images.map(image => {
      if (image.complete) return image.decode?.().catch(() => undefined);
      return new Promise(resolve => {
        const ready = () => resolve();
        image.addEventListener("load", ready, { once: true });
        image.addEventListener("error", ready, { once: true });
        setTimeout(ready, 6000);
      });
    }));
    window.print();
  }
};
