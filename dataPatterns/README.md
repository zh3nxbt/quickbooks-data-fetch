# Data Patterns

This folder contains JSON pattern files for vendors and customers, documenting how data is typically posted to QuickBooks Desktop.

## IMPORTANT: Check Order

Before creating any transaction, check files in this order:

1. **`_business_rules.json`** - Universal rules (NET 30 EOM, PO linking, etc.)
2. **`_common_references.json`** - Shared IDs (tax codes, accounts, items)
3. **`vendor_*.json` or `customer_*.json`** - Entity-specific patterns

## File Structure

| File | Purpose |
|------|---------|
| `_business_rules.json` | Business rules that override defaults (MUST CHECK FIRST) |
| `_common_references.json` | Shared IDs for tax codes, accounts, service items |
| `_template_vendor.json` | Template for creating new vendor patterns |
| `_template_customer.json` | Template for creating new customer patterns |
| `vendor_*.json` | Individual vendor patterns |
| `customer_*.json` | Individual customer patterns |

## Key Business Rules (Summary)

- **Payment Terms**: ALWAYS use `NET 30 EOM` - ignore terms on vendor invoices
- **PO Linking**: ALWAYS link bills to existing POs using `linkToTransactionIds`
- **Data Source**: Use last 1 year of data as reference patterns
- **Vendor Items**: Use `Subcontractor:*` items, NOT `Job Type:Manufacturing Job`

See `_business_rules.json` for complete rules.

## File Naming Convention

- Vendors: `vendor_{name}.json`
- Customers: `customer_{name}.json`

## Usage Notes

1. **Business rules override patterns** - If a rule conflicts with a pattern, follow the rule
2. **Patterns are guidelines** - New cases may require different items or accounts
3. **Always verify IDs are current** - Items/accounts may be added or modified
4. **Use recent data** - Pull last 1 year of data, not old historical data
