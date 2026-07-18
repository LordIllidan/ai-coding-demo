// Ekran mobilny "Transza odszkodowania" (AISDLC-137, parent story AISDLC-119).
// UMD-style export so this pure module is testable from Node without a build step.
(function (root, factory) {
  if (typeof module === 'object' && module.exports) {
    module.exports = factory();
  } else {
    root.LastPaidInstallment = factory();
  }
})(typeof self !== 'undefined' ? self : this, function () {
  const EMPTY_STATE_MESSAGE = 'Nie mamy jeszcze wypłaconej transzy odszkodowania.';

  const ERROR_MESSAGES = {
    CLAIM_ACCESS_DENIED: 'Brak uprawnień do danych szkody.',
    CLAIM_NOT_FOUND: 'Nie znaleziono danych szkody.',
    CLAIM_PAYOUT_LOOKUP_FAILED: 'Wystąpił błąd techniczny. Spróbuj ponownie później.',
    UNKNOWN: 'Wystąpił nieoczekiwany błąd.',
  };

  const REQUIRED_INSTALLMENT_FIELDS = ['installmentId', 'installmentNo', 'paidAt', 'amount', 'currency'];

  // Backend contract: values are only trustworthy for screenState='PAID' and only
  // when every lastPaidInstallment field is present — never render placeholders
  // for a partial record (NO_PAYOUT and INCOMPLETE_DATA share the same empty UI).
  function isCompleteInstallment(installment) {
    if (!installment || typeof installment !== 'object') return false;
    return REQUIRED_INSTALLMENT_FIELDS.every((field) => {
      const value = installment[field];
      return value !== null && value !== undefined && value !== '';
    });
  }

  function mapPayoutResponse(body) {
    if (body && body.screenState === 'PAID' && isCompleteInstallment(body.lastPaidInstallment)) {
      const i = body.lastPaidInstallment;
      return {
        screenState: 'PAID',
        claimNumber: body.claimNumber,
        installment: {
          installmentId: i.installmentId,
          installmentNo: i.installmentNo,
          paidAt: i.paidAt,
          amount: i.amount,
          currency: i.currency,
        },
        canEdit: false,
        message: null,
      };
    }
    return { screenState: 'EMPTY', claimNumber: null, installment: null, canEdit: false, message: EMPTY_STATE_MESSAGE };
  }

  function mapErrorResponse(status, body) {
    if (status === 401) return { kind: 'redirect-login' };
    const code = body && body.code;
    if (status === 403) return { kind: 'message', code: 'CLAIM_ACCESS_DENIED', text: ERROR_MESSAGES.CLAIM_ACCESS_DENIED };
    if (status === 404) return { kind: 'message', code: 'CLAIM_NOT_FOUND', text: ERROR_MESSAGES.CLAIM_NOT_FOUND };
    if (status === 500) return { kind: 'message', code: 'CLAIM_PAYOUT_LOOKUP_FAILED', text: ERROR_MESSAGES.CLAIM_PAYOUT_LOOKUP_FAILED };
    return { kind: 'message', code: code || 'UNKNOWN', text: ERROR_MESSAGES.UNKNOWN };
  }

  function formatAmount(installment) {
    return `${installment.amount.toFixed(2)} ${installment.currency}`;
  }

  return {
    EMPTY_STATE_MESSAGE,
    ERROR_MESSAGES,
    isCompleteInstallment,
    mapPayoutResponse,
    mapErrorResponse,
    formatAmount,
  };
});
