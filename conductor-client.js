/**
 * Conductor.is REST API Client
 *
 * Simple wrapper for QuickBooks Desktop API via Conductor
 * Includes automatic logging for all WRITE operations
 */

require('dotenv').config();
const fs = require('fs');
const path = require('path');
const { logWriteAction } = require('./lib/logger');

const API_BASE = 'https://api.conductor.is/v1';
const DATA_PATTERNS_DIR = path.join(__dirname, 'dataPatterns');

class ConductorClient {
  constructor(apiKey, endUserId) {
    this.apiKey = apiKey || process.env.CONDUCTOR_API_KEY;
    this.endUserId = endUserId || process.env.CONDUCTOR_END_USER_ID;

    if (!this.apiKey || !this.endUserId) {
      throw new Error('Missing CONDUCTOR_API_KEY or CONDUCTOR_END_USER_ID');
    }
  }

  async request(endpoint, options = {}) {
    const url = `${API_BASE}${endpoint}`;
    const method = options.method || 'GET';
    const isWriteAction = ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method.toUpperCase());

    const response = await fetch(url, {
      method,
      headers: {
        'Authorization': `Bearer ${this.apiKey}`,
        'Conductor-End-User-Id': this.endUserId,
        'Content-Type': 'application/json',
        ...options.headers
      },
      body: options.body ? JSON.stringify(options.body) : undefined
    });

    const data = await response.json();

    // Log write actions
    if (isWriteAction) {
      const entity = this._extractEntity(endpoint);
      const action = this._methodToAction(method);

      logWriteAction({
        action,
        entity,
        endpoint: `${method} ${endpoint}`,
        request: options.body || null,
        response: response.ok ? data : { error: data },
        status: response.ok ? 'success' : 'error',
        refNumber: options.body?.refNumber || data?.refNumber || null,
        linkedEntities: this._extractLinkedEntities(options.body, data)
      });
    }

    if (!response.ok) {
      throw new Error(`API Error ${response.status}: ${JSON.stringify(data)}`);
    }

    return data;
  }

  _extractEntity(endpoint) {
    const match = endpoint.match(/\/quickbooks-desktop\/([^\/\?]+)/);
    return match ? match[1].replace(/-/g, '_') : 'unknown';
  }

  _methodToAction(method) {
    const map = { POST: 'create', PUT: 'update', PATCH: 'update', DELETE: 'delete' };
    return map[method.toUpperCase()] || method.toLowerCase();
  }

  _extractLinkedEntities(request, response) {
    const linked = [];

    // From request linkToTransactionIds
    if (request?.linkToTransactionIds) {
      request.linkToTransactionIds.forEach(id => {
        linked.push({ type: 'linked_transaction', id });
      });
    }

    // From response linkedTransactions
    if (response?.linkedTransactions) {
      response.linkedTransactions.forEach(txn => {
        linked.push({
          type: txn.transactionType || 'transaction',
          id: txn.id,
          refNumber: txn.refNumber
        });
      });
    }

    return linked;
  }

  // ============================================================
  // DATA PATTERN VALIDATION
  // ============================================================

  /**
   * Find vendor pattern file by vendor ID
   * Returns the pattern data if found, null otherwise
   */
  findVendorPattern(vendorId) {
    if (!fs.existsSync(DATA_PATTERNS_DIR)) {
      return null;
    }

    const files = fs.readdirSync(DATA_PATTERNS_DIR);
    const vendorFiles = files.filter(f => f.startsWith('vendor_') && f.endsWith('.json'));

    for (const file of vendorFiles) {
      try {
        const filePath = path.join(DATA_PATTERNS_DIR, file);
        const content = JSON.parse(fs.readFileSync(filePath, 'utf8'));
        if (content.vendor?.id === vendorId) {
          return { file, pattern: content };
        }
      } catch (e) {
        // Skip files that can't be parsed
        continue;
      }
    }

    return null;
  }

  /**
   * Check if a vendor pattern file exists for the given vendor ID
   */
  vendorPatternExists(vendorId) {
    return this.findVendorPattern(vendorId) !== null;
  }

  /**
   * Get all vendor patterns
   */
  getAllVendorPatterns() {
    if (!fs.existsSync(DATA_PATTERNS_DIR)) {
      return [];
    }

    const files = fs.readdirSync(DATA_PATTERNS_DIR);
    const vendorFiles = files.filter(f => f.startsWith('vendor_') && f.endsWith('.json'));
    const patterns = [];

    for (const file of vendorFiles) {
      try {
        const filePath = path.join(DATA_PATTERNS_DIR, file);
        const content = JSON.parse(fs.readFileSync(filePath, 'utf8'));
        patterns.push({ file, pattern: content });
      } catch (e) {
        continue;
      }
    }

    return patterns;
  }

  // ============================================================
  // READ OPERATIONS
  // ============================================================

  async getEndUser() {
    return this.request(`/end-users/${this.endUserId}`);
  }

  async getCustomers(params = {}) {
    const query = new URLSearchParams(params).toString();
    return this.request(`/quickbooks-desktop/customers${query ? '?' + query : ''}`);
  }

  async getCustomer(id) {
    return this.request(`/quickbooks-desktop/customers/${id}`);
  }

  async getVendors(params = {}) {
    const query = new URLSearchParams(params).toString();
    return this.request(`/quickbooks-desktop/vendors${query ? '?' + query : ''}`);
  }

  async getVendor(id) {
    return this.request(`/quickbooks-desktop/vendors/${id}`);
  }

  /**
   * Fetch all active vendors for LLM fuzzy matching
   * Returns simplified list: { id, name, balance }
   */
  async getVendorListForMatching() {
    const vendors = [];
    let cursor = null;
    let page = 0;
    const maxPages = 20;

    do {
      const params = { limit: 150, status: 'active' };
      if (cursor) params.cursor = cursor;

      const response = await this.getVendors(params);

      if (response.data) {
        response.data.forEach(v => {
          vendors.push({
            id: v.id,
            name: v.name,
            companyName: v.companyName,
            balance: v.balance
          });
        });
      }

      cursor = response.nextCursor;
      page++;
    } while (cursor && page < maxPages);

    return vendors;
  }

  async getAccounts(params = {}) {
    const query = new URLSearchParams(params).toString();
    return this.request(`/quickbooks-desktop/accounts${query ? '?' + query : ''}`);
  }

  async getInvoices(params = {}) {
    const query = new URLSearchParams(params).toString();
    return this.request(`/quickbooks-desktop/invoices${query ? '?' + query : ''}`);
  }

  async getBills(params = {}) {
    const query = new URLSearchParams(params).toString();
    return this.request(`/quickbooks-desktop/bills${query ? '?' + query : ''}`);
  }

  async getBill(id) {
    return this.request(`/quickbooks-desktop/bills/${id}`);
  }

  async findBillByRefNumber(refNumber, vendorId = null) {
    let cursor = null;
    let page = 0;
    const maxPages = 10;

    // Search recent bills (last 6 months) - sufficient for duplicate detection
    const sixMonthsAgo = new Date();
    sixMonthsAgo.setMonth(sixMonthsAgo.getMonth() - 6);
    const updatedAfter = sixMonthsAgo.toISOString().split('T')[0];

    do {
      const params = { limit: 150, updatedAfter };
      if (vendorId) params.vendorIds = vendorId;  // Filter by vendor for faster search
      if (cursor) params.cursor = cursor;

      const response = await this.getBills(params);

      const match = response.data?.find(bill => bill.refNumber === refNumber);
      if (match) return match;

      cursor = response.nextCursor;
      page++;
    } while (cursor && page < maxPages);

    return null;
  }

  async getPurchaseOrders(params = {}) {
    const query = new URLSearchParams(params).toString();
    return this.request(`/quickbooks-desktop/purchase-orders${query ? '?' + query : ''}`);
  }

  async getPurchaseOrder(id) {
    return this.request(`/quickbooks-desktop/purchase-orders/${id}`);
  }

  /**
   * Find active (open) POs for a vendor by PO number
   * Active = not fully received AND not manually closed
   */
  async findActivePO(vendorId, poNumber) {
    let cursor = null;
    let page = 0;
    const maxPages = 10;

    do {
      const params = { limit: 150 };
      if (vendorId) params.vendorIds = vendorId;
      if (cursor) params.cursor = cursor;

      const response = await this.getPurchaseOrders(params);

      const match = response.data?.find(po =>
        po.refNumber === poNumber &&
        !po.isFullyReceived &&
        !po.isManuallyClosed
      );

      if (match) return match;

      cursor = response.nextCursor;
      page++;
    } while (cursor && page < maxPages);

    return null;
  }

  /**
   * Get all active (open) POs for a vendor
   * Active = not fully received AND not manually closed
   */
  async getActivePOs(vendorId) {
    const activePOs = [];
    let cursor = null;
    let page = 0;
    const maxPages = 10;

    do {
      const params = { limit: 150 };
      if (vendorId) params.vendorIds = vendorId;
      if (cursor) params.cursor = cursor;

      const response = await this.getPurchaseOrders(params);

      const active = (response.data || []).filter(po =>
        !po.isFullyReceived && !po.isManuallyClosed
      );
      activePOs.push(...active);

      cursor = response.nextCursor;
      page++;
    } while (cursor && page < maxPages);

    return activePOs;
  }

  // ============================================================
  // WRITE OPERATIONS (Auto-logged)
  // ============================================================

  async createBill(billData, { skipDuplicateCheck = false, skipPatternCheck = false } = {}) {
    // Check vendor dataPattern exists (PREREQUISITE)
    if (!skipPatternCheck) {
      const vendorId = billData.vendorId || billData.vendor?.id;
      if (!vendorId) {
        const error = new Error('MISSING_VENDOR: billData must include vendorId or vendor.id');
        error.code = 'MISSING_VENDOR';
        throw error;
      }

      const patternResult = this.findVendorPattern(vendorId);
      if (!patternResult) {
        const error = new Error(
          `NO_VENDOR_PATTERN: No dataPattern file found for vendor ID ${vendorId}. ` +
          `Create a dataPatterns/vendor_*.json file first using _template_vendor.json as a guide.`
        );
        error.code = 'NO_VENDOR_PATTERN';
        error.vendorId = vendorId;
        throw error;
      }
    }

    // Check for duplicate bill by refNumber + vendorId
    if (!skipDuplicateCheck && billData.refNumber) {
      const vendorId = billData.vendorId || billData.vendor?.id;
      const existing = await this.findBillByRefNumber(billData.refNumber, vendorId);
      if (existing) {
        const error = new Error(
          `DUPLICATE_BILL: Bill #${billData.refNumber} already exists for vendor ${existing.vendor?.fullName || vendorId}. ` +
          `Existing bill ID: ${existing.id}, Date: ${existing.transactionDate}, Amount: $${existing.amountDue}`
        );
        error.code = 'DUPLICATE_BILL';
        error.existingBill = existing;
        throw error;
      }
    }

    return this.request('/quickbooks-desktop/bills', {
      method: 'POST',
      body: billData
    });
  }

  async updateBill(id, billData) {
    return this.request(`/quickbooks-desktop/bills/${id}`, {
      method: 'POST',  // Conductor uses POST for updates
      body: billData
    });
  }

  async createInvoice(invoiceData) {
    return this.request('/quickbooks-desktop/invoices', {
      method: 'POST',
      body: invoiceData
    });
  }

  async createPurchaseOrder(poData) {
    return this.request('/quickbooks-desktop/purchase-orders', {
      method: 'POST',
      body: poData
    });
  }

  async createVendor(vendorData) {
    return this.request('/quickbooks-desktop/vendors', {
      method: 'POST',
      body: vendorData
    });
  }

  async getCreditCardCharges(params = {}) {
    const query = new URLSearchParams(params).toString();
    return this.request(`/quickbooks-desktop/credit-card-charges${query ? '?' + query : ''}`);
  }

  async createCreditCardCharge(chargeData) {
    return this.request('/quickbooks-desktop/credit-card-charges', {
      method: 'POST',
      body: chargeData
    });
  }
}

module.exports = ConductorClient;
