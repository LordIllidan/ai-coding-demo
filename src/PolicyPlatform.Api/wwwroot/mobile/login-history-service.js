// Serwis danych ekranu historii logowań (AISDLC-188).
// UMD-style export so this pure module is testable from Node without a build step.
(function (root, factory) {
  if (typeof module === 'object' && module.exports) {
    module.exports = factory();
  } else {
    root.LoginHistoryService = factory();
  }
})(typeof self !== 'undefined' ? self : this, function () {
  const LOGIN_HISTORY_ENDPOINT = '/api/mobile/me/login-history';

  class LoginHistoryError extends Error {
    constructor(status, message) {
      super(message);
      this.name = 'LoginHistoryError';
      this.status = status;
    }
  }

  // Frontend nigdy nie wysyła userId/customerId/policyId ani innych identyfikatorów
  // użytkownika — backend ustala tożsamość wyłącznie na podstawie tokenu JWT.
  async function fetchLoginHistory(token, fetchImpl = fetch) {
    let res;
    try {
      res = await fetchImpl(LOGIN_HISTORY_ENDPOINT, {
        method: 'GET',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
    } catch {
      throw new LoginHistoryError(0, 'Brak połączenia z serwerem.');
    }

    if (res.status === 401) {
      throw new LoginHistoryError(401, 'Sesja wygasła. Zaloguj się ponownie.');
    }
    if (!res.ok) {
      throw new LoginHistoryError(res.status, 'Nie udało się pobrać historii logowań. Spróbuj ponownie później.');
    }

    const body = await res.json();
    return Array.isArray(body?.items) ? body.items : [];
  }

  return { LOGIN_HISTORY_ENDPOINT, LoginHistoryError, fetchLoginHistory };
});
