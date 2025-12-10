import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import type * as actual from "@actual-app/api";

/**
 * Register account-related tools for the MCP server
 */
export function registerAccountTools(
    server: McpServer,
    ensureInitialized: () => Promise<void>,
    api: typeof actual
): void {
    /**
     * Get all accounts
     */
    server.registerTool(
        "get-accounts",
        {
            title: "Get All Accounts",
            description:
                "Gets information for all available accounts. Use this to get IDs, names and status for all accounts.",
            inputSchema: {},
            outputSchema: {
                accounts: z.array(
                    z.object({
                        id: z.string(),
                        name: z.string(),
                        offbudget: z.boolean().optional(),
                        closed: z.boolean().optional(),
                    })
                ),
                count: z.number(),
            },
        },
        async () => {
            await ensureInitialized();

            const accounts = await api.getAccounts();

            const output = {
                accounts: accounts.map((a: any) => ({
                    id: a.id,
                    name: a.name,
                    offbudget: a.offbudget,
                    closed: a.closed,
                })),
                count: accounts.length,
            };

            return {
                content: [
                    { type: "text", text: JSON.stringify(output, null, 2) },
                ],
                structuredContent: output,
            };
        }
    );

    /**
     * Get account balance
     */
    server.registerTool(
        "get-account-balance",
        {
            title: "Get Account Balance",
            description:
                "Get the current balance for an account, optionally as of a specific date.",
            inputSchema: {
                accountId: z
                    .string()
                    .regex(/^[a-f0-9\-]{36}$/)
                    .describe("The account UUID"),
                date: z.date().optional().describe("Optional cutoff date"),
            },
            outputSchema: {
                accountId: z.string(),
                balance: z.number(),
                asOfDate: z.string().optional(),
            },
        },
        async ({ accountId, date }: any) => {
            await ensureInitialized();

            const cutoffDate = date ? new Date(date) : undefined;
            const balance = await api.getAccountBalance(accountId, cutoffDate);

            const output = {
                accountId,
                balance: balance / 100,
                asOfDate: date,
            };

            return {
                content: [
                    { type: "text", text: JSON.stringify(output, null, 2) },
                ],
                structuredContent: output,
            };
        }
    );

    // /**
    //  * Find account by name
    //  */
    // server.registerTool(
    //   'find-account-by-name',
    //   {
    //     title: 'Find Account by Name',
    //     description: 'Search for an account by name.',
    //     inputSchema: {
    //       name: z.string().describe('The account name to search for'),
    //       exactMatch: z.boolean().default(false).describe('Require exact match')
    //     },
    //     outputSchema: {
    //       matches: z.array(z.object({
    //         id: z.string(),
    //         name: z.string(),
    //         offbudget: z.boolean().optional(),
    //         closed: z.boolean().optional()
    //       })),
    //       count: z.number()
    //     }
    //   },
    //   async ({ name, exactMatch }: any) => {
    //     await ensureInitialized();

    //     const allAccounts = await api.getAccounts();
    //     const searchLower = name.toLowerCase();

    //     let matches;
    //     if (exactMatch) {
    //       matches = allAccounts.filter((a: any) => a.name.toLowerCase() === searchLower);
    //     } else {
    //       matches = allAccounts.filter((a: any) => a.name.toLowerCase().includes(searchLower));
    //     }

    //     const output = {
    //       matches: matches.map((a: any) => ({
    //         id: a.id,
    //         name: a.name,
    //         offbudget: a.offbudget,
    //         closed: a.closed
    //       })),
    //       count: matches.length
    //     };

    //     return {
    //       content: [{ type: 'text', text: JSON.stringify(output, null, 2) }],
    //       structuredContent: output
    //     };
    //   }
    // );
}
