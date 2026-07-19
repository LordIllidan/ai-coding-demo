const { test } = require('node:test');
const assert = require('node:assert/strict');
const {
  ERROR_MESSAGES,
  formatBadgeValue,
  mapCounterResponse,
  mapListResponse,
  mapReadResponse,
  applyReadResult,
  mapErrorResponse,
} = require('./notification-counter.js');

test('formatBadgeValue: renders 0 as visible "0", not empty/hidden', () => {
  assert.equal(formatBadgeValue(0), '0');
});

test('formatBadgeValue: renders positive counts as strings', () => {
  assert.equal(formatBadgeValue(1), '1');
  assert.equal(formatBadgeValue(42), '42');
});

test('formatBadgeValue: rejects negative numbers', () => {
  assert.throws(() => formatBadgeValue(-1), TypeError);
});

test('formatBadgeValue: rejects non-finite numbers', () => {
  assert.throws(() => formatBadgeValue(Infinity), TypeError);
  assert.throws(() => formatBadgeValue(NaN), TypeError);
});

test('formatBadgeValue: rejects non-number types', () => {
  assert.throws(() => formatBadgeValue(null), TypeError);
  assert.throws(() => formatBadgeValue(undefined), TypeError);
  assert.throws(() => formatBadgeValue('3'), TypeError);
});

test('mapCounterResponse: extracts unreadCount and calculatedAt', () => {
  const result = mapCounterResponse({ unreadCount: 5, calculatedAt: '2026-07-19T00:00:00Z' });
  assert.deepEqual(result, { unreadCount: 5, calculatedAt: '2026-07-19T00:00:00Z' });
});

test('mapCounterResponse: preserves explicit 0 (never coerced away)', () => {
  const result = mapCounterResponse({ unreadCount: 0, calculatedAt: '2026-07-19T00:00:00Z' });
  assert.equal(result.unreadCount, 0);
});

test('mapListResponse: maps items and preserves fields', () => {
  const body = {
    items: [
      { id: 'a1', title: 'T', body: 'B', type: 'info', createdAt: '2026-07-19T00:00:00Z', isRead: false, readAt: null },
    ],
    nextCursor: 'cursor-1',
  };
  const result = mapListResponse(body);
  assert.deepEqual(result, {
    items: [
      { id: 'a1', title: 'T', body: 'B', type: 'info', createdAt: '2026-07-19T00:00:00Z', isRead: false, readAt: null },
    ],
    nextCursor: 'cursor-1',
  });
});

test('mapListResponse: defaults items to empty array when missing', () => {
  const result = mapListResponse({ nextCursor: null });
  assert.deepEqual(result.items, []);
  assert.equal(result.nextCursor, null);
});

test('mapReadResponse: extracts all fields from PATCH /read response', () => {
  const result = mapReadResponse({
    notificationId: 'n1',
    isRead: true,
    readAt: '2026-07-19T00:00:00Z',
    unreadCount: 3,
  });
  assert.deepEqual(result, {
    notificationId: 'n1',
    isRead: true,
    readAt: '2026-07-19T00:00:00Z',
    unreadCount: 3,
  });
});

test('applyReadResult: badge count comes from server unreadCount, not local decrement', () => {
  const readResult = { notificationId: 'n1', isRead: true, readAt: '2026-07-19T00:00:00Z', unreadCount: 4 };
  assert.deepEqual(applyReadResult(readResult), { unreadCount: 4, calculatedAt: null });
});

test('applyReadResult: idempotent re-read (already-read notification) does not double-decrement', () => {
  const readResult = { notificationId: 'n1', isRead: true, readAt: '2026-07-19T00:00:00Z', unreadCount: 4 };
  assert.equal(applyReadResult(readResult).unreadCount, 4);
});

test('mapErrorResponse: 401 maps to redirect-login', () => {
  const err = mapErrorResponse(401, null);
  assert.equal(err.kind, 'redirect-login');
  assert.equal(err.code, 'UNAUTHENTICATED');
  assert.equal(err.text, ERROR_MESSAGES.UNAUTHENTICATED);
});

test('mapErrorResponse: 400 maps to VALIDATION_ERROR', () => {
  const err = mapErrorResponse(400, { code: 'VALIDATION_ERROR' });
  assert.equal(err.kind, 'message');
  assert.equal(err.code, 'VALIDATION_ERROR');
  assert.equal(err.text, ERROR_MESSAGES.VALIDATION_ERROR);
});

test('mapErrorResponse: 403 maps to FORBIDDEN', () => {
  const err = mapErrorResponse(403, { code: 'FORBIDDEN' });
  assert.equal(err.code, 'FORBIDDEN');
  assert.equal(err.text, ERROR_MESSAGES.FORBIDDEN);
});

test('mapErrorResponse: 404 maps to NOTIFICATION_NOT_FOUND', () => {
  const err = mapErrorResponse(404, { code: 'NOTIFICATION_NOT_FOUND' });
  assert.equal(err.code, 'NOTIFICATION_NOT_FOUND');
  assert.equal(err.text, ERROR_MESSAGES.NOTIFICATION_NOT_FOUND);
});

test('mapErrorResponse: unrecognized status falls back to UNKNOWN with body code preserved', () => {
  const err = mapErrorResponse(500, { code: 'SOME_SERVER_CODE' });
  assert.equal(err.kind, 'message');
  assert.equal(err.code, 'SOME_SERVER_CODE');
  assert.equal(err.text, ERROR_MESSAGES.UNKNOWN);
});

test('mapErrorResponse: unrecognized status with no body code falls back to UNKNOWN code', () => {
  const err = mapErrorResponse(500, null);
  assert.equal(err.code, 'UNKNOWN');
  assert.equal(err.text, ERROR_MESSAGES.UNKNOWN);
});
