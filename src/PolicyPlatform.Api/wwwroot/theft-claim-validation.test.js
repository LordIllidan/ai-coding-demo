const { test } = require('node:test');
const assert = require('node:assert/strict');
const {
  validatePoliceReportNumber,
  validateIncidentDate,
  validateTheftClaimForm,
  ERRORS,
} = require('./theft-claim-validation.js');

test('validatePoliceReportNumber: rejects empty/missing values', () => {
  assert.equal(validatePoliceReportNumber(''), ERRORS.POLICE_REPORT_NUMBER_REQUIRED);
  assert.equal(validatePoliceReportNumber('   '), ERRORS.POLICE_REPORT_NUMBER_REQUIRED);
  assert.equal(validatePoliceReportNumber(undefined), ERRORS.POLICE_REPORT_NUMBER_REQUIRED);
  assert.equal(validatePoliceReportNumber(null), ERRORS.POLICE_REPORT_NUMBER_REQUIRED);
});

test('validatePoliceReportNumber: rejects too-short values', () => {
  assert.equal(validatePoliceReportNumber('A1'), ERRORS.POLICE_REPORT_NUMBER_INVALID_FORMAT);
});

test('validatePoliceReportNumber: rejects values without a digit', () => {
  assert.equal(validatePoliceReportNumber('ABC/RSD'), ERRORS.POLICE_REPORT_NUMBER_INVALID_FORMAT);
});

test('validatePoliceReportNumber: rejects disallowed characters', () => {
  assert.equal(validatePoliceReportNumber('RSD#123'), ERRORS.POLICE_REPORT_NUMBER_INVALID_FORMAT);
});

test('validatePoliceReportNumber: rejects values over 40 characters', () => {
  const tooLong = 'A1'.repeat(21); // 42 chars, contains digits
  assert.equal(validatePoliceReportNumber(tooLong), ERRORS.POLICE_REPORT_NUMBER_INVALID_FORMAT);
});

test('validatePoliceReportNumber: accepts typical formats', () => {
  assert.equal(validatePoliceReportNumber('L.dz. 123/26/RSD'), null);
  assert.equal(validatePoliceReportNumber('RSD-1234/26'), null);
  assert.equal(validatePoliceReportNumber('AB1'), null);
});

test('validatePoliceReportNumber: trims surrounding whitespace before validating', () => {
  assert.equal(validatePoliceReportNumber('  RSD-1234/26  '), null);
});

test('validateIncidentDate: rejects empty/missing values', () => {
  assert.equal(validateIncidentDate(''), ERRORS.INCIDENT_DATE_REQUIRED);
  assert.equal(validateIncidentDate('   '), ERRORS.INCIDENT_DATE_REQUIRED);
  assert.equal(validateIncidentDate(undefined), ERRORS.INCIDENT_DATE_REQUIRED);
  assert.equal(validateIncidentDate(null), ERRORS.INCIDENT_DATE_REQUIRED);
});

test('validateIncidentDate: rejects unparseable dates', () => {
  assert.equal(validateIncidentDate('not-a-date'), ERRORS.INCIDENT_DATE_REQUIRED);
});

test('validateIncidentDate: rejects future dates', () => {
  const future = new Date(Date.now() + 7 * 24 * 3600 * 1000).toISOString().slice(0, 10);
  assert.equal(validateIncidentDate(future), ERRORS.INCIDENT_DATE_FUTURE);
});

test('validateIncidentDate: accepts today', () => {
  const today = new Date().toISOString().slice(0, 10);
  assert.equal(validateIncidentDate(today), null);
});

test('validateIncidentDate: accepts past dates', () => {
  assert.equal(validateIncidentDate('2020-01-15'), null);
});

test('validateTheftClaimForm: valid form has no errors', () => {
  const result = validateTheftClaimForm({
    policeReportNumber: 'L.dz. 123/26/RSD',
    incidentDate: '2020-01-15',
  });
  assert.equal(result.valid, true);
  assert.deepEqual(result.errors, {});
});

test('validateTheftClaimForm: reports both fields when both are missing', () => {
  const result = validateTheftClaimForm({ policeReportNumber: '', incidentDate: '' });
  assert.equal(result.valid, false);
  assert.equal(result.errors.policeReportNumber, ERRORS.POLICE_REPORT_NUMBER_REQUIRED);
  assert.equal(result.errors.incidentDate, ERRORS.INCIDENT_DATE_REQUIRED);
});

test('validateTheftClaimForm: reports only the invalid field', () => {
  const result = validateTheftClaimForm({
    policeReportNumber: 'L.dz. 123/26/RSD',
    incidentDate: '',
  });
  assert.equal(result.valid, false);
  assert.equal('policeReportNumber' in result.errors, false);
  assert.equal(result.errors.incidentDate, ERRORS.INCIDENT_DATE_REQUIRED);
});
