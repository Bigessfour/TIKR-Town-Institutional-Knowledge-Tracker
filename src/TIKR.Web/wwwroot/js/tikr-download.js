window.tikrDownload = {
  bytes: function (fileName, base64, contentType) {
    const link = document.createElement('a');
    link.href = `data:${contentType || 'application/octet-stream'};base64,${base64}`;
    link.download = fileName;
    link.click();
  }
};
