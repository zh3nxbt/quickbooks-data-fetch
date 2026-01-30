/**
 * Conductor.is Connection Test (REST API)
 *
 * Run: node test-connection.js
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

async function testConnection() {
  console.log('='.repeat(60));
  console.log('Conductor.is Connection Test (READ ONLY)');
  console.log('='.repeat(60));
  console.log('');

  // Validate environment
  if (!process.env.CONDUCTOR_API_KEY || !process.env.CONDUCTOR_END_USER_ID) {
    console.error('ERROR: Missing credentials in .env file');
    process.exit(1);
  }

  console.log('API Key:', process.env.CONDUCTOR_API_KEY.substring(0, 15) + '...');
  console.log('End User ID:', process.env.CONDUCTOR_END_USER_ID);
  console.log('');

  try {
    // 1. Check End User Status
    console.log('[1] Checking End User Connection...');
    const endUser = await conductorRequest(`/end-users/${process.env.CONDUCTOR_END_USER_ID}`);
    console.log(`    Company: ${endUser.companyName}`);
    console.log(`    Email: ${endUser.email}`);

    const qbConnection = endUser.integrationConnections?.find(c => c.integrationSlug === 'quickbooks_desktop');
    if (qbConnection) {
      console.log(`    QuickBooks Connected: YES`);
      console.log(`    Last Request: ${qbConnection.lastRequestAt}`);
    } else {
      console.log(`    QuickBooks Connected: NO - Complete auth flow first`);
      process.exit(1);
    }
    console.log('');

    // 2. Fetch Customers (READ ONLY)
    console.log('[2] Fetching Customers (READ ONLY)...');
    const customers = await conductorRequest('/quickbooks-desktop/customers?limit=5');
    console.log(`    Found ${customers.data?.length || 0} customers:`);
    customers.data?.slice(0, 5).forEach((c, i) => {
      console.log(`    ${i + 1}. ${c.fullName || c.name}`);
    });
    console.log('');

    // 3. Fetch Accounts (READ ONLY)
    console.log('[3] Fetching Chart of Accounts (READ ONLY)...');
    const accounts = await conductorRequest('/quickbooks-desktop/accounts?limit=5');
    console.log(`    Found ${accounts.data?.length || 0} accounts:`);
    accounts.data?.slice(0, 5).forEach((a, i) => {
      console.log(`    ${i + 1}. ${a.fullName} (${a.accountType})`);
    });
    console.log('');

    console.log('='.repeat(60));
    console.log('SUCCESS! Connection verified - all READ operations working');
    console.log('='.repeat(60));

  } catch (error) {
    console.error('\nERROR:', error.message);
    process.exit(1);
  }
}

testConnection();
