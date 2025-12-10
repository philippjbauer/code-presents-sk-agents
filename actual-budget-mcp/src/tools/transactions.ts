import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import type * as actual from "@actual-app/api";

/**
 * Register transaction-related tools for the MCP server
 */
export function registerTransactionTools(
    server: McpServer,
    ensureInitialized: () => Promise<void>,
    api: typeof actual
): void {
    /**
     * Get transactions for an account within a date range
     */
    server.registerTool(
        "get-transactions",
        {
            title: "Get Transactions",
            description:
                "Retrieve transactions for an account within a date range. Use this to review transactions that need categorization.",
            inputSchema: {
                accountId: z
                    .string()
                    .regex(/^[a-f0-9\-]{36}$/)
                    .describe("The account UUID"),
                startDate: z
                    .string()
                    .regex(/^\d{4}-\d{2}-\d{2}$/)
                    .describe("Start date in YYYY-MM-DD format"),
                endDate: z
                    .string()
                    .regex(/^\d{4}-\d{2}-\d{2}$/)
                    .describe("End date in YYYY-MM-DD format"),
                offset: z.string().optional().describe("Pagination offset"),
                limit: z.string().optional().describe("Pagination limit"),
                category: z
                    .string()
                    .optional()
                    .describe("Category string to filter by"),
            },
            outputSchema: {
                transactions: z.array(
                    z.object({
                        id: z.string(),
                        account: z.string(),
                        date: z.string(),
                        amount: z.number(),
                        payee_id: z.string().nullable().optional(),
                        category: z.string().nullable().optional(),
                        notes: z.string().nullable().optional(),
                        cleared: z.boolean().optional(),
                        imported_payee: z.string().nullable().optional(),
                    })
                ),
                count: z.number(),
            },
        },
        async ({ accountId, startDate, endDate, offset, limit, category }) => {
            await ensureInitialized();

            let transactions = await api.getTransactions(
                accountId,
                startDate,
                endDate
            );

            // If a category filter was provided, return only matching transactions (case-insensitive)
            if (
                category !== undefined &&
                category !== null &&
                String(category).trim() !== ""
            ) {
                const normalizedCategory = String(category)
                    .trim()
                    .toLowerCase();
                transactions = transactions.filter((t) => {
                    const tcat = t.category ?? "";
                    return (
                        String(tcat).trim().toLowerCase() === normalizedCategory
                    );
                });
            }

            const parsedOffset =
                offset !== undefined ? parseInt(offset, 10) : undefined;
            const parsedLimit =
                limit !== undefined ? parseInt(limit, 10) : undefined;

            const paginated = paginateResults(
                transactions,
                parsedOffset,
                parsedLimit
            );

            const output = {
                transactions: paginated.map((t) => ({
                    id: t.id,
                    account: t.account,
                    date: t.date,
                    amount: t.amount / 100,
                    payee_id: t.payee_id,
                    category: t.category,
                    notes: t.notes,
                    cleared: t.cleared,
                    imported_payee: t.imported_payee,
                })),
                count: paginated.length,
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
     * Get uncategorized transactions
     */
    server.registerTool(
        "get-uncategorized-transactions",
        {
            title: "Get Uncategorized Transactions",
            description:
                "Retrieve all (or paginated) transactions without a category. Perfect for finding transactions that need to be reviewed and categorized.",
            inputSchema: {
                accountId: z
                    .string()
                    .regex(/^[a-f0-9\-]{36}$/)
                    .describe("The account UUID"),
                startDate: z
                    .string()
                    .regex(/^\d{4}-\d{2}-\d{2}$/)
                    .describe("Start date in YYYY-MM-DD format"),
                endDate: z
                    .string()
                    .regex(/^\d{4}-\d{2}-\d{2}$/)
                    .describe("End date in YYYY-MM-DD format"),
                offset: z.string().optional().describe("Pagination offset"),
                limit: z.string().optional().describe("Pagination limit"),
            },
            outputSchema: {
                transactions: z.array(
                    z.object({
                        id: z.string(),
                        account: z.string(),
                        date: z.string(),
                        amount: z.number(),
                        payee_id: z.string().nullable().optional(),
                        notes: z.string().nullable().optional(),
                        cleared: z.boolean().optional(),
                        imported_payee: z.string().nullable().optional(),
                    })
                ),
                count: z.number(),
            },
        },
        async ({ accountId, startDate, endDate, offset, limit }) => {
            await ensureInitialized();

            const parsedOffset =
                offset !== undefined ? parseInt(offset, 10) : undefined;
            const parsedLimit =
                limit !== undefined ? parseInt(limit, 10) : undefined;

            const transactions = await api.getTransactions(
                accountId,
                startDate,
                endDate
            );

            const uncategorized = transactions.filter((t) => !t.category);

            const paginated = paginateResults(
                uncategorized,
                parsedOffset,
                parsedLimit
            );

            const output = {
                transactions: paginated.map((t) => ({
                    id: t.id,
                    account: t.account,
                    date: t.date,
                    amount: t.amount / 100,
                    payee_id: t.payee_id,
                    notes: t.notes,
                    cleared: t.cleared,
                    imported_payee: t.imported_payee,
                })),
                count: paginated.length,
            };

            return {
                content: [
                    { type: "text", text: JSON.stringify(output, null, 2) },
                ],
                structuredContent: output,
            };
        }
    );

    function paginateResults<T>(
        results: T[],
        offset: number = 0,
        limit: number = 1000
    ): T[] {
        return results.slice(offset, offset + limit);
    }

    /**
     * Update a transaction
     */
    server.registerTool(
        "update-transaction",
        {
            title: "Update Transaction",
            description:
                "Update transaction fields including payee, category, and notes. Use this to categorize transactions after reviewing them.",
            inputSchema: {
                transactionId: z
                    .string()
                    .regex(/^[a-f0-9\-]{36}$/)
                    .describe("The transaction UUID"),
                payeeId: z
                    .string()
                    .regex(/^[a-f0-9\-]{36}$/)
                    .nullable()
                    .optional()
                    .describe("Payee UUID to assign"),
                categoryId: z
                    .string()
                    .regex(/^[a-f0-9\-]{36}$/)
                    .nullable()
                    .optional()
                    .describe("Category UUID to assign"),
                notes: z
                    .string()
                    .nullable()
                    .optional()
                    .describe("Notes to add or update"),
                // cleared: z
                //     .boolean()
                //     .optional()
                //     .describe("Whether the transaction is cleared"),
            },
            outputSchema: {
                success: z.boolean(),
                transactionId: z.string(),
                updatedFields: z.record(z.any()),
            },
        },
        // async ({ transactionId, payee, category, notes, cleared }) => {
        async ({ transactionId, payeeId, categoryId, notes }) => {
            await ensureInitialized();

            const updates: Record<string, any> = {};
            if (payeeId !== undefined && payeeId !== null)
                updates.payee = payeeId;
            if (categoryId !== undefined && categoryId !== null)
                updates.category = categoryId;
            if (notes !== undefined && notes !== null) updates.notes = notes;
            // if (cleared !== undefined) updates.cleared = cleared;

            await api.updateTransaction(transactionId, updates);

            const output = {
                success: true,
                transactionId,
                updatedFields: updates,
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
     * Update multiple transactions
     */
    server.registerTool(
        "update-multiple-transactions",
        {
            title: "Update Multiple Transactions",
            description:
                "Update multiple transactions in a single call. Each update can include payee, category, and notes changes. Use this to efficiently categorize or modify multiple transactions at once.",
            inputSchema: {
                updates: z
                    .array(
                        z.object({
                            transactionId: z
                                .string()
                                .regex(/^[a-f0-9\-]{36}$/)
                                .describe("The transaction UUID"),
                            payeeId: z
                                .string()
                                .regex(/^[a-f0-9\-]{36}$/)
                                .nullable()
                                .optional()
                                .describe("Payee UUID to assign"),
                            categoryId: z
                                .string()
                                .regex(/^[a-f0-9\-]{36}$/)
                                .nullable()
                                .optional()
                                .describe("Category UUID to assign"),
                            notes: z
                                .string()
                                .nullable()
                                .optional()
                                .describe("Notes to add or update"),
                        })
                    )
                    .describe("Array of transaction updates to apply"),
            },
            outputSchema: {
                success: z.boolean(),
                processedCount: z.number(),
                results: z.array(
                    z.object({
                        transactionId: z.string(),
                        success: z.boolean(),
                        updatedFields: z.record(z.any()).optional(),
                        error: z.string().optional(),
                    })
                ),
            },
        },
        async ({ updates }) => {
            await ensureInitialized();

            const results = [];
            let successCount = 0;

            for (const update of updates) {
                try {
                    const { transactionId, payeeId, categoryId, notes } =
                        update;

                    const updateFields: Record<string, any> = {};
                    if (payeeId !== undefined && payeeId !== null)
                        updateFields.payee = payeeId;
                    if (categoryId !== undefined && categoryId !== null)
                        updateFields.category = categoryId;
                    if (notes !== undefined && notes !== null)
                        updateFields.notes = notes;

                    await api.updateTransaction(transactionId, updateFields);

                    results.push({
                        transactionId,
                        success: true,
                        updatedFields: updateFields,
                    });
                    successCount++;
                } catch (error) {
                    results.push({
                        transactionId: update.transactionId,
                        success: false,
                        error:
                            error instanceof Error
                                ? error.message
                                : String(error),
                    });
                }
            }

            const output = {
                success: successCount === updates.length,
                processedCount: successCount,
                results,
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
