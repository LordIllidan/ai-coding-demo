const { test } = require('node:test');
const assert = require('node:assert/strict');
const { STATUS, mapLoginHistoryEntries, buildViewState } = require('./login-history-viewmodel.js');

test('buildViewState: loading takes precedence and yields no items', () => {
  const state = buildViewState({ loading: true, entries: [{ loginId: '1', occurredAt: '2026-07-19T10:00:00Z', deviceType: 'PHONE' }] });
  assert.equal(state.status, STATUS.LOADING);
  assert.deepEqual(state.items, []);
});

test('buildViewState: error yields the error message and no items', () => {
  const state = buildViewState({ loading: false, error: new Error('Sesja wygasła. Zaloguj się ponownie.') });
  assert.equal(state.status, STATUS.ERROR);
  assert.equal(state.errorMessage, 'Sesja wygasła. Zaloguj się ponownie.');
});

test('buildViewState: empty entries yields empty status', () => {
  const state = buildViewState({ loading: false, entries: [] });
  assert.equal(state.status, STATUS.EMPTY);
});

test('buildViewState: entries yield list status with mapped items', () => {
  const state = buildViewState({
    loading: false,
    entries: [{ loginId: '1', occurredAt: '2026-07-19T10:00:00Z', deviceType: 'PHONE', deviceLabel: null, osName: null, osVersion: null, ipAddress: null }],
  });
  assert.equal(state.status, STATUS.LIST);
  assert.equal(state.items.length, 1);
});

test('mapLoginHistoryEntries: sorts descending by occurredAt regardless of input order', () => {
  const entries = [
    { loginId: 'old', occurredAt: '2026-01-01T00:00:00Z', deviceType: 'WEB' },
    { loginId: 'new', occurredAt: '2026-07-19T00:00:00Z', deviceType: 'PHONE' },
  ];
  const mapped = mapLoginHistoryEntries(entries);
  assert.deepEqual(mapped.map((m) => m.loginId), ['new', 'old']);
});

test('mapLoginHistoryEntries: falls back to device type label when deviceLabel is missing', () => {
  const mapped = mapLoginHistoryEntries([{ loginId: '1', occurredAt: '2026-07-19T00:00:00Z', deviceType: 'TABLET', deviceLabel: null }]);
  assert.equal(mapped[0].deviceLabel, 'Tablet');
});

test('mapLoginHistoryEntries: unknown device type falls back gracefully', () => {
  const mapped = mapLoginHistoryEntries([{ loginId: '1', occurredAt: '2026-07-19T00:00:00Z', deviceType: 'SMART_FRIDGE', deviceLabel: null }]);
  assert.equal(mapped[0].deviceLabel, 'Nieznane urządzenie');
});
