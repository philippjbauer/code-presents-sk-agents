# Actual Budget MCP Server - Quick Reference

## Installation

```bash
npm install
npm run build
cp .env.example .env
# Edit .env with your configuration
npm test
```

## Available Tools

### 🏥 Health & Status
- **health** - Check server connection status

### 💳 Transactions
- **get-transactions** - Get transactions for account/date range
- **get-uncategorized-transactions** - Find transactions without categories
- **update-transaction** - Update single transaction
- **bulk-update-transactions** - Update multiple transactions
- **smart-categorize-transaction** - AI-powered categorization

### 👤 Payees
- **get-payees** - List all payees
- **find-matching-payee** - Search payees (fuzzy)
- **create-payee** - Create new payee
- **update-payee** - Update payee details
- **get-or-create-payee** - ⭐ Find or create (recommended)

### 📁 Categories
- **get-categories** - List all categories
- **get-category-groups** - List category groups
- **find-matching-category** - Search categories (fuzzy)
- **create-category** - Create new category
- **get-or-create-category** - ⭐ Find or create (recommended)

### 🏦 Accounts
- **get-accounts** - List all accounts
- **get-account-balance** - Get account balance
- **find-account-by-name** - Search accounts by name

## Common Workflows

### 1. Review Uncategorized Transactions

```
1. get-accounts
2. get-uncategorized-transactions (with accountId)
3. For each transaction:
   - get-or-create-payee
   - get-or-create-category
   - update-transaction
```

### 2. Categorize Based on Notes

```
1. Parse transaction.notes for hints
2. find-matching-payee (fuzzy search)
3. find-matching-category (fuzzy search)
4. update-transaction with matched IDs
```

### 3. Batch Update Similar Transactions

```
1. get-transactions (find similar ones)
2. get-or-create-payee (once)
3. get-or-create-category (once)
4. bulk-update-transactions (all at once)
```

## Tool Arguments

### Date Format
```json
{
  "startDate": "2024-11-01",
  "endDate": "2024-11-30"
}
```

### Search Options
```json
{
  "searchTerm": "Starbucks",
  "exactMatch": false  // true for exact, false for fuzzy
}
```

### Transaction Update
```json
{
  "transactionId": "tx-123",
  "payee": "payee-id",
  "category": "category-id",
  "notes": "Updated notes",
  "cleared": true
}
```

### Create Payee
```json
{
  "name": "Starbucks",
  "category": "category-id"  // optional default
}
```

### Create Category
```json
{
  "name": "Coffee & Tea",
  "groupId": "group-id",  // required
  "isIncome": false
}
```

## Response Structure

### Success Response
```json
{
  "content": [
    { "type": "text", "text": "..." }
  ],
  "structuredContent": {
    // Typed data here
  }
}
```

### Transaction Object
```json
{
  "id": "tx-123",
  "account": "acct-456",
  "date": "2024-11-15",
  "amount": -4500,  // $45.00 (negative = expense)
  "payee_id": "payee-789",
  "category": "cat-012",
  "notes": "Coffee at Starbucks",
  "imported_payee": "STARBUCKS #12345",
  "cleared": true
}
```

## Amount Format

Amounts are integers (cents):
- `$45.00` → `4500`
- `$120.30` → `12030`
- `-$25.50` → `-2550` (negative = expense)

## Environment Variables

### Required
```env
ACTUAL_SERVER_URL=http://localhost:5006
ACTUAL_SERVER_PASSWORD=your-password
ACTUAL_BUDGET_ID=your-sync-id
```

### Optional
```env
ACTUAL_BUDGET_PASSWORD=encryption-password
ACTUAL_DATA_DIR=./data
PORT=3000
```

## Error Handling

### Common Errors

1. **"Not initialized"**
   - Run health tool first
   - Check environment variables

2. **"Budget not found"**
   - Verify ACTUAL_BUDGET_ID
   - Check server is running

3. **"Invalid date format"**
   - Use YYYY-MM-DD format
   - Check date is valid

4. **"Category group required"**
   - Must specify groupId when creating categories
   - Use get-category-groups first

## Best Practices

### ✅ Do
- Use get-or-create-* tools for safety
- Use fuzzy matching for searches
- Batch similar transactions
- Cache payees/categories
- Preserve original imported_payee

### ❌ Don't
- Don't hardcode IDs
- Don't skip health check on startup
- Don't create duplicates without searching
- Don't use exact match unless necessary
- Don't modify transfer transactions

## Claude Desktop Example

Ask Claude:
```
"Show me my uncategorized transactions from last month 
and help me categorize them"
```

Claude will:
1. Call get-accounts
2. Call get-uncategorized-transactions
3. For each transaction:
   - Analyze notes
   - Search for matching payee
   - Search for matching category
   - Update transaction

## Troubleshooting

### Server Won't Start
```bash
# Check environment
npm test

# Check logs
npm start 2>&1 | tee server.log

# Verify Actual server
curl http://localhost:5006
```

### Tools Not Working
```bash
# Test health
echo '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"health","arguments":{}}}' | npm start

# Check MCP client config
cat ~/.config/Claude/claude_desktop_config.json
```

### Connection Issues
```bash
# Test server connection
curl -X POST http://localhost:5006/account/login \
  -H "Content-Type: application/json" \
  -d '{"password":"your-password"}'
```

## URLs & Resources

- **Documentation**: See README.md
- **Workflows**: See USAGE.md
- **Client Config**: See MCP_CLIENT_CONFIG.md
- **Actual Budget**: https://actualbudget.org/docs/api/
- **MCP Protocol**: https://modelcontextprotocol.io

## Version

Current: v1.0.0
API: @actual-app/api v25.11.0
MCP SDK: @modelcontextprotocol/sdk v1.0.4

---

**Need Help?**
1. Check README.md for details
2. Run npm test to verify installation
3. Review error messages in console
4. Check Actual Budget server logs
