// Klient API dla GET /api/claims/{claimId}/last-paid-tranche (AISDLC-135 / AISDLC-120).
// UMD-style export so this pure module is testable from Node without a build step.
(function (root, factory) {
  if (typeof module === 'object' && module.exports) {
    module.exports = factory();
  } else {
    root.LastPaidTrancheClient = factory();
  }
})(typeof self !== 'undefined' ? self : this, function () {
  const GENERIC_ERROR_MESSAGE = 'Nie udało się pobrać danych ostatniej wypłaconej transzy. Spróbuj ponownie.';

  function safeParseJson(text) {
    if (!text) return null;
    try {
      return JSON.parse(text);
    } catch {
      return null;
    }
  }

  // Zwraca { ok: true, data } albo { ok: false, error: { code, message, retryable, correlationId } }.
  // Nigdy nie rzuca wyjątku — wywołujący ma zawsze dostać jednoznaczny wynik do wyrenderowania.
  async function fetchLastPaidTranche(claimId, options) {
    const { fetchImpl = fetch, token } = options || {};
    const headers = {};
    if (token) headers.Authorization = `Bearer ${token}`;

    let res;
    try {
      res = await fetchImpl(`/api/claims/${encodeURIComponent(claimId)}/last-paid-tranche`, { headers });
    } catch {
      return {
        ok: false,
        error: { code: 'NETWORK_ERROR', message: GENERIC_ERROR_MESSAGE, retryable: true, correlationId: null },
      };
    }

    const body = safeParseJson(await res.text());

    if (!res.ok) {
      return {
        ok: false,
        error: {
          code: body?.code || `HTTP_${res.status}`,
          message: body?.message || GENERIC_ERROR_MESSAGE,
          retryable: body?.retryable ?? true,
          correlationId: body?.correlationId || null,
        },
      };
    }

    return { ok: true, data: body };
  }

  return { fetchLastPaidTranche, GENERIC_ERROR_MESSAGE };
});
