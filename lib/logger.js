/**
 * Write Action Logger
 * Logs all WRITE actions (POST, PUT, DELETE) to /logs folder
 */

const fs = require('fs');
const path = require('path');

const LOGS_DIR = path.join(__dirname, '..', 'logs');

function ensureLogsDir() {
  if (!fs.existsSync(LOGS_DIR)) {
    fs.mkdirSync(LOGS_DIR, { recursive: true });
  }
}

function sanitizeFilename(str) {
  return str.replace(/[^a-zA-Z0-9-_]/g, '-').substring(0, 50);
}

function logWriteAction({ action, entity, endpoint, request, response, status, refNumber, linkedEntities }) {
  ensureLogsDir();

  const timestamp = new Date().toISOString();
  const safeTimestamp = timestamp.replace(/[:.]/g, '-');
  const safeRef = refNumber ? sanitizeFilename(refNumber) : 'no-ref';

  const filename = `${action}_${entity}_${safeRef}_${safeTimestamp}.json`;
  const filepath = path.join(LOGS_DIR, filename);

  const logEntry = {
    timestamp,
    action,
    entity,
    endpoint,
    request,
    response,
    status,
    refNumber: refNumber || null,
    linkedEntities: linkedEntities || []
  };

  fs.writeFileSync(filepath, JSON.stringify(logEntry, null, 2));

  console.log(`[LOG] Written to: logs/${filename}`);

  return filepath;
}

module.exports = { logWriteAction };
