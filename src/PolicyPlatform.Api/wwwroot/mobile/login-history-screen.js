// Kontroler ekranu historii logowań (AISDLC-188): spina serwis, viewmodel i komponent listy.
(function () {
  const { fetchLoginHistory } = window.LoginHistoryService;
  const { STATUS, buildViewState } = window.LoginHistoryViewModel;
  const { renderLoginHistoryList } = window.LoginHistoryList;

  const AUTH_TOKEN_STORAGE_KEY = 'mobile_auth_token';

  function render(root, viewState) {
    root.innerHTML = '';

    if (viewState.status === STATUS.LOADING) {
      const loading = document.createElement('p');
      loading.className = 'login-history-loading';
      loading.textContent = 'Ładowanie historii logowań…';
      root.appendChild(loading);
      return;
    }

    if (viewState.status === STATUS.ERROR) {
      const error = document.createElement('p');
      error.className = 'login-history-error';
      error.textContent = viewState.errorMessage;
      root.appendChild(error);
      return;
    }

    if (viewState.status === STATUS.EMPTY) {
      const empty = document.createElement('p');
      empty.className = 'login-history-empty';
      empty.textContent = 'Brak zarejestrowanych logowań.';
      root.appendChild(empty);
      return;
    }

    root.appendChild(renderLoginHistoryList(viewState.items));
  }

  async function initLoginHistoryScreen(root) {
    render(root, buildViewState({ loading: true }));

    const token = window.localStorage.getItem(AUTH_TOKEN_STORAGE_KEY);
    try {
      const entries = await fetchLoginHistory(token);
      render(root, buildViewState({ loading: false, entries }));
    } catch (error) {
      render(root, buildViewState({ loading: false, error }));
    }
  }

  window.addEventListener('DOMContentLoaded', () => {
    const root = document.getElementById('login-history-root');
    if (root) initLoginHistoryScreen(root);
  });

  window.initLoginHistoryScreen = initLoginHistoryScreen;
})();
