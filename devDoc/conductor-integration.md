# Conductor.is Integration Guide

## Overview

Conductor is a modern API platform that enables real-time connectivity to QuickBooks Desktop instances. It allows you to fetch, create, and update accounting data programmatically without needing direct access to QuickBooks Desktop.

## Architecture

- **Organizations**: Represent companies using Conductor
- **Projects**: Segment work within organizations (e.g., testing, production)
- **End-users**: Map individual application users to specific QuickBooks Desktop company files

Each end-user connects to one QuickBooks Desktop instance.

## Setup Steps

### 1. Account Setup (Conductor Dashboard)

1. Register at [Conductor Dashboard](https://dashboard.conductor.is) with work email
2. Create an organization (your company name)
3. Select or create a project for development/testing

### 2. End-User Connection

1. Click "Create end user" in the dashboard
2. Visit the auto-generated auth session URL **on the same computer** where QuickBooks Desktop is installed
3. Complete the browser-based authentication flow

### 3. API Credentials

1. Generate a secret key from the **API Keys** tab
2. Retrieve the **End-User ID** from the End Users tab
3. Store both securely in your `.env` file

## Authentication

Conductor uses bearer token authentication with two required headers:

```
Authorization: Bearer YOUR_SECRET_KEY
Conductor-End-User-Id: YOUR_END_USER_ID
```

## API Base URL

```
https://api.conductor.is/v1
```

## Code Examples

### Using the Conductor Client (Recommended)

```javascript
require('dotenv').config();
const ConductorClient = require('./conductor-client');

const client = new ConductorClient();

// Get customers
const customers = await client.getCustomers({ limit: 10 });
console.log(customers.data);

// Get a specific customer
const customer = await client.getCustomer('8000049F-1750774893');

// Get accounts
const accounts = await client.getAccounts();

// Get invoices
const invoices = await client.getInvoices({ limit: 50 });
```

### Direct REST API

```javascript
require('dotenv').config();

const response = await fetch('https://api.conductor.is/v1/quickbooks-desktop/customers?limit=10', {
  headers: {
    'Authorization': `Bearer ${process.env.CONDUCTOR_API_KEY}`,
    'Conductor-End-User-Id': process.env.CONDUCTOR_END_USER_ID
  }
});

const data = await response.json();
console.log(data);
```

### cURL

```bash
curl -X GET "https://api.conductor.is/v1/quickbooks-desktop/customers?limit=10" \
  -H "Authorization: Bearer $CONDUCTOR_API_KEY" \
  -H "Conductor-End-User-Id: $CONDUCTOR_END_USER_ID"
```

## Available Endpoints (READ ONLY)

| Resource | Endpoint |
|----------|----------|
| Customers | `/v1/quickbooks-desktop/customers` |
| Vendors | `/v1/quickbooks-desktop/vendors` |
| Accounts | `/v1/quickbooks-desktop/accounts` |
| Invoices | `/v1/quickbooks-desktop/invoices` |
| Bills | `/v1/quickbooks-desktop/bills` |
| Items | `/v1/quickbooks-desktop/items` |
| Employees | `/v1/quickbooks-desktop/employees` |
| Sales Receipts | `/v1/quickbooks-desktop/sales-receipts` |
| Checks | `/v1/quickbooks-desktop/checks` |
| End User | `/v1/end-users/{id}` |

## Query Parameters

Common query parameters for list endpoints:

| Parameter | Description |
|-----------|-------------|
| `limit` | Max records to return (default varies) |
| `cursor` | Pagination cursor for next page |
| `updatedAfter` | Filter by modification date |

## Response Format

```json
{
  "objectType": "list",
  "url": "/v1/quickbooks-desktop/customers",
  "data": [
    {
      "id": "8000049F-1750774893",
      "objectType": "qbd_customer",
      "fullName": "Customer Name",
      "isActive": true,
      ...
    }
  ],
  "nextCursor": "cursor_for_next_page"
}
```

## Troubleshooting

1. **Connection Failed**: Ensure QuickBooks Desktop is running and the auth session was completed on the correct machine
2. **401 Unauthorized**: Verify your API key is correct
3. **404 Not Found**: Check your End-User ID is correct
4. **Empty Responses**: Check that the QuickBooks company file has data in the requested resource

## Important Notes

- The npm package `conductor-node` does NOT work with production API
- Use the REST API directly or the included `conductor-client.js` wrapper
- All operations in this project are READ ONLY

## Resources

- [Conductor Documentation](https://docs.conductor.is)
- [Conductor Dashboard](https://dashboard.conductor.is)
