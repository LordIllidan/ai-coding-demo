const { test } = require('node:test');
const assert = require('node:assert/strict');
const { LOGIN_HISTORY_ENDPOINT, LoginHistoryError, fetchLoginHistory } = require('./login-history-service.js');

function fakeFetch({ status, ok, body }) {
  const calls = [];
  const impl = async (url, options) => {
    calls.push({ url, options });
    return {
      status,
      ok,
      json: async () => body,
    };
  };
  impl.calls = calls;
  return impl;
}

test('fetchLoginHistory: sends an Authorization header when a token is provided', async () => {
  const impl = fakeFetch({ status: 200, ok: true, body: { items: [] } });
  await fetchLoginHistory('abc123', impl);
  assert.equal(impl.calls.length, 1);
  assert.equal(impl.calls[0].url, LOGIN_HISTORY_ENDPOINT);
  assert.equal(impl.calls[0].options.method, 'GET');
  assert.deepEqual(impl.calls[0].options.headers, { Authorization: 'Bearer abc123' });
});

test('fetchLoginHistory: omits the Authorization header when no token is provided', async () => {
  const impl = fakeFetch({ status: 200, ok: true, body: { items: [] } });
  await fetchLoginHistory(null, impl);
  assert.deepEqual(impl.calls[0].options.headers, {});
});

test('fetchLoginHistory: returns items from a 200 response', async () => {
  const items = [{ loginId: '1' }, { loginId: '2' }];
  const impl = fakeFetch({ status: 200, ok: true, body: { items } });
  const result = await fetchLoginHistory('tok', impl);
  assert.deepEqual(result, items);
});

test('fetchLoginHistory: returns an empty array when items is missing or not an array', async () => {
  const implMissing = fakeFetch({ status: 200, ok: true, body: {} });
  assert.deepEqual(await fetchLoginHistory('tok', implMissing), []);

  const implWrongType = fakeFetch({ status: 200, ok: true, body: { items: 'not-an-array' } });
  assert.deepEqual(await fetchLoginHistory('tok', implWrongType), []);
});

test('fetchLoginHistory: throws a 401 LoginHistoryError on an expired/missing token', async () => {
  const impl = fakeFetch({ status: 401, ok: false, body: {} });
  await assert.rejects(
    () => fetchLoginHistory('expired', impl),
    (err) => {
      assert.ok(err instanceof LoginHistoryError);
      assert.equal(err.status, 401);
      assert.equal(err.message, 'Sesja wygasła. Zaloguj się ponownie.');
      return true;
    },
  );
});

test('fetchLoginHistory: throws a generic LoginHistoryError for other non-ok statuses', async () => {
  const impl = fakeFetch({ status: 500, ok: false, body: {} });
  await assert.rejects(
    () => fetchLoginHistory('tok', impl),
    (err) => {
      assert.ok(err instanceof LoginHistoryError);
      assert.equal(err.status, 500);
      assert.equal(err.message, 'Nie udało się pobrać historii logowań. Spróbuj ponownie później.');
      return true;
    },
  );
});

test('fetchLoginHistory: throws a status-0 LoginHistoryError when the fetch itself fails', async () => {
  const impl = async () => {
    throw new Error('network down');
  };
  await assert.rejects(
    () => fetchLoginHistory('tok', impl),
    (err) => {
      assert.ok(err instanceof LoginHistoryError);
      assert.equal(err.status, 0);
      assert.equal(err.message, 'Brak połączenia z serwerem.');
      return true;
    },
  );
});
