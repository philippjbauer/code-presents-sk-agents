# MCP Client Configuration Examples

This document provides configuration examples for various MCP clients to use the Actual Budget MCP Server.

## Claude Desktop

### macOS

Edit `~/Library/Application Support/Claude/claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "actual-budget": {
      "command": "node",
      "args": [
        "/Users/YOUR_USERNAME/path/to/actual-mcp/dist/index.js"
      ],
      "env": {
        "ACTUAL_SERVER_URL": "http://localhost:5006",
        "ACTUAL_SERVER_PASSWORD": "your-server-password",
        "ACTUAL_BUDGET_ID": "your-budget-sync-id",
        "ACTUAL_DATA_DIR": "/Users/YOUR_USERNAME/path/to/actual-mcp/data"
      }
    }
  }
}
```

### Windows

Edit `%APPDATA%\Claude\claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "actual-budget": {
      "command": "node",
      "args": [
        "C:\\Users\\YOUR_USERNAME\\path\\to\\actual-mcp\\dist\\index.js"
      ],
      "env": {
        "ACTUAL_SERVER_URL": "http://localhost:5006",
        "ACTUAL_SERVER_PASSWORD": "your-server-password",
        "ACTUAL_BUDGET_ID": "your-budget-sync-id",
        "ACTUAL_DATA_DIR": "C:\\Users\\YOUR_USERNAME\\path\\to\\actual-mcp\\data"
      }
    }
  }
}
```

### Linux

Edit `~/.config/Claude/claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "actual-budget": {
      "command": "node",
      "args": [
        "/home/YOUR_USERNAME/path/to/actual-mcp/dist/index.js"
      ],
      "env": {
        "ACTUAL_SERVER_URL": "http://localhost:5006",
        "ACTUAL_SERVER_PASSWORD": "your-server-password",
        "ACTUAL_BUDGET_ID": "your-budget-sync-id",
        "ACTUAL_DATA_DIR": "/home/YOUR_USERNAME/path/to/actual-mcp/data"
      }
    }
  }
}
```

### With Encrypted Budget

If your budget uses end-to-end encryption:

```json
{
  "mcpServers": {
    "actual-budget": {
      "command": "node",
      "args": [
        "/path/to/actual-mcp/dist/index.js"
      ],
      "env": {
        "ACTUAL_SERVER_URL": "http://localhost:5006",
        "ACTUAL_SERVER_PASSWORD": "your-server-password",
        "ACTUAL_BUDGET_ID": "your-budget-sync-id",
        "ACTUAL_BUDGET_PASSWORD": "your-encryption-password",
        "ACTUAL_DATA_DIR": "/path/to/actual-mcp/data"
      }
    }
  }
}
```

## VS Code with GitHub Copilot Chat

Add to your workspace settings (`.vscode/settings.json`):

```json
{
  "mcp.servers": {
    "actual-budget": {
      "command": "node",
      "args": ["${workspaceFolder}/../actual-mcp/dist/index.js"],
      "env": {
        "ACTUAL_SERVER_URL": "http://localhost:5006",
        "ACTUAL_SERVER_PASSWORD": "your-server-password",
        "ACTUAL_BUDGET_ID": "your-budget-sync-id"
      }
    }
  }
}
```

## Continue.dev

Add to `~/.continue/config.json`:

```json
{
  "mcpServers": [
    {
      "name": "actual-budget",
      "command": "node",
      "args": ["/path/to/actual-mcp/dist/index.js"],
      "env": {
        "ACTUAL_SERVER_URL": "http://localhost:5006",
        "ACTUAL_SERVER_PASSWORD": "your-server-password",
        "ACTUAL_BUDGET_ID": "your-budget-sync-id"
      }
    }
  ]
}
```

## Cody (Sourcegraph)

Add to your Cody settings:

```json
{
  "cody.experimental.mcp.servers": {
    "actual-budget": {
      "command": "node",
      "args": ["/path/to/actual-mcp/dist/index.js"],
      "env": {
        "ACTUAL_SERVER_URL": "http://localhost:5006",
        "ACTUAL_SERVER_PASSWORD": "your-server-password",
        "ACTUAL_BUDGET_ID": "your-budget-sync-id"
      }
    }
  }
}
```

## Custom MCP Client (Node.js)

```javascript
import { Client } from '@modelcontextprotocol/sdk/client/index.js';
import { StdioClientTransport } from '@modelcontextprotocol/sdk/client/stdio.js';

const transport = new StdioClientTransport({
  command: 'node',
  args: ['/path/to/actual-mcp/dist/index.js'],
  env: {
    ACTUAL_SERVER_URL: 'http://localhost:5006',
    ACTUAL_SERVER_PASSWORD: 'your-server-password',
    ACTUAL_BUDGET_ID: 'your-budget-sync-id',
    ACTUAL_DATA_DIR: './data'
  }
});

const client = new Client({
  name: 'actual-budget-client',
  version: '1.0.0'
});

await client.connect(transport);

// Use the client
const accounts = await client.callTool({
  name: 'get-accounts',
  arguments: {}
});

console.log(accounts);
```

## Custom MCP Client (Python)

```python
import asyncio
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

server_params = StdioServerParameters(
    command="node",
    args=["/path/to/actual-mcp/dist/index.js"],
    env={
        "ACTUAL_SERVER_URL": "http://localhost:5006",
        "ACTUAL_SERVER_PASSWORD": "your-server-password",
        "ACTUAL_BUDGET_ID": "your-budget-sync-id",
        "ACTUAL_DATA_DIR": "./data"
    }
)

async def main():
    async with stdio_client(server_params) as (read, write):
        async with ClientSession(read, write) as session:
            await session.initialize()
            
            # Call tools
            result = await session.call_tool("get-accounts", {})
            print(result)

asyncio.run(main())
```

## Docker Compose

Create a `docker-compose.yml`:

```yaml
version: '3.8'

services:
  actual-mcp:
    build: .
    environment:
      - ACTUAL_SERVER_URL=http://actual-server:5006
      - ACTUAL_SERVER_PASSWORD=${ACTUAL_SERVER_PASSWORD}
      - ACTUAL_BUDGET_ID=${ACTUAL_BUDGET_ID}
      - ACTUAL_DATA_DIR=/data
    volumes:
      - ./data:/data
    stdin_open: true
    tty: true

  actual-server:
    image: actualbudget/actual-server:latest
    ports:
      - "5006:5006"
    volumes:
      - actual-data:/data

volumes:
  actual-data:
```

With `Dockerfile`:

```dockerfile
FROM node:18-alpine

WORKDIR /app

COPY package*.json ./
RUN npm ci --only=production

COPY dist ./dist

CMD ["node", "dist/index.js"]
```

## Environment Variables Reference

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `ACTUAL_SERVER_URL` | Yes | - | URL of your Actual Budget server |
| `ACTUAL_SERVER_PASSWORD` | Yes | - | Server login password |
| `ACTUAL_BUDGET_ID` | Yes | - | Budget Sync ID from Settings |
| `ACTUAL_BUDGET_PASSWORD` | No | - | E2E encryption password if enabled |
| `ACTUAL_DATA_DIR` | No | `./data` | Local cache directory |
| `PORT` | No | `3000` | HTTP server port (if using HTTP transport) |

## Troubleshooting

### Connection Refused

If you get connection errors:

1. Verify Actual Budget server is running
2. Check `ACTUAL_SERVER_URL` is correct
3. Test with curl: `curl http://localhost:5006`

### Budget Not Found

If budget can't be loaded:

1. Verify `ACTUAL_BUDGET_ID` is correct
2. Check it's the Sync ID, not the budget name
3. Try logging into the web UI to confirm the ID

### Permission Denied

If you get permission errors:

1. Check file permissions on the MCP server directory
2. Ensure `ACTUAL_DATA_DIR` is writable
3. Verify Node.js has execute permissions

### Tools Not Appearing

If tools don't show up in your MCP client:

1. Restart the MCP client
2. Check the server logs for errors
3. Run `health` tool to verify connection
4. Check MCP client configuration is correct

## Security Best Practices

### 1. Use Environment Variables

Never hardcode passwords in configuration files:

```bash
# .env file
ACTUAL_SERVER_PASSWORD=my-secret-password
ACTUAL_BUDGET_PASSWORD=my-encryption-key
```

Load with:
```json
{
  "env": {
    "ACTUAL_SERVER_PASSWORD": "${ACTUAL_SERVER_PASSWORD}",
    "ACTUAL_BUDGET_PASSWORD": "${ACTUAL_BUDGET_PASSWORD}"
  }
}
```

### 2. Restrict File Permissions

```bash
chmod 600 ~/.config/Claude/claude_desktop_config.json
chmod 700 /path/to/actual-mcp/data
```

### 3. Use HTTPS

If accessing Actual server over network:
```json
{
  "ACTUAL_SERVER_URL": "https://actual.example.com"
}
```

### 4. Network Isolation

For production, consider:
- VPN or SSH tunnel to Actual server
- Firewall rules limiting access
- Dedicated user account for MCP server

## Testing Configuration

After setting up, test with:

1. **Health Check**
   ```bash
   # Run the server manually
   node dist/index.js
   
   # In another terminal, test stdio
   echo '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"health","arguments":{}}}' | node dist/index.js
   ```

2. **MCP Client Test**
   - Open Claude Desktop
   - Ask: "Can you check my Actual Budget connection?"
   - Should see the health tool being called

3. **Full Workflow Test**
   - Ask: "Show me my budget accounts"
   - Should list all accounts
   - Ask: "Get uncategorized transactions from last month"
   - Should show transactions

## Migration Notes

### From Previous Versions

If you're updating from an older version:

1. Rebuild the project: `npm run build`
2. Update configuration paths to `dist/index.js`
3. Check for new environment variables
4. Restart your MCP client

### Changing Budget

To switch to a different budget:

1. Get new Sync ID from Actual Budget
2. Update `ACTUAL_BUDGET_ID` in configuration
3. Restart MCP client
4. Old cached data will be automatically replaced

## Support

For configuration issues:
- Check the main [README.md](README.md)
- Review [USAGE.md](USAGE.md) for workflow examples
- Check Actual Budget documentation
- Review MCP client-specific documentation
