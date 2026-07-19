const { test } = require('node:test');
const assert = require('node:assert/strict');
const { LOGIN_HISTORY_ENDPOINT, LoginHistoryError, fetchLoginHistory } = require('./login-history-service.js');

function fakeFetch(status, body) {
  return async () => ({
    status,
    ok: status >= 200 && status < 300,
    json: async () => body,
  });
}

test('fetchLoginHistory: sends Authorization header when token is provided', async () => {
  let seenUrl, seenOptions;
  const fetchImpl = async (url, options) => {
    seenUrl = url;
    seenOptions = options;
    return { status: 200, ok: true, json: async () => ({ items: [] }) };
  };
  await fetchLoginHistory('abc.def.ghi', fetchImpl);
  assert.equal(seenUrl, LOGIN_HISTORY_ENDPOINT);
  assert.equal(seenOptions.method, 'GET');
  assert.equal(seenOptions.headers.Authorization, 'Bearer abc.def.ghi');
});

test('fetchLoginHistory: omits Authorization header when token is missing', async () => {
  let seenOptions;
  const fetchImpl = async (url, options) => {
    seenOptions = options;
    return { status: 200, ok: true, json: async () => ({ items: [] }) };
  };
  await fetchLoginHistory(undefined, fetchImpl);
  assert.equal('Authorization' in seenOptions.headers, false);
});

test('fetchLoginHistory: returns items array on 200', async () => {
  const items = [{ loginId: '1', occurredAt: '2026-07-19T00:00:00Z', deviceType: 'PHONE' }];
  const result = await fetchLoginHistory('t', fakeFetch(200, { items }));
  assert.deepEqual(result, items);
});

test('fetchLoginHistory: falls back to empty array when items field is missing', async () => {
  const result = await fetchLoginHistory('t', fakeFetch(200, {}));
  assert.deepEqual(result, []);
});

test('fetchLoginHistory: falls back to empty array when items is not an array', async () => {
  const result = await fetchLoginHistory('t', fakeFetch(200, { items: 'not-an-array' }));
  assert.deepEqual(result, []);
});

test('fetchLoginHistory: throws LoginHistoryError with status 401 on unauthorized', async () => {
  await assert.rejects(
    () => fetchLoginHistory('t', fakeFetch(401, {})),
    (err) => {
      assert.ok(err instanceof LoginHistoryError);
      assert.equal(err.status, 401);
      assert.equal(err.message, 'Sesja wygasła. Zaloguj się ponownie.');
      return true;
    }
  );
});

test('fetchLoginHistory: throws LoginHistoryError with status 500 on server error', async () => {
  await assert.rejects(
    () => fetchLoginHistory('t', fakeFetch(500, {})),
    (err) => {
      assert.ok(err instanceof LoginHistoryError);
      assert.equal(err.status, 500);
      assert.equal(err.message, 'Nie udało się pobrać historii logowań. Spróbuj ponownie później.');
      return true;
    }
  );
});

test('fetchLoginHistory: throws LoginHistoryError for other non-ok statuses', async () => {
  await assert.rejects(
    () => fetchLoginHistory('t', fakeFetch(404, {})),
    (err) => {
      assert.ok(err instanceof LoginHistoryError);
      assert.equal(err.status, 404);
      return true;
    }
  );
});

test('fetchLoginHistory: throws LoginHistoryError with status 0 when the network call rejects', async () => {
  const fetchImpl = async () => {
    throw new Error('network down');
  };
  await assert.rejects(
    () => fetchLoginHistory('t', fetchImpl),
    (err) => {
      assert.ok(err instanceof LoginHistoryError);
      assert.equal(err.status, 0);
      assert.equal(err.message, 'Brak połączenia z serwerem.');
      return true;
    }
  );
});
