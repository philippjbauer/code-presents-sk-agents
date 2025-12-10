import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import type * as actual from "@actual-app/api";

/**
 * Register payee-related tools for the MCP server
 */
export function registerPayeeTools(
    server: McpServer,
    ensureInitialized: () => Promise<void>,
    api: typeof actual
): void {
    /**
     * Get all payees
     */
    server.registerTool(
        "get-payees",
        {
            title: "Get All Payees",
            description:
                "Retrieve all payees in the budget. Use this to see what payees already exist before creating new ones.",
            inputSchema: {},
            outputSchema: {
                payees: z.array(
                    z.object({
                        id: z.string(),
                        name: z.string(),
                        category: z.string().optional(),
                        transfer_acct: z.string().nullable().optional(),
                    })
                ),
                count: z.number(),
            },
        },
        async () => {
            await ensureInitialized();

            const payees = await api.getPayees();

            const output = {
                payees: payees.map((p) => ({
                    id: p.id,
                    name: p.name,
                    category: p.category,
                    transfer_acct: p.transfer_acct,
                })),
                count: payees.length,
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
     * Find matching payee by name
     */
    server.registerTool(
        "find-matching-payee",
        {
            title: "Find Matching Payee",
            description:
                "Search for payees by name using fuzzy matching. Returns payees that closely match the search term.",
            inputSchema: {
                searchTerm: z.string().describe("The payee name to search for"),
                exactMatch: z
                    .boolean()
                    .default(false)
                    .describe("Require exact match instead of fuzzy search"),
            },
            outputSchema: {
                matches: z.array(
                    z.object({
                        id: z.string(),
                        name: z.string(),
                        category: z.string().optional(),
                        similarity: z.number().optional(),
                    })
                ),
                count: z.number(),
            },
        },
        async ({ searchTerm, exactMatch }) => {
            await ensureInitialized();

            const allPayees = await api.getPayees();
            const searchLower = searchTerm.toLowerCase();

            let matches;
            if (exactMatch) {
                matches = allPayees.filter(
                    (p) => p.name.toLowerCase() === searchLower
                );
            } else {
                // Fuzzy matching: includes substring
                matches = allPayees.filter(
                    (p) =>
                        p.name.toLowerCase().includes(searchLower) ||
                        searchLower.includes(p.name.toLowerCase())
                );
            }

            const output = {
                matches: matches.map((p) => ({
                    id: p.id,
                    name: p.name,
                    category: p.category,
                })),
                count: matches.length,
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
     * Get or create payee
     */
    server.registerTool(
        "get-or-create-payee",
        {
            title: "Get or Create Payee",
            description:
                "Find an existing payee by name, or create a new one if it doesn't exist. This is the recommended tool for ensuring a payee exists.",
            inputSchema: {
                name: z.string().describe("The payee name"),
                category: z
                    .string()
                    .optional()
                    .describe("Default category if creating new payee"),
            },
            outputSchema: {
                payeeId: z.string(),
                name: z.string(),
                created: z.boolean(),
                category: z.string().optional(),
            },
        },
        async ({ name, category }) => {
            await ensureInitialized();

            // First, try to find existing payee
            const allPayees = await api.getPayees();
            const existing = allPayees.find(
                (p) => p.name.toLowerCase() === name.toLowerCase()
            );

            if (existing) {
                const output = {
                    payeeId: existing.id,
                    name: existing.name,
                    created: false,
                    category: existing.category,
                };

                return {
                    content: [
                        { type: "text", text: JSON.stringify(output, null, 2) },
                    ],
                    structuredContent: output,
                };
            }

            // Create new payee
            const payeeId = await api.createPayee({ name, category });

            const output = {
                payeeId,
                name,
                created: true,
                category,
            };

            return {
                content: [
                    { type: "text", text: JSON.stringify(output, null, 2) },
                ],
                structuredContent: output,
            };
        }
    );
}
