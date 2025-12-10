# Actual Budget MCP Server - Usage Guide

## Quick Start Workflow

This guide demonstrates how an AI agent can use the MCP server to automatically review and categorize transactions.

## Scenario: Automatic Transaction Categorization

Let's say you have uncategorized transactions that need to be reviewed and properly categorized based on their notes.

### Step 1: Check Server Health

First, verify the connection is working:

```json
{
  "tool": "health",
  "arguments": {}
}
```

Expected response:
```json
{
  "status": "healthy",
  "message": "Connected to Actual Budget",
  "initialized": true
}
```

### Step 2: Get Available Accounts

Find out what accounts exist:

```json
{
  "tool": "get-accounts",
  "arguments": {}
}
```

This returns a list of accounts with their IDs.

### Step 3: Get Uncategorized Transactions

Retrieve transactions that need categorization:

```json
{
  "tool": "get-uncategorized-transactions",
  "arguments": {
    "accountId": "abc123...",
    "startDate": "2024-11-01",
    "endDate": "2024-11-30"
  }
}
```

Example response:
```json
{
  "transactions": [
    {
      "id": "tx-001",
      "date": "2024-11-15",
      "amount": -4500,
      "notes": "Coffee at Starbucks Main Street",
      "imported_payee": "STARBUCKS #12345"
    },
    {
      "id": "tx-002",
      "date": "2024-11-16",
      "amount": -12000,
      "notes": "Groceries - Whole Foods",
      "imported_payee": "WHOLE FOODS MKT"
    }
  ],
  "count": 2
}
```

### Step 4: Get Existing Payees and Categories

Before creating new entities, check what already exists:

```json
{
  "tool": "get-payees",
  "arguments": {}
}
```

```json
{
  "tool": "get-categories",
  "arguments": {}
}
```

### Step 5: Search for Matching Payees

For the first transaction with notes "Coffee at Starbucks Main Street":

```json
{
  "tool": "find-matching-payee",
  "arguments": {
    "searchTerm": "Starbucks",
    "exactMatch": false
  }
}
```

If a match is found, use that payee ID. Otherwise, create a new one.

### Step 6: Get or Create Payee

Use the convenience tool to find or create:

```json
{
  "tool": "get-or-create-payee",
  "arguments": {
    "name": "Starbucks",
    "category": "category-id-for-coffee"
  }
}
```

Response:
```json
{
  "payeeId": "payee-123",
  "name": "Starbucks",
  "created": false
}
```

### Step 7: Get or Create Category

Similarly for categories:

```json
{
  "tool": "find-matching-category",
  "arguments": {
    "searchTerm": "Coffee",
    "exactMatch": false
  }
}
```

Or use the convenience tool:

```json
{
  "tool": "get-or-create-category",
  "arguments": {
    "name": "Coffee & Tea",
    "groupId": "group-dining-out",
    "isIncome": false
  }
}
```

### Step 8: Update the Transaction

Now assign the payee and category:

```json
{
  "tool": "update-transaction",
  "arguments": {
    "transactionId": "tx-001",
    "payee": "payee-123",
    "category": "category-456",
    "notes": "Coffee at Starbucks Main Street - Auto-categorized"
  }
}
```

### Step 9: Bulk Update Similar Transactions

If you have multiple transactions with the same payee:

```json
{
  "tool": "bulk-update-transactions",
  "arguments": {
    "transactionIds": ["tx-003", "tx-004", "tx-005"],
    "payee": "payee-123",
    "category": "category-456"
  }
}
```

## AI Agent Decision Logic

Here's the recommended logic for an AI agent:

### 1. Parse Transaction Notes

Extract hints from the `notes` field:
- Payee name (e.g., "Starbucks", "Whole Foods")
- Category hints (e.g., "groceries", "coffee", "gas")
- Additional context

### 2. Match Payees

```
For each transaction:
  1. Extract probable payee name from notes or imported_payee
  2. Clean up the name (remove transaction codes, locations)
  3. Search for existing payees using find-matching-payee
  4. If found with high confidence: use existing
  5. If not found or low confidence: create new payee
```

### 3. Match Categories

```
For each transaction:
  1. Identify category from notes or payee's default category
  2. Search for existing categories using find-matching-category
  3. Consider category groups for context
  4. If found: use existing
  5. If not found: ask user or create with sensible defaults
```

### 4. Update Transaction

```
Once payee and category are determined:
  1. Use update-transaction to assign them
  2. Optionally update notes to indicate auto-categorization
  3. Mark transaction as reviewed
```

## Common Patterns

### Pattern 1: Coffee Shop Transactions

```
Notes: "Coffee at Starbucks"
Payee: Search "Starbucks" → Create if not found
Category: Search "Coffee" → Use "Dining:Coffee & Tea"
```

### Pattern 2: Grocery Store

```
Notes: "Groceries - Whole Foods"
Payee: Search "Whole Foods" → Create if not found
Category: Search "Groceries" → Use "Food:Groceries"
```

### Pattern 3: Gas Station

```
Notes: "Gas - Shell Station"
Payee: Search "Shell" → Create if not found
Category: Search "Gas" → Use "Transportation:Gas"
```

### Pattern 4: Online Shopping

```
Notes: "Amazon - Books"
Payee: Search "Amazon" → Create if not found
Category: Parse "Books" → Use "Shopping:Books & Media"
```

## Best Practices

1. **Always Check Existing First**: Use find-matching-* tools before creating
2. **Fuzzy Matching**: Use fuzzy matching for better results (exactMatch=false)
3. **Batch Similar**: Group similar transactions for bulk updates
4. **Learn from History**: If a payee has a default category, use it
5. **Preserve Original**: Keep imported_payee intact for reference
6. **Add Context to Notes**: Include why categorization was chosen

## Error Handling

### Common Errors

1. **Account Not Found**: Verify account ID with get-accounts
2. **Invalid Date Format**: Use YYYY-MM-DD format
3. **Category Group Required**: When creating categories, group_id is mandatory
4. **Transaction Already Categorized**: Check if update is still needed

### Recovery Steps

```json
{
  "tool": "health",
  "arguments": {}
}
```

Check health status and reinitialize if needed.

## Performance Tips

1. **Cache Payees and Categories**: Fetch once, use throughout session
2. **Batch Updates**: Use bulk-update-transactions when possible
3. **Date Ranges**: Limit transaction queries to reasonable date ranges
4. **Parallel Processing**: Process multiple accounts concurrently

## Example: Complete Workflow Script

```python
# Pseudo-code for AI agent workflow

async def categorize_transactions(account_id, start_date, end_date):
    # 1. Get uncategorized transactions
    transactions = await call_tool("get-uncategorized-transactions", {
        "accountId": account_id,
        "startDate": start_date,
        "endDate": end_date
    })
    
    # 2. Cache existing data
    payees = await call_tool("get-payees", {})
    categories = await call_tool("get-categories", {})
    
    # 3. Process each transaction
    for tx in transactions["transactions"]:
        # Extract hints
        payee_hint = extract_payee_name(tx["notes"], tx["imported_payee"])
        category_hint = extract_category_hint(tx["notes"], tx["amount"])
        
        # Find or create payee
        payee_id = await find_or_create_payee(payee_hint, payees)
        
        # Find or create category
        category_id = await find_or_create_category(category_hint, categories)
        
        # Update transaction
        await call_tool("update-transaction", {
            "transactionId": tx["id"],
            "payee": payee_id,
            "category": category_id,
            "notes": tx["notes"] + " [Auto-categorized]"
        })
        
    return f"Categorized {len(transactions)} transactions"
```

## Integration Examples

### With Claude Desktop

Claude can use these tools conversationally:

```
User: "Review my uncategorized transactions for November"

Claude: Let me check your uncategorized transactions...
[Uses get-accounts, get-uncategorized-transactions]

Claude: I found 15 uncategorized transactions. Here are some examples:
- $45.00 at Starbucks on Nov 15
- $120.00 at Whole Foods on Nov 16

Would you like me to categorize them automatically?

User: "Yes, please"

Claude: [Uses find-matching-payee, get-or-create-payee, 
         find-matching-category, update-transaction]

Claude: Done! I've categorized all 15 transactions:
- 5 transactions at Starbucks → Coffee & Tea category
- 3 transactions at Whole Foods → Groceries category
- ...
```

### With Custom Agent

Build a background agent that runs periodically:

```javascript
setInterval(async () => {
  const accounts = await getAccounts();
  
  for (const account of accounts) {
    await categorizeTransactions(
      account.id,
      getLastWeekStart(),
      getToday()
    );
  }
}, 24 * 60 * 60 * 1000); // Daily
```

## Advanced Usage

### Custom Categorization Rules

Implement custom logic based on:
- Transaction amounts (e.g., > $1000 → Major Purchase)
- Specific payees (e.g., IRS → Taxes)
- Date patterns (e.g., 1st of month → Rent)
- Note keywords (e.g., "gift" → Gifts)

### Learning from History

Use transaction history to improve categorization:
1. Get historical transactions for a payee
2. Analyze most common category used
3. Apply that category to new transactions

### Multi-step Verification

Before updating:
1. Calculate confidence score
2. If high confidence: auto-update
3. If low confidence: flag for manual review
4. Store decision rationale in notes

## Conclusion

This MCP server provides all the tools needed for intelligent transaction management. By combining these tools with AI reasoning, you can build sophisticated automation that learns from patterns and makes smart categorization decisions.
