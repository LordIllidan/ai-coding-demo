const { test } = require('node:test');
const assert = require('node:assert/strict');
const { fetchLastPaidTranche, GENERIC_ERROR_MESSAGE } = require('./last-paid-tranche-client.js');

function fakeFetch(response) {
  return async () => response;
}

test('fetchLastPaidTranche: returns data on 200 OK', async () => {
  const data = { claimId: 'c1', lastPaidTranche: null, fetchedAt: '2026-07-18T00:00:00Z' };
  const result = await fetchLastPaidTranche('c1', {
    fetchImpl: fakeFetch({ ok: true, status: 200, text: async () => JSON.stringify(data) }),
  });
  assert.deepEqual(result, { ok: true, data });
});

test('fetchLastPaidTranche: maps non-2xx envelope to a normalized error', async () => {
  const envelope = { code: 'CLAIM_NOT_FOUND', message: 'Brak zgłoszenia', retryable: false, correlationId: 'corr-1' };
  const result = await fetchLastPaidTranche('missing', {
    fetchImpl: fakeFetch({ ok: false, status: 404, text: async () => JSON.stringify(envelope) }),
  });
  assert.deepEqual(result, { ok: false, error: envelope });
});

test('fetchLastPaidTranche: falls back to a generic message when the error body is missing/unparsable', async () => {
  const result = await fetchLastPaidTranche('c1', {
    fetchImpl: fakeFetch({ ok: false, status: 503, text: async () => '' }),
  });
  assert.equal(result.ok, false);
  assert.equal(result.error.code, 'HTTP_503');
  assert.equal(result.error.message, GENERIC_ERROR_MESSAGE);
  assert.equal(result.error.retryable, true);
});

test('fetchLastPaidTranche: network failure is treated as a retryable error, not a thrown exception', async () => {
  const result = await fetchLastPaidTranche('c1', {
    fetchImpl: async () => { throw new TypeError('failed to fetch'); },
  });
  assert.equal(result.ok, false);
  assert.equal(result.error.code, 'NETWORK_ERROR');
  assert.equal(result.error.retryable, true);
});

test('fetchLastPaidTranche: sends Authorization header when a token is provided', async () => {
  let capturedHeaders;
  await fetchLastPaidTranche('c1', {
    token: 'abc',
    fetchImpl: async (_url, opts) => {
      capturedHeaders = opts.headers;
      return { ok: true, status: 200, text: async () => JSON.stringify({ claimId: 'c1', lastPaidTranche: null, fetchedAt: 'x' }) };
    },
  });
  assert.equal(capturedHeaders.Authorization, 'Bearer abc');
});

test('fetchLastPaidTranche: omits Authorization header when no token is provided', async () => {
  let capturedHeaders;
  await fetchLastPaidTranche('c1', {
    fetchImpl: async (_url, opts) => {
      capturedHeaders = opts.headers;
      return { ok: true, status: 200, text: async () => JSON.stringify({ claimId: 'c1', lastPaidTranche: null, fetchedAt: 'x' }) };
    },
  });
  assert.equal('Authorization' in capturedHeaders, false);
});

test('fetchLastPaidTranche: returns a populated lastPaidTranche object on 200 OK', async () => {
  const data = {
    claimId: 'c1',
    lastPaidTranche: {
      trancheId: 't1',
      trancheNumber: 2,
      status: 'PAID',
      paidAt: '2026-07-01T00:00:00Z',
      grossAmount: 1234.56,
      currency: 'PLN',
    },
    fetchedAt: '2026-07-18T00:00:00Z',
  };
  const result = await fetchLastPaidTranche('c1', {
    fetchImpl: fakeFetch({ ok: true, status: 200, text: async () => JSON.stringify(data) }),
  });
  assert.deepEqual(result, { ok: true, data });
});

test('fetchLastPaidTranche: falls back to a generic message when the error body is malformed JSON', async () => {
  const result = await fetchLastPaidTranche('c1', {
    fetchImpl: fakeFetch({ ok: false, status: 500, text: async () => '{not-json' }),
  });
  assert.equal(result.ok, false);
  assert.equal(result.error.code, 'HTTP_500');
  assert.equal(result.error.message, GENERIC_ERROR_MESSAGE);
  assert.equal(result.error.retryable, true);
  assert.equal(result.error.correlationId, null);
});

test('fetchLastPaidTranche: fills in defaults for fields missing from a partial error envelope', async () => {
  const result = await fetchLastPaidTranche('c1', {
    fetchImpl: fakeFetch({ ok: false, status: 403, text: async () => JSON.stringify({ code: 'CLAIM_ACCESS_DENIED' }) }),
  });
  assert.deepEqual(result, {
    ok: false,
    error: { code: 'CLAIM_ACCESS_DENIED', message: GENERIC_ERROR_MESSAGE, retryable: true, correlationId: null },
  });
});

test('fetchLastPaidTranche: URL-encodes the claimId in the request path', async () => {
  let capturedUrl;
  await fetchLastPaidTranche('a/b c', {
    fetchImpl: async (url) => {
      capturedUrl = url;
      return { ok: true, status: 200, text: async () => JSON.stringify({ claimId: 'a/b c', lastPaidTranche: null, fetchedAt: 'x' }) };
    },
  });
  assert.equal(capturedUrl, `/api/claims/${encodeURIComponent('a/b c')}/last-paid-tranche`);
});
