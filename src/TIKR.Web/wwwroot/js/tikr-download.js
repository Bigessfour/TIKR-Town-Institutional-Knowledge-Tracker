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
