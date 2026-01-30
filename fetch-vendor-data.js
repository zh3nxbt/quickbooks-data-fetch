/**
 * Fetch Valk's Machinery - LATEST data (last 1 year)
 */

require('dotenv').config();

const API_BASE = 'https://api.conductor.is/v1';

async function conductorRequest(endpoint) {
  const response = await fetch(`${API_BASE}${endpoint}`, {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${process.env.CONDUCTOR_API_KEY}`,
      'Conductor-End-User-Id': process.env.CONDUCTOR_END_USER_ID,
      'Content-Type': 'application/json'
    }
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: response.statusText }));
    throw new Error(`API Error ${response.status}: ${error.error?.message || error.message}`);
  }

  return response.json();
}

async function fetchAllPages(baseEndpoint, maxPages = 20) {
  let allData = [];
  let cursor = null;
  let page = 0;

  do {
    const url = cursor
      ? `${baseEndpoint}${baseEndpoint.includes('?') ? '&' : '?'}cursor=${cursor}`
      : baseEndpoint;
    const response = await conductorRequest(url);
    allData = allData.concat(response.data || []);
    cursor = response.nextCursor;
    page++;
    process.stdout.write(`  Page ${page}: ${allData.length} records...\r`);
  } while (cursor && page < maxPages);

  console.log('');
  return allData;
}

const VALKS_ID = '800002F2-1498582191';
const ONE_YEAR_AGO = new Date();
ONE_YEAR_AGO.setFullYear(ONE_YEAR_AGO.getFullYear() - 1);
const DATE_FILTER = ONE_YEAR_AGO.toISOString().split('T')[0]; // YYYY-MM-DD

async function main() {
  console.log('='.repeat(70));
  console.log('Fetching Valk\'s Machinery - LATEST DATA (Last 1 Year)');
  console.log(`Date filter: >= ${DATE_FILTER}`);
  console.log('='.repeat(70));
  console.log('');

  try {
    // 1. Get recent POs for Valks
    console.log('[1] Fetching recent Purchase Orders...\n');
    const allPOs = await fetchAllPages('/quickbooks-desktop/purchase-orders?limit=150', 20);

    // Filter to Valks and last year
    const valksPOs = allPOs.filter(po =>
      (po.vendor?.id === VALKS_ID || po.vendor?.fullName?.toLowerCase().includes('valk')) &&
      po.transactionDate >= DATE_FILTER
    );

    console.log(`\nFound ${valksPOs.length} Valk's POs in last year\n`);

    if (valksPOs.length > 0) {
      // Sort by date descending (newest first)
      valksPOs.sort((a, b) => b.transactionDate.localeCompare(a.transactionDate));

      console.log('--- Recent POs (newest first) ---\n');
      valksPOs.slice(0, 5).forEach((po, i) => {
        console.log(`PO ${i + 1}: #${po.refNumber} (${po.transactionDate}) - Total: $${po.totalAmount}`);
        if (po.lines && po.lines.length > 0) {
          po.lines.forEach(line => {
            if (line.item && !line.item.fullName?.includes('HST') && !line.item.fullName?.includes('PST')) {
              console.log(`  Item: ${line.item.fullName}`);
              console.log(`  Description: ${line.description}`);
              console.log(`  Qty: ${line.quantity} @ $${line.rate} = $${line.amount}`);
              console.log('');
            }
          });
        }
        console.log('---');
      });

      // Show full JSON of most recent PO
      console.log('\n\nFull JSON of most recent PO:');
      console.log(JSON.stringify(valksPOs[0], null, 2));
    }

    // 2. Get recent Bills for Valks
    console.log('\n\n[2] Fetching recent Bills...\n');
    const allBills = await fetchAllPages('/quickbooks-desktop/bills?limit=150', 20);

    const valksBills = allBills.filter(b =>
      (b.vendor?.id === VALKS_ID || b.vendor?.fullName?.toLowerCase().includes('valk')) &&
      b.transactionDate >= DATE_FILTER
    );

    console.log(`\nFound ${valksBills.length} Valk's Bills in last year\n`);

    if (valksBills.length > 0) {
      valksBills.sort((a, b) => b.transactionDate.localeCompare(a.transactionDate));

      console.log('--- Recent Bills (newest first) ---\n');
      valksBills.slice(0, 5).forEach((bill, i) => {
        console.log(`Bill ${i + 1}: #${bill.refNumber} (${bill.transactionDate}) - Amount: $${bill.amountDue}`);
        console.log(`  Memo: ${bill.memo || 'N/A'}`);

        if (bill.expenseLines && bill.expenseLines.length > 0) {
          console.log('  Expense Lines:');
          bill.expenseLines.forEach(line => {
            console.log(`    Account: ${line.account?.fullName} | $${line.amount} | ${line.memo || 'no memo'}`);
          });
        }

        if (bill.itemLines && bill.itemLines.length > 0) {
          console.log('  Item Lines:');
          bill.itemLines.forEach(line => {
            console.log(`    Item: ${line.item?.fullName} | Qty: ${line.quantity} @ $${line.rate} | ${line.description || 'no desc'}`);
          });
        }
        console.log('---');
      });

      // Show full JSON of most recent bill
      console.log('\n\nFull JSON of most recent Bill:');
      console.log(JSON.stringify(valksBills[0], null, 2));
    }

  } catch (error) {
    console.error('Error:', error.message);
  }
}

main();
