const { test } = require('node:test');
const assert = require('node:assert/strict');
const {
  isCompleteInstallment,
  mapPayoutResponse,
  mapErrorResponse,
  formatAmount,
  EMPTY_STATE_MESSAGE,
  ERROR_MESSAGES,
} = require('./last-paid-installment.js');

const FULL_INSTALLMENT = {
  installmentId: '9c6b1a1e-1111-4a1a-9a1a-000000000001',
  installmentNo: 1,
  paidAt: '2026-06-01',
  amount: 1234.56,
  currency: 'PLN',
};

test('mapPayoutResponse: renders PAID when screenState is PAID and all fields present', () => {
  const result = mapPayoutResponse({
    claimId: 'c1',
    claimNumber: 'SZK/2026/001',
    screenState: 'PAID',
    lastPaidInstallment: FULL_INSTALLMENT,
    canEdit: false,
  });
  assert.equal(result.screenState, 'PAID');
  assert.equal(result.claimNumber, 'SZK/2026/001');
  assert.deepEqual(result.installment, FULL_INSTALLMENT);
  assert.equal(result.canEdit, false);
});

test('mapPayoutResponse: NO_PAYOUT yields empty state with fixed message, no data', () => {
  const result = mapPayoutResponse({ claimId: 'c1', screenState: 'NO_PAYOUT', lastPaidInstallment: null, canEdit: false });
  assert.equal(result.screenState, 'EMPTY');
  assert.equal(result.installment, null);
  assert.equal(result.claimNumber, null);
  assert.equal(result.message, EMPTY_STATE_MESSAGE);
});

test('mapPayoutResponse: INCOMPLETE_DATA yields the same empty state as NO_PAYOUT', () => {
  const result = mapPayoutResponse({ claimId: 'c1', screenState: 'INCOMPLETE_DATA', lastPaidInstallment: null, canEdit: false });
  assert.equal(result.screenState, 'EMPTY');
  assert.equal(result.message, EMPTY_STATE_MESSAGE);
});

test('mapPayoutResponse: PAID with a missing field falls back to empty state (no placeholders)', () => {
  for (const field of Object.keys(FULL_INSTALLMENT)) {
    const partial = { ...FULL_INSTALLMENT, [field]: '' };
    const result = mapPayoutResponse({ claimId: 'c1', screenState: 'PAID', lastPaidInstallment: partial, canEdit: false });
    assert.equal(result.screenState, 'EMPTY', `expected empty state when ${field} is blank`);
    assert.equal(result.installment, null);
  }
});

test('mapPayoutResponse: PAID with null lastPaidInstallment falls back to empty state', () => {
  const result = mapPayoutResponse({ claimId: 'c1', screenState: 'PAID', lastPaidInstallment: null, canEdit: false });
  assert.equal(result.screenState, 'EMPTY');
});

test('mapPayoutResponse: unrecognized screenState falls back to empty state', () => {
  const result = mapPayoutResponse({ claimId: 'c1', screenState: 'SOMETHING_ELSE', lastPaidInstallment: FULL_INSTALLMENT, canEdit: false });
  assert.equal(result.screenState, 'EMPTY');
  assert.equal(result.message, EMPTY_STATE_MESSAGE);
});

test('mapPayoutResponse: null body falls back to empty state', () => {
  const result = mapPayoutResponse(null);
  assert.equal(result.screenState, 'EMPTY');
  assert.equal(result.message, EMPTY_STATE_MESSAGE);
});

test('isCompleteInstallment: rejects missing/null/undefined/empty-string fields', () => {
  assert.equal(isCompleteInstallment(null), false);
  assert.equal(isCompleteInstallment({}), false);
  assert.equal(isCompleteInstallment({ ...FULL_INSTALLMENT, amount: undefined }), false);
  assert.equal(isCompleteInstallment(FULL_INSTALLMENT), true);
});

test('mapErrorResponse: 401 signals a login redirect', () => {
  assert.deepEqual(mapErrorResponse(401, null), { kind: 'redirect-login' });
});

test('mapErrorResponse: 403 maps to CLAIM_ACCESS_DENIED message', () => {
  const result = mapErrorResponse(403, { code: 'CLAIM_ACCESS_DENIED' });
  assert.equal(result.kind, 'message');
  assert.equal(result.text, 'Brak uprawnień do danych szkody.');
});

test('mapErrorResponse: 404 maps to a not-found message', () => {
  const result = mapErrorResponse(404, { code: 'CLAIM_NOT_FOUND' });
  assert.equal(result.kind, 'message');
  assert.equal(result.text, 'Nie znaleziono danych szkody.');
});

test('mapErrorResponse: 500 maps to a technical message without exposing details', () => {
  const result = mapErrorResponse(500, { code: 'CLAIM_PAYOUT_LOOKUP_FAILED' });
  assert.equal(result.kind, 'message');
  assert.equal(result.text, 'Wystąpił błąd techniczny. Spróbuj ponownie później.');
});

test('mapErrorResponse: unrecognized status falls back to UNKNOWN with the response code echoed', () => {
  const result = mapErrorResponse(400, { code: 'SOME_OTHER_CODE' });
  assert.equal(result.kind, 'message');
  assert.equal(result.code, 'SOME_OTHER_CODE');
  assert.equal(result.text, ERROR_MESSAGES.UNKNOWN);
});

test('mapErrorResponse: unrecognized status with no body code falls back to UNKNOWN code', () => {
  const result = mapErrorResponse(400, null);
  assert.equal(result.kind, 'message');
  assert.equal(result.code, 'UNKNOWN');
  assert.equal(result.text, ERROR_MESSAGES.UNKNOWN);
});

test('formatAmount: formats amount to 2 decimals with currency suffix', () => {
  assert.equal(formatAmount({ amount: 1234.56, currency: 'PLN' }), '1234.56 PLN');
  assert.equal(formatAmount({ amount: 5, currency: 'EUR' }), '5.00 EUR');
  assert.equal(formatAmount({ amount: 1.005, currency: 'PLN' }), '1.00 PLN');
});
