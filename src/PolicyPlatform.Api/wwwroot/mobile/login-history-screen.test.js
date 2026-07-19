const { test } = require('node:test');
const assert = require('node:assert/strict');

const SCREEN_MODULE_PATH = require.resolve('./login-history-screen.js');
const { STATUS, buildViewState } = require('./login-history-viewmodel.js');

// login-history-screen.js is a plain (non-UMD) browser script: it reads
// `window.*` collaborators into local consts at require time and attaches
// itself to `window.initLoginHistoryScreen` rather than module.exports.
// To unit test it under node:test without jsdom, each test installs its own
// fake `window`/`document` globals and re-requires the module fresh so the
// destructured collaborators are picked up from that test's stubs.
function fakeParagraph() {
  return { textContent: '' };
}

function fakeRoot() {
  const appended = [];
  return {
    innerHTML: '',
    appendChild(node) {
      appended.push(node);
    },
    _appended: appended,
  };
}

function loadScreenModule({ fetchLoginHistory, renderLoginHistoryList, token }) {
  delete require.cache[SCREEN_MODULE_PATH];
  global.document = { createElement: () => fakeParagraph(), getElementById: () => null };
  global.window = {
    LoginHistoryService: { fetchLoginHistory },
    LoginHistoryViewModel: { STATUS, buildViewState },
    LoginHistoryList: { renderLoginHistoryList: renderLoginHistoryList ?? ((items) => ({ tag: 'ul', items })) },
    localStorage: { getItem: () => token ?? null },
    addEventListener: () => {},
  };
  require(SCREEN_MODULE_PATH);
  return global.window.initLoginHistoryScreen;
}

test('initLoginHistoryScreen: shows loading first, then the list on success', async () => {
  const items = [{ loginId: 'a', occurredAt: '2024-01-01T00:00:00Z', deviceType: 'PHONE' }];
  const initLoginHistoryScreen = loadScreenModule({
    fetchLoginHistory: async () => items,
    token: 'tok-123',
  });
  const root = fakeRoot();

  await initLoginHistoryScreen(root);

  assert.equal(root._appended.length, 2);
  assert.equal(root._appended[0].textContent, 'Ładowanie historii logowań…');
  assert.equal(root._appended[1].tag, 'ul');
  assert.equal(root._appended[1].items.length, 1);
  assert.equal(root._appended[1].items[0].loginId, 'a');
});

test('initLoginHistoryScreen: passes the stored auth token through to fetchLoginHistory', async () => {
  let receivedToken;
  const initLoginHistoryScreen = loadScreenModule({
    fetchLoginHistory: async (token) => {
      receivedToken = token;
      return [];
    },
    token: 'stored-token',
  });

  await initLoginHistoryScreen(fakeRoot());

  assert.equal(receivedToken, 'stored-token');
});

test('initLoginHistoryScreen: renders the empty state when there are no entries', async () => {
  const initLoginHistoryScreen = loadScreenModule({
    fetchLoginHistory: async () => [],
    token: null,
  });
  const root = fakeRoot();

  await initLoginHistoryScreen(root);

  const final = root._appended[root._appended.length - 1];
  assert.equal(final.textContent, 'Brak zarejestrowanych logowań.');
});

test('initLoginHistoryScreen: renders the error message when the service call rejects', async () => {
  const initLoginHistoryScreen = loadScreenModule({
    fetchLoginHistory: async () => {
      throw new Error('Sesja wygasła. Zaloguj się ponownie.');
    },
    token: 'expired',
  });
  const root = fakeRoot();

  await initLoginHistoryScreen(root);

  const final = root._appended[root._appended.length - 1];
  assert.equal(final.textContent, 'Sesja wygasła. Zaloguj się ponownie.');
});
