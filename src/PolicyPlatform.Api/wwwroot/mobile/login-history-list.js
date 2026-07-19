// Komponent listy historii logowań (AISDLC-188).
// UMD-style export so this pure module is testable from Node without a build step.
(function (root, factory) {
  if (typeof module === 'object' && module.exports) {
    module.exports = factory();
  } else {
    root.LoginHistoryList = factory();
  }
})(typeof self !== 'undefined' ? self : this, function () {
  function renderLoginHistoryList(items) {
    const list = document.createElement('ul');
    list.className = 'login-history-list';
    for (const item of items) {
      const row = document.createElement('li');
      row.className = 'login-history-row';
      row.dataset.loginId = item.loginId;
      row.innerHTML = `
        <div class="login-history-row-primary">
          <span class="login-history-device">${item.deviceLabel}</span>
          <span class="login-history-time">${item.occurredAtLabel}</span>
        </div>
        <div class="login-history-row-secondary">
          ${item.osLabel ? `<span>${item.osLabel}</span>` : ''}
          ${item.ipAddress ? `<span>${item.ipAddress}</span>` : ''}
        </div>`;
      list.appendChild(row);
    }
    return list;
  }

  return { renderLoginHistoryList };
});
