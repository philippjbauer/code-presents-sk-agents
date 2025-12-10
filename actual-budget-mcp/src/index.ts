#!/usr/bin/env node
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";
import * as actual from "@actual-app/api";
import { registerTransactionTools } from "./tools/transactions.js";
import { registerPayeeTools } from "./tools/payees.js";
import { registerCategoryTools } from "./tools/categories.js";
import { registerAccountTools } from "./tools/accounts.js";

// Configuration from environment variables
const config = {
    serverURL: process.env.ACTUAL_SERVER_URL || "http://localhost:5006",
    password: process.env.ACTUAL_SERVER_PASSWORD || "",
    budgetId: process.env.ACTUAL_BUDGET_ID || "",
    budgetPassword: process.env.ACTUAL_BUDGET_PASSWORD,
    dataDir: process.env.ACTUAL_DATA_DIR || "./data",
};

// Validate required configuration
if (!config.password || !config.budgetId) {
    console.error("Error: Missing required environment variables");
    console.error("Please set: ACTUAL_SERVER_PASSWORD and ACTUAL_BUDGET_ID");
    process.exit(1);
}

// Initialize MCP Server
const server = new McpServer({
    name: "actual-budget-server",
    version: "1.0.0",
});

// State management
let isInitialized = false;
let initializationError: Error | null = null;

/**
 * Initialize connection to Actual Budget
 */
async function initializeActual(): Promise<void> {
    if (isInitialized) {
        return;
    }

    try {
        console.error("Initializing Actual Budget API...");

        await actual.init({
            dataDir: config.dataDir,
            serverURL: config.serverURL,
            password: config.password,
        });

        console.error("Downloading budget...");

        if (config.budgetPassword) {
            await actual.downloadBudget(config.budgetId, {
                password: config.budgetPassword,
            });
        } else {
            await actual.downloadBudget(config.budgetId);
        }

        isInitialized = true;
        console.error("Actual Budget API initialized successfully");
    } catch (error) {
        initializationError = error as Error;
        console.error("Failed to initialize Actual Budget API:", error);
        throw error;
    }
}

/**
 * Ensure Actual is initialized before tool execution
 */
async function ensureInitialized(): Promise<void> {
    if (initializationError) {
        throw new Error(
            `Actual Budget initialization failed: ${initializationError.message}`
        );
    }

    if (!isInitialized) {
        await initializeActual();
    }
}

/**
 * Health check tool
 */
server.registerTool(
    "health",
    {
        title: "Health Check",
        description:
            "Check if the MCP server and Actual Budget connection are working",
        inputSchema: {},
        outputSchema: {
            status: z.enum(["healthy", "unhealthy"]),
            message: z.string(),
            initialized: z.boolean(),
        },
    },
    async () => {
        await ensureInitialized();

        const output = {
            status: isInitialized
                ? ("healthy" as const)
                : ("unhealthy" as const),
            message: initializationError
                ? `Error: ${initializationError.message}`
                : isInitialized
                ? "Connected to Actual Budget"
                : "Not yet initialized",
            initialized: isInitialized,
        };

        return {
            content: [{ type: "text", text: JSON.stringify(output, null, 2) }],
            structuredContent: output,
        };
    }
);

// Register all tool groups
registerTransactionTools(server, ensureInitialized, actual);
registerPayeeTools(server, ensureInitialized, actual);
registerCategoryTools(server, ensureInitialized, actual);
registerAccountTools(server, ensureInitialized, actual);

// Setup stdio transport and start server
async function main() {
    const transport = new StdioServerTransport();

    await server.connect(transport);

    console.error("Actual Budget MCP Server running on stdio");
    console.error("Configuration:");
    console.error(`  Server URL: ${config.serverURL}`);
    console.error(`  Budget ID: ${config.budgetId}`);
    console.error(`  Data Dir: ${config.dataDir}`);
}

// Handle shutdown gracefully
process.on("SIGINT", async () => {
    console.error("Shutting down...");
    if (isInitialized) {
        await actual.shutdown();
    }
    process.exit(0);
});

process.on("SIGTERM", async () => {
    console.error("Shutting down...");
    if (isInitialized) {
        await actual.shutdown();
    }
    process.exit(0);
});

// Start the server
main().catch((error) => {
    console.error("Fatal error:", error);
    process.exit(1);
});
