const { test } = require('node:test');
const assert = require('node:assert/strict');
const {
  STATUS,
  deviceTypeLabel,
  formatOccurredAt,
  mapLoginHistoryEntries,
  buildViewState,
} = require('./login-history-viewmodel.js');

test('deviceTypeLabel: maps known device types', () => {
  assert.equal(deviceTypeLabel('PHONE'), 'Telefon');
  assert.equal(deviceTypeLabel('TABLET'), 'Tablet');
  assert.equal(deviceTypeLabel('WEB'), 'Przeglądarka');
  assert.equal(deviceTypeLabel('UNKNOWN'), 'Nieznane urządzenie');
});

test('deviceTypeLabel: falls back to UNKNOWN label for unrecognized/missing values', () => {
  assert.equal(deviceTypeLabel('SMART_FRIDGE'), 'Nieznane urządzenie');
  assert.equal(deviceTypeLabel(undefined), 'Nieznane urządzenie');
  assert.equal(deviceTypeLabel(null), 'Nieznane urządzenie');
});

test('formatOccurredAt: returns the raw input for unparseable dates', () => {
  assert.equal(formatOccurredAt('not-a-date'), 'not-a-date');
  assert.equal(formatOccurredAt(''), '');
});

test('formatOccurredAt: formats a valid ISO date into a non-empty label', () => {
  const label = formatOccurredAt('2024-03-01T10:15:00Z');
  assert.equal(typeof label, 'string');
  assert.ok(label.length > 0);
  assert.notEqual(label, '2024-03-01T10:15:00Z');
});

test('mapLoginHistoryEntries: sorts descending by occurredAt regardless of input order', () => {
  const entries = [
    { loginId: 'a', occurredAt: '2024-01-01T00:00:00Z', deviceType: 'PHONE' },
    { loginId: 'b', occurredAt: '2024-03-01T00:00:00Z', deviceType: 'PHONE' },
    { loginId: 'c', occurredAt: '2024-02-01T00:00:00Z', deviceType: 'PHONE' },
  ];
  const result = mapLoginHistoryEntries(entries);
  assert.deepEqual(result.map((item) => item.loginId), ['b', 'c', 'a']);
});

test('mapLoginHistoryEntries: does not mutate the input array', () => {
  const entries = [
    { loginId: 'a', occurredAt: '2024-01-01T00:00:00Z', deviceType: 'PHONE' },
    { loginId: 'b', occurredAt: '2024-03-01T00:00:00Z', deviceType: 'PHONE' },
  ];
  const copy = [...entries];
  mapLoginHistoryEntries(entries);
  assert.deepEqual(entries, copy);
});

test('mapLoginHistoryEntries: falls back to device type label when deviceLabel is missing/empty', () => {
  const entries = [
    { loginId: 'a', occurredAt: '2024-01-01T00:00:00Z', deviceType: 'TABLET', deviceLabel: null },
    { loginId: 'b', occurredAt: '2024-01-02T00:00:00Z', deviceType: 'WEB', deviceLabel: '' },
  ];
  const result = mapLoginHistoryEntries(entries);
  assert.equal(result.find((item) => item.loginId === 'a').deviceLabel, 'Tablet');
  assert.equal(result.find((item) => item.loginId === 'b').deviceLabel, 'Przeglądarka');
});

test('mapLoginHistoryEntries: keeps an explicit deviceLabel over the device type label', () => {
  const entries = [
    { loginId: 'a', occurredAt: '2024-01-01T00:00:00Z', deviceType: 'PHONE', deviceLabel: 'iPhone Kasi' },
  ];
  const result = mapLoginHistoryEntries(entries);
  assert.equal(result[0].deviceLabel, 'iPhone Kasi');
});

test('mapLoginHistoryEntries: combines osName and osVersion into osLabel', () => {
  const entries = [
    { loginId: 'a', occurredAt: '2024-01-01T00:00:00Z', deviceType: 'PHONE', osName: 'iOS', osVersion: '17.4' },
  ];
  const result = mapLoginHistoryEntries(entries);
  assert.equal(result[0].osLabel, 'iOS 17.4');
});

test('mapLoginHistoryEntries: omits osVersion from osLabel when missing, and osLabel is null without osName', () => {
  const entries = [
    { loginId: 'a', occurredAt: '2024-01-01T00:00:00Z', deviceType: 'PHONE', osName: 'Android', osVersion: null },
    { loginId: 'b', occurredAt: '2024-01-02T00:00:00Z', deviceType: 'PHONE', osName: null, osVersion: null },
  ];
  const result = mapLoginHistoryEntries(entries);
  assert.equal(result.find((item) => item.loginId === 'a').osLabel, 'Android');
  assert.equal(result.find((item) => item.loginId === 'b').osLabel, null);
});

test('mapLoginHistoryEntries: passes through ipAddress or null', () => {
  const entries = [
    { loginId: 'a', occurredAt: '2024-01-01T00:00:00Z', deviceType: 'PHONE', ipAddress: '10.0.0.1' },
    { loginId: 'b', occurredAt: '2024-01-02T00:00:00Z', deviceType: 'PHONE', ipAddress: null },
  ];
  const result = mapLoginHistoryEntries(entries);
  assert.equal(result.find((item) => item.loginId === 'a').ipAddress, '10.0.0.1');
  assert.equal(result.find((item) => item.loginId === 'b').ipAddress, null);
});

test('buildViewState: loading takes priority over error/entries', () => {
  const state = buildViewState({ loading: true, error: new Error('boom'), entries: [{ loginId: 'a', occurredAt: '2024-01-01T00:00:00Z', deviceType: 'PHONE' }] });
  assert.equal(state.status, STATUS.LOADING);
  assert.deepEqual(state.items, []);
  assert.equal(state.errorMessage, null);
});

test('buildViewState: error takes priority over entries when not loading', () => {
  const state = buildViewState({ loading: false, error: new Error('Sesja wygasła.'), entries: [{ loginId: 'a', occurredAt: '2024-01-01T00:00:00Z', deviceType: 'PHONE' }] });
  assert.equal(state.status, STATUS.ERROR);
  assert.equal(state.errorMessage, 'Sesja wygasła.');
  assert.deepEqual(state.items, []);
});

test('buildViewState: empty entries produce the empty status', () => {
  const state = buildViewState({ loading: false, entries: [] });
  assert.equal(state.status, STATUS.EMPTY);
  assert.deepEqual(state.items, []);
});

test('buildViewState: missing entries default to empty status', () => {
  const state = buildViewState({ loading: false });
  assert.equal(state.status, STATUS.EMPTY);
});

test('buildViewState: non-empty entries produce the list status with mapped items', () => {
  const state = buildViewState({
    loading: false,
    entries: [{ loginId: 'a', occurredAt: '2024-01-01T00:00:00Z', deviceType: 'PHONE' }],
  });
  assert.equal(state.status, STATUS.LIST);
  assert.equal(state.items.length, 1);
  assert.equal(state.items[0].loginId, 'a');
});
