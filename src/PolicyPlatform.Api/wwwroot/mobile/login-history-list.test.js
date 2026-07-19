const { test } = require('node:test');
const assert = require('node:assert/strict');
const { renderLoginHistoryList } = require('./login-history-list.js');

// login-history-list.js calls document.createElement at call time (not at require
// time), so a minimal fake DOM installed on the global is enough to exercise it
// under node:test without pulling in a jsdom dependency.
function fakeElement() {
  return {
    className: '',
    dataset: {},
    innerHTML: '',
    children: [],
    appendChild(child) {
      this.children.push(child);
    },
  };
}

global.document = { createElement: () => fakeElement() };

test('renderLoginHistoryList: returns an empty list element for no items', () => {
  const list = renderLoginHistoryList([]);
  assert.equal(list.className, 'login-history-list');
  assert.equal(list.children.length, 0);
});

test('renderLoginHistoryList: renders one row per item with the loginId on the dataset', () => {
  const items = [
    { loginId: 'a', deviceLabel: 'iPhone', occurredAtLabel: '01.01.2024', osLabel: null, ipAddress: null },
    { loginId: 'b', deviceLabel: 'Pixel', occurredAtLabel: '02.01.2024', osLabel: null, ipAddress: null },
  ];
  const list = renderLoginHistoryList(items);
  assert.equal(list.children.length, 2);
  assert.equal(list.children[0].className, 'login-history-row');
  assert.equal(list.children[0].dataset.loginId, 'a');
  assert.equal(list.children[1].dataset.loginId, 'b');
});

test('renderLoginHistoryList: row markup includes device label and time', () => {
  const list = renderLoginHistoryList([
    { loginId: 'a', deviceLabel: 'iPhone Kasi', occurredAtLabel: '01.01.2024 10:00', osLabel: null, ipAddress: null },
  ]);
  const row = list.children[0];
  assert.ok(row.innerHTML.includes('iPhone Kasi'));
  assert.ok(row.innerHTML.includes('01.01.2024 10:00'));
});

test('renderLoginHistoryList: omits secondary os/ip spans when both are absent', () => {
  const list = renderLoginHistoryList([
    { loginId: 'a', deviceLabel: 'iPhone', occurredAtLabel: '01.01.2024', osLabel: null, ipAddress: null },
  ]);
  const row = list.children[0];
  assert.equal(row.innerHTML.includes('<span>'), false);
});

test('renderLoginHistoryList: includes os label and ip address when present', () => {
  const list = renderLoginHistoryList([
    { loginId: 'a', deviceLabel: 'iPhone', occurredAtLabel: '01.01.2024', osLabel: 'iOS 17.4', ipAddress: '10.0.0.1' },
  ]);
  const row = list.children[0];
  assert.ok(row.innerHTML.includes('iOS 17.4'));
  assert.ok(row.innerHTML.includes('10.0.0.1'));
});
