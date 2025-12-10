# Actual Budget MCP Server

A Model Context Protocol (MCP) server that provides AI agents with access to the Actual Budget API. This server enables intelligent transaction review, automatic categorization, and budget management through conversational AI interfaces.

## Features

- **Transaction Management**: Get, update, and bulk-update transactions
- **Smart Categorization**: Tools for AI-powered transaction categorization based on notes and patterns
- **Payee Management**: Search, create, and manage payees with fuzzy matching
- **Category Management**: Search, create, and manage categories and category groups
- **Account Operations**: View accounts and check balances
- **Intelligent Matching**: Find or create payees and categories automatically

## Use Cases

This MCP server is designed for AI agents to:

1. **Review Uncategorized Transactions**: Automatically find transactions that need categorization
2. **Smart Categorization**: Parse transaction notes to identify appropriate payees and categories
3. **Match or Create Entities**: Intelligently match existing payees/categories or create new ones
4. **Batch Operations**: Update multiple transactions at once
5. **Budget Analysis**: Query account balances and transaction patterns

## Prerequisites

- Node.js 18.0.0 or higher
- An Actual Budget server instance
- Your budget's Sync ID (found in Settings → Show advanced settings → Sync ID)

## Installation

1. Clone this repository or download the source code

2. Install dependencies:
   ```bash
   npm install
   ```

3. Create a `.env` file based on `.env.example`:
   ```bash
   cp .env.example .env
   ```

4. Configure your `.env` file:
   ```env
   ACTUAL_SERVER_URL=http://localhost:5006
   ACTUAL_SERVER_PASSWORD=your-server-password
   ACTUAL_BUDGET_ID=your-budget-sync-id
   ACTUAL_BUDGET_PASSWORD=your-encryption-password-if-enabled
   ACTUAL_DATA_DIR=./data
   ```

## Building

Compile the TypeScript source:

```bash
npm run build
```

## Running

### Standalone Mode

Run directly with stdio transport (for use with MCP clients):

```bash
npm start
```

### Development Mode

Build and run in one command:

```bash
npm run dev
```

## Configuration

### Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `ACTUAL_SERVER_URL` | Yes | URL of your Actual Budget server |
| `ACTUAL_SERVER_PASSWORD` | Yes | Server login password |
| `ACTUAL_BUDGET_ID` | Yes | Sync ID of your budget |
| `ACTUAL_BUDGET_PASSWORD` | No | Encryption password if E2EE is enabled |
| `ACTUAL_DATA_DIR` | No | Directory for local cache (default: `./data`) |

### Finding Your Budget Sync ID

1. Open Actual Budget in your browser
2. Go to Settings
3. Click "Show advanced settings"
4. Copy the "Sync ID"

## Available Tools

### Health Check

- **`health`**: Check server and Actual Budget connection status

### Transactions

- **`get-transactions`**: Get transactions for an account within a date range
- **`get-uncategorized-transactions`**: Find all transactions without categories
- **`update-transaction`**: Update transaction fields (payee, category, notes)
- **`bulk-update-transactions`**: Update multiple transactions at once
- **`smart-categorize-transaction`**: AI-powered categorization based on notes

### Payees

- **`get-payees`**: List all payees
- **`find-matching-payee`**: Search payees with fuzzy matching
- **`create-payee`**: Create a new payee
- **`update-payee`**: Update payee details
- **`get-or-create-payee`**: Find existing or create new payee (recommended)

### Categories

- **`get-categories`**: List all categories
- **`get-category-groups`**: List category groups
- **`find-matching-category`**: Search categories with fuzzy matching
- **`create-category`**: Create a new category
- **`get-or-create-category`**: Find existing or create new category (recommended)

### Accounts

- **`get-accounts`**: List all accounts
- **`get-account-balance`**: Get account balance (optionally as of a date)
- **`find-account-by-name`**: Search for accounts by name

## Usage Example Workflow

Here's how an AI agent might use these tools to automatically categorize transactions:

1. **Get accounts**: `get-accounts` to find available accounts
2. **Get uncategorized**: `get-uncategorized-transactions` for a specific account
3. **For each transaction**:
   - Parse the `notes` field to extract payee and category hints
   - Use `find-matching-payee` to search for existing payees
   - If not found, use `create-payee` or `get-or-create-payee`
   - Use `find-matching-category` to search for categories
   - If not found, use `create-category` or `get-or-create-category`
   - Use `update-transaction` to assign the payee and category
4. **Batch updates**: Use `bulk-update-transactions` for multiple similar transactions

## Integration with AI Assistants

### Claude Desktop

Add to your Claude Desktop configuration (`~/Library/Application Support/Claude/claude_desktop_config.json` on macOS):

```json
{
  "mcpServers": {
    "actual-budget": {
      "command": "node",
      "args": ["/path/to/actual-mcp/dist/index.js"],
      "env": {
        "ACTUAL_SERVER_URL": "http://localhost:5006",
        "ACTUAL_SERVER_PASSWORD": "your-password",
        "ACTUAL_BUDGET_ID": "your-budget-id"
      }
    }
  }
}
```

### Other MCP Clients

Any MCP-compatible client can use this server by spawning it as a subprocess with stdio transport.

## Data Format Notes

### Amounts

Actual Budget stores amounts as integers without decimal places. For example:
- `$120.30` is stored as `12030`
- Use `utils.amountToInteger()` and `utils.integerToAmount()` for conversions

### Dates

Dates are in ISO format:
- Transactions: `YYYY-MM-DD` (e.g., `2024-03-15`)
- Months: `YYYY-MM` (e.g., `2024-03`)

### Transaction Fields

- `payee_id`: Reference to payee (use in get requests)
- `payee`: Payee ID (use in create/update requests)
- `payee_name`: Auto-creates/matches payee (only in create requests)
- `category`: Category ID
- `notes`: Free-form text field for notes
- `imported_payee`: Original payee name from import

## Development

### Project Structure

```
actual-mcp/
├── src/
│   ├── index.ts           # Main server entry point
│   └── tools/
│       ├── transactions.ts # Transaction tools
│       ├── payees.ts      # Payee tools
│       ├── categories.ts  # Category tools
│       └── accounts.ts    # Account tools
├── package.json
├── tsconfig.json
└── README.md
```

### Adding New Tools

1. Create or edit a tool file in `src/tools/`
2. Register the tool using `server.registerTool()`
3. Import and call the registration function in `src/index.ts`

### TypeScript Configuration

The project uses strict TypeScript settings. All code is compiled to ES2022 modules.

## Troubleshooting

### Connection Issues

- Verify your Actual Budget server is running
- Check that `ACTUAL_SERVER_URL` is correct
- Ensure `ACTUAL_SERVER_PASSWORD` matches your server password

### Budget Not Loading

- Verify `ACTUAL_BUDGET_ID` is correct (get from Settings → Advanced)
- If using encryption, ensure `ACTUAL_BUDGET_PASSWORD` is set
- Check the `data` directory has write permissions

### Tool Errors

- Run `health` tool to check connection status
- Check console output for detailed error messages
- Ensure Actual Budget API version is compatible (requires `@actual-app/api` ^7.0.0)

## API Reference

For detailed information about the Actual Budget API:
- [Official API Documentation](https://actualbudget.org/docs/api/)
- [API Reference](https://actualbudget.org/docs/api/reference)

## License

This project uses the Actual Budget API which is open source. Please refer to the Actual Budget project for licensing information.

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## Support

For issues related to:
- **This MCP server**: Open an issue in this repository
- **Actual Budget API**: Visit [Actual Budget GitHub](https://github.com/actualbudget/actual)
- **Model Context Protocol**: Visit [MCP Documentation](https://modelcontextprotocol.io)

## Acknowledgments

- [Actual Budget](https://actualbudget.org/) - The fantastic open-source budgeting app
- [Model Context Protocol](https://modelcontextprotocol.io) - The standard for AI-application integration
- [Anthropic](https://www.anthropic.com) - For Claude and MCP

## Version History

### 1.0.0 (Initial Release)
- Complete transaction management tools
- Payee search and creation
- Category management
- Account operations
- Fuzzy matching for payees and categories
- Batch transaction updates
