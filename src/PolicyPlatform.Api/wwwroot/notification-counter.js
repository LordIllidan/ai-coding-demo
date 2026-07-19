// Ekran mobilny "Badge licznika powiadomień" (AISDLC-155, parent story AISDLC-148).
// UMD-style export so this pure module is testable from Node without a build step.
(function (root, factory) {
  if (typeof module === 'object' && module.exports) {
    module.exports = factory();
  } else {
    root.NotificationCounter = factory();
  }
})(typeof self !== 'undefined' ? self : this, function () {
  const ERROR_MESSAGES = {
    UNAUTHENTICATED: 'Sesja wygasła. Zaloguj się ponownie.',
    VALIDATION_ERROR: 'Nieprawidłowe żądanie.',
    FORBIDDEN: 'Brak dostępu do tego powiadomienia.',
    NOTIFICATION_NOT_FOUND: 'Powiadomienie nie zostało znalezione.',
    UNKNOWN: 'Wystąpił nieoczekiwany błąd.',
  };

  // unreadCount is mandatory and never null per contract — 0 must render as the
  // visible digit "0", never as an empty/hidden badge.
  function formatBadgeValue(unreadCount) {
    if (typeof unreadCount !== 'number' || !Number.isFinite(unreadCount) || unreadCount < 0) {
      throw new TypeError('unreadCount must be a non-negative finite number');
    }
    return String(unreadCount);
  }

  function mapCounterResponse(body) {
    return { unreadCount: body.unreadCount, calculatedAt: body.calculatedAt };
  }

  function mapListResponse(body) {
    return {
      items: (body.items || []).map((item) => ({
        id: item.id,
        title: item.title,
        body: item.body,
        type: item.type,
        createdAt: item.createdAt,
        isRead: item.isRead,
        readAt: item.readAt,
      })),
      nextCursor: body.nextCursor,
    };
  }

  function mapReadResponse(body) {
    return {
      notificationId: body.notificationId,
      isRead: body.isRead,
      readAt: body.readAt,
      unreadCount: body.unreadCount,
    };
  }

  // Source of truth for the badge after marking a notification read is the
  // server-returned unreadCount from the PATCH response, not a local decrement —
  // the endpoint is idempotent and re-reading an already-read notification must
  // not double-decrement the badge.
  function applyReadResult(readResult) {
    return { unreadCount: readResult.unreadCount, calculatedAt: null };
  }

  function mapErrorResponse(status, body) {
    if (status === 401) return { kind: 'redirect-login', code: 'UNAUTHENTICATED', text: ERROR_MESSAGES.UNAUTHENTICATED };
    const code = (body && body.code) || null;
    if (status === 400) return { kind: 'message', code: 'VALIDATION_ERROR', text: ERROR_MESSAGES.VALIDATION_ERROR };
    if (status === 403) return { kind: 'message', code: 'FORBIDDEN', text: ERROR_MESSAGES.FORBIDDEN };
    if (status === 404) return { kind: 'message', code: 'NOTIFICATION_NOT_FOUND', text: ERROR_MESSAGES.NOTIFICATION_NOT_FOUND };
    return { kind: 'message', code: code || 'UNKNOWN', text: ERROR_MESSAGES.UNKNOWN };
  }

  return {
    ERROR_MESSAGES,
    formatBadgeValue,
    mapCounterResponse,
    mapListResponse,
    mapReadResponse,
    applyReadResult,
    mapErrorResponse,
  };
});
