# QuickBooks Data Project

## Purpose

This project integrates with QuickBooks Desktop via **Conductor.is** API to access accounting data.

## What is Conductor?

Conductor is a real-time API platform for QuickBooks Desktop. Unlike QuickBooks Online which has a direct API, QuickBooks Desktop requires a connector service - Conductor provides this bridge.

Key characteristics:
- Real-time sync with QuickBooks Desktop
- RESTful API at `https://api.conductor.is/v1`
- Bearer token authentication
- Each end-user maps to one QuickBooks Desktop company file
- Official MCP server for Claude Desktop integration

## Project Structure

```
quickbooks-data/
├── .env                    # API credentials (never commit)
├── .gitignore              # Git ignore rules
├── CLAUDE.md               # This file
├── package.json            # Project config
├── conductor-client.js     # REST API wrapper (auto-logging + duplicate check)
├── test-connection.js      # Connection test script
├── lib/
│   └── logger.js           # Write action logger
├── logs/                   # Auto-generated write action logs
├── devDoc/
│   └── conductor-integration.md  # Detailed integration guide
└── dataPatterns/
    ├── _business_rules.json         # Business rules (NET 30, PO linking, etc.)
    ├── _common_references.json      # Shared IDs (tax codes, accounts, items)
    ├── _template_vendor.json        # Template for new vendors
    ├── _template_customer.json      # Template for new customers
    └── vendor_valks_machinery.json  # Valk's Machinery patterns
```

## Data Patterns (IMPORTANT - CHECK BEFORE ANY TRANSACTION)

Before creating bills, invoices, or other transactions, **check files in this order**:

1. **`_business_rules.json`** - Business rules that OVERRIDE everything else
2. **`_common_references.json`** - Shared IDs for tax codes, accounts, service items
3. **`vendor_*.json`** - Vendor-specific patterns (items used, bill structure)
4. **`customer_*.json`** - Customer-specific patterns (invoice structure)

### Key Business Rules (from `_business_rules.json`)

| Rule | Description |
|------|-------------|
| **Vendor Pattern Required** | MUST have `vendor_*.json` pattern file before creating bills (throws `NO_VENDOR_PATTERN`) |
| **NET 30 EOM** | ALWAYS use this term - ignore terms on vendor invoices |
| **Link to PO** | ALWAYS link bills to existing POs via `linkToTransactionIds` |
| **Duplicate Check** | Auto-checks bill refNumber+vendorId before creating (throws `DUPLICATE_BILL`) |
| **Recent Data** | Use last 1 year of data as reference, not old data |
| **Vendor Items** | Use `Subcontractor:*` items, NOT `Job Type:Manufacturing Job` |

## Bill Entry Workflow

When user provides a vendor invoice PDF:

```
1. EXTRACT      → Read PDF, extract vendor name, invoice #, date, line items, total
2. VENDOR MATCH → Fetch getVendorListForMatching(), fuzzy match vendor name
                  - If match found → use existing vendor ID
                  - If no match    → NEW VENDOR: create vendor in QuickBooks first
3. PATTERN CHECK → Does vendor_*.json exist for this vendor?
                  - If yes  → proceed
                  - If no   → create pattern file first (fetch vendor data, analyze history)
4. PO CHECK     → ALWAYS search for matching ACTIVE PO (use findActivePO or getActivePOs)
                  - Active = not fully received AND not manually closed
                  - If active PO found → link via linkToTransactionIds (auto-populates lines)
                  - If no active PO    → create bill with itemLines from invoice
5. CREATE BILL  → Use createBill() with pattern, auto-checks duplicates
6. MOVE PDF     → Move processed PDF to data/posted/
```

### Vendor Matching (LLM Fuzzy Match)

The code provides `getVendorListForMatching()` which returns all vendors. Claude performs fuzzy matching:

| Invoice Says | QuickBooks Has | Match? |
|--------------|----------------|--------|
| "T.N.T. TOOLS 2025 INC." | "TNT Tools Inc" | ✓ Yes (likely same) |
| "Valks Machinery" | "Valk's Machinery Ltd." | ✓ Yes |
| "ABC Manufacturing" | (not found) | ✗ New vendor |

### Example: Creating a bill for Valk's Machinery

```javascript
// Check dataPatterns/vendor_valks_machinery.json first
const bill = {
  vendor: { id: "800002F2-1498582191" },
  transactionDate: "2026-01-29",
  refNumber: "INVOICE_NUMBER",
  terms: { id: "80000008-1592232388" },  // NET 30 EOM
  itemLines: [{
    item: { id: "80000042-1592233560" },  // Subcontractor:Waterjet Cutting
    description: "Part number / job description",
    quantity: 1,
    rate: "100.00",
    salesTaxCode: { id: "90000-1284388831" }  // HST
  }]
};
```

## Claude Desktop MCP

The Conductor MCP server is configured in Claude Desktop:

**Config location:** `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
    "conductor_quickbooks": {
      "command": "cmd",
      "args": ["/c", "npx", "-y", "conductor-node-mcp"],
      "env": {
        "CONDUCTOR_SECRET_KEY": "sk_conductor_...",
        "CONDUCTOR_END_USER_ID": "end_usr_..."
      }
    }
  }
}
```

**Restart Claude Desktop** after config changes to activate.

## Environment Variables

Required in `.env`:
```
CONDUCTOR_API_KEY=sk_conductor_...
CONDUCTOR_END_USER_ID=end_usr_...
```

## Quick Start

```bash
# Test connection
npm test
```

## Write Action Logging

**All WRITE operations are automatically logged to `/logs` folder.**

- Logs include: timestamp, request, response, status, linked entities
- File format: `{action}_{entity}_{refNumber}_{timestamp}.json`
- Use `ConductorClient` class for auto-logging, or `logWriteAction()` manually

```javascript
const ConductorClient = require('./conductor-client');
const client = new ConductorClient();

// This will auto-check for duplicates and auto-log to /logs
const bill = await client.createBill({
  vendorId: "...",
  transactionDate: "2026-01-27",
  refNumber: "312094",
  linkToTransactionIds: ["PO_ID"]
});

// Skip duplicate check (not recommended)
const bill = await client.createBill(data, { skipDuplicateCheck: true });
```

## Safeguards

| Safeguard | Description |
|-----------|-------------|
| **Vendor Pattern Check** | `createBill()` requires a `vendor_*.json` pattern file to exist (throws `NO_VENDOR_PATTERN`) |
| **Duplicate Detection** | `createBill()` auto-checks if bill # already exists for vendor (throws `DUPLICATE_BILL`) |
| **Write Logging** | All POST/PUT/DELETE operations logged to `/logs` |

```javascript
try {
  await client.createBill(billData);
} catch (err) {
  if (err.code === 'NO_VENDOR_PATTERN') {
    console.log('Create vendor pattern first:', err.vendorId);
  } else if (err.code === 'DUPLICATE_BILL') {
    console.log('Bill already exists:', err.existingBill);
  }
}

// Skip checks (not recommended)
await client.createBill(billData, { skipPatternCheck: true, skipDuplicateCheck: true });
```

## Important Notes

- QuickBooks Desktop must be running on the authenticated machine
- The MCP gives Claude Desktop full access to QuickBooks data
- **Always check dataPatterns/ before posting transactions**
- **All WRITE actions are logged to /logs for audit trail**
- See `devDoc/conductor-integration.md` for full documentation
