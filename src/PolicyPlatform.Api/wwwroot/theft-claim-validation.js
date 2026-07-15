// Walidacja formularza zgłoszenia kradzieży (AISDLC-40).
// UMD-style export so this pure module is testable from Node without a build step.
(function (root, factory) {
  if (typeof module === 'object' && module.exports) {
    module.exports = factory();
  } else {
    root.TheftClaimValidation = factory();
  }
})(typeof self !== 'undefined' ? self : this, function () {
  // Numer zgłoszenia Policji: brak jednego oficjalnego standardu, więc akceptujemy
  // typowe formaty ("L.dz. 123/26/RSD", "RSD-1234/26" itp.) — litery, cyfry,
  // kropki, myślniki, ukośniki i spacje, min. 3 znaki, przynajmniej jedna cyfra.
  const POLICE_REPORT_NUMBER_PATTERN = /^(?=.*\d)[A-Za-z0-9./ -]{3,40}$/;

  const ERRORS = {
    POLICE_REPORT_NUMBER_REQUIRED: 'Numer zgłoszenia Policji jest wymagany.',
    POLICE_REPORT_NUMBER_INVALID_FORMAT:
      'Nieprawidłowy format numeru zgłoszenia Policji (dozwolone litery, cyfry, kropki, myślniki, ukośniki, min. 3 znaki, w tym co najmniej jedna cyfra).',
    INCIDENT_DATE_REQUIRED: 'Data kradzieży jest wymagana.',
    INCIDENT_DATE_FUTURE: 'Data kradzieży nie może być w przyszłości.',
  };

  function validatePoliceReportNumber(value) {
    const trimmed = (value ?? '').trim();
    if (!trimmed) return ERRORS.POLICE_REPORT_NUMBER_REQUIRED;
    if (!POLICE_REPORT_NUMBER_PATTERN.test(trimmed)) return ERRORS.POLICE_REPORT_NUMBER_INVALID_FORMAT;
    return null;
  }

  function validateIncidentDate(value) {
    const trimmed = (value ?? '').trim();
    if (!trimmed) return ERRORS.INCIDENT_DATE_REQUIRED;
    const date = new Date(trimmed);
    if (Number.isNaN(date.getTime())) return ERRORS.INCIDENT_DATE_REQUIRED;
    const endOfToday = new Date();
    endOfToday.setHours(23, 59, 59, 999);
    if (date.getTime() > endOfToday.getTime()) return ERRORS.INCIDENT_DATE_FUTURE;
    return null;
  }

  function validateTheftClaimForm(form) {
    const errors = {};
    const policeReportNumberError = validatePoliceReportNumber(form.policeReportNumber);
    if (policeReportNumberError) errors.policeReportNumber = policeReportNumberError;
    const incidentDateError = validateIncidentDate(form.incidentDate);
    if (incidentDateError) errors.incidentDate = incidentDateError;
    return { valid: Object.keys(errors).length === 0, errors };
  }

  return {
    POLICE_REPORT_NUMBER_PATTERN,
    ERRORS,
    validatePoliceReportNumber,
    validateIncidentDate,
    validateTheftClaimForm,
  };
});
