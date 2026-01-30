# Write Action Logs

This folder contains logs of all WRITE actions (POST, PUT, DELETE) made to QuickBooks via Conductor API.

## Purpose

- Permanent record of all data modifications
- Recovery/audit trail if session context is lost
- Debug issues with QuickBooks data

## File Naming

```
{action}_{entity}_{refNumber}_{timestamp}.json
```

Example: `create_bill_312094_2026-01-29T12-02-03.json`

## Log Structure

```json
{
  "timestamp": "ISO 8601 timestamp",
  "action": "create | update | delete",
  "entity": "bill | invoice | purchase_order | etc",
  "endpoint": "API endpoint called",
  "request": { "payload sent" },
  "response": { "response received" },
  "status": "success | error",
  "refNumber": "reference number if applicable",
  "linkedEntities": ["related POs, invoices, etc"]
}
```

## Retention

Keep logs indefinitely for audit purposes.
