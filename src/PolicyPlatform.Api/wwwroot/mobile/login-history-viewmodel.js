// Mapowanie stanu ekranu historii logowań (AISDLC-188).
// UMD-style export so this pure module is testable from Node without a build step.
(function (root, factory) {
  if (typeof module === 'object' && module.exports) {
    module.exports = factory();
  } else {
    root.LoginHistoryViewModel = factory();
  }
})(typeof self !== 'undefined' ? self : this, function () {
  const STATUS = {
    LOADING: 'loading',
    LIST: 'list',
    EMPTY: 'empty',
    ERROR: 'error',
  };

  const DEVICE_TYPE_LABELS = {
    PHONE: 'Telefon',
    TABLET: 'Tablet',
    WEB: 'Przeglądarka',
    UNKNOWN: 'Nieznane urządzenie',
  };

  function deviceTypeLabel(deviceType) {
    return DEVICE_TYPE_LABELS[deviceType] ?? DEVICE_TYPE_LABELS.UNKNOWN;
  }

  function formatOccurredAt(occurredAtIso) {
    const date = new Date(occurredAtIso);
    if (Number.isNaN(date.getTime())) return occurredAtIso;
    return date.toLocaleString('pl-PL', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  // Backend zwraca już malejąco po occurredAt, ale ekran nie polega na tym założeniu —
  // "od najnowszego do najstarszego" sortujemy również na froncie.
  function sortByOccurredAtDescending(entries) {
    return [...entries].sort((a, b) => new Date(b.occurredAt).getTime() - new Date(a.occurredAt).getTime());
  }

  function toListItem(entry) {
    const osLabel = entry.osName ? `${entry.osName}${entry.osVersion ? ' ' + entry.osVersion : ''}` : null;
    return {
      loginId: entry.loginId,
      occurredAtLabel: formatOccurredAt(entry.occurredAt),
      deviceLabel: entry.deviceLabel || deviceTypeLabel(entry.deviceType),
      osLabel,
      ipAddress: entry.ipAddress || null,
    };
  }

  function mapLoginHistoryEntries(entries) {
    return sortByOccurredAtDescending(entries).map(toListItem);
  }

  // Buduje jednoznaczny stan widoku: dokładnie jeden z loading/list/empty/error jest aktywny.
  function buildViewState({ loading, error, entries }) {
    if (loading) return { status: STATUS.LOADING, items: [], errorMessage: null };
    if (error) return { status: STATUS.ERROR, items: [], errorMessage: error.message };
    const items = mapLoginHistoryEntries(entries ?? []);
    if (items.length === 0) return { status: STATUS.EMPTY, items: [], errorMessage: null };
    return { status: STATUS.LIST, items, errorMessage: null };
  }

  return {
    STATUS,
    deviceTypeLabel,
    formatOccurredAt,
    mapLoginHistoryEntries,
    buildViewState,
  };
});
