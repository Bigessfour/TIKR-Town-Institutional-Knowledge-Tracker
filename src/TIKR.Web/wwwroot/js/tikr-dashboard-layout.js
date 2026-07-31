window.tikrDashboardLayout = {
  get: function (key) {
    try {
      return localStorage.getItem(key);
    } catch {
      return null;
    }
  },
  set: function (key, value) {
    try {
      localStorage.setItem(key, value);
      return true;
    } catch {
      return false;
    }
  },
  remove: function (key) {
    try {
      localStorage.removeItem(key);
      return true;
    } catch {
      return false;
    }
  }
};
