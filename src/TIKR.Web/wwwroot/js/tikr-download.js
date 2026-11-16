window.tikrDownload = {
  downloadFromBytes: (fileName, contentType, bytes) => {
    const blob = new Blob([new Uint8Array(bytes)], { type: contentType || 'application/octet-stream' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(url);
  },
  createBlobUrl: (bytes, contentType) => {
    const blob = new Blob([new Uint8Array(bytes)], { type: contentType || 'application/pdf' });
    return URL.createObjectURL(blob);
  },
  revokeBlobUrl: (url) => {
    if (url) {
      URL.revokeObjectURL(url);
    }
  }
};

window.tikrUi = {
  clickFileInput: (selector) => {
    const input = document.querySelector(selector);
    if (input) input.click();
  },
  focusId: (id) => {
    const root = document.getElementById(id);
    const target = root?.querySelector('button, a, input, textarea') ?? root;
    if (!target) return;
    target.scrollIntoView({ block: 'center' });
    if (typeof target.focus === 'function') target.focus();
  }
};
