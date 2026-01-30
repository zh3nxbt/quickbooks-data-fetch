/**
 * Test: Create Bill from Valk's Machinery Invoice #312094
 * Links to PO #1050
 */

require('dotenv').config();

const API_BASE = 'https://api.conductor.is/v1';

async function conductorRequest(endpoint, method = 'GET', body = null) {
  const options = {
    method,
    headers: {
      'Authorization': `Bearer ${process.env.CONDUCTOR_API_KEY}`,
      'Conductor-End-User-Id': process.env.CONDUCTOR_END_USER_ID,
      'Content-Type': 'application/json'
    }
  };

  if (body) {
    options.body = JSON.stringify(body);
  }

  const response = await fetch(`${API_BASE}${endpoint}`, options);
  const data = await response.json();

  if (!response.ok) {
    throw new Error(`API Error ${response.status}: ${JSON.stringify(data, null, 2)}`);
  }

  return data;
}

async function findPO(refNumber) {
  console.log(`[1] Searching for PO #${refNumber}...\n`);

  let cursor = null;
  let found = null;
  let page = 0;

  do {
    const url = cursor
      ? `/quickbooks-desktop/purchase-orders?limit=150&cursor=${cursor}`
      : '/quickbooks-desktop/purchase-orders?limit=150';

    const response = await conductorRequest(url);

    found = response.data?.find(po => po.refNumber === refNumber);
    if (found) break;

    cursor = response.nextCursor;
    page++;
    process.stdout.write(`  Searching page ${page}...\r`);
  } while (cursor && page < 20);

  console.log('');
  return found;
}

async function main() {
  console.log('='.repeat(60));
  console.log('Test: Create Bill from Invoice #312094');
  console.log('Vendor: Valk\'s Machinery Ltd.');
  console.log('PO Reference: 1050');
  console.log('='.repeat(60));
  console.log('');

  try {
    // Step 1: Find PO #1050
    const po = await findPO('1050');

    if (!po) {
      console.error('ERROR: PO #1050 not found');
      process.exit(1);
    }

    console.log('PO Found:');
    console.log(`  ID: ${po.id}`);
    console.log(`  Ref: ${po.refNumber}`);
    console.log(`  Date: ${po.transactionDate}`);
    console.log(`  Total: $${po.totalAmount}`);
    console.log(`  Vendor: ${po.vendor?.fullName}`);
    console.log('');

    // Step 2: Prepare bill payload
    const billPayload = {
      vendorId: "800002F2-1498582191",  // Valk's Machinery
      transactionDate: "2026-01-27",     // Invoice date
      refNumber: "312094",               // Vendor invoice number
      termsId: "80000008-1592232388",    // NET 30 EOM (business rule)
      linkToTransactionIds: [po.id]      // Link to PO #1050
    };

    console.log('[2] Bill Payload:');
    console.log(JSON.stringify(billPayload, null, 2));
    console.log('');

    // Step 3: Create the bill
    console.log('[3] Creating bill...\n');
    const bill = await conductorRequest('/quickbooks-desktop/bills', 'POST', billPayload);

    console.log('SUCCESS! Bill created:');
    console.log(JSON.stringify(bill, null, 2));

  } catch (error) {
    console.error('ERROR:', error.message);
    process.exit(1);
  }
}

main();
