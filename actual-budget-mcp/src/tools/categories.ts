import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import type * as actual from "@actual-app/api";

/**
 * Register category-related tools for the MCP server
 */
export function registerCategoryTools(
    server: McpServer,
    ensureInitialized: () => Promise<void>,
    api: typeof actual
): void {
    /**
     * Get all categories
     */
    server.registerTool(
        "get-categories",
        {
            title: "Get All Categories",
            description:
                "Retrieve all categories in the budget. Use this to see what categories exist before creating new ones.",
            inputSchema: {},
            outputSchema: {
                categories: z.array(
                    z.object({
                        id: z.string(),
                        name: z.string(),
                        group_id: z.string(),
                        is_income: z.boolean().optional(),
                    })
                ),
                count: z.number(),
            },
        },
        async () => {
            await ensureInitialized();

            const categories = await api.getCategories();

            const output = {
                categories: categories.map((c: any) => ({
                    id: c.id,
                    name: c.name,
                    group_id: c.group_id,
                    is_income: c.is_income,
                })),
                count: categories.length,
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
     * Get category groups
     */
    server.registerTool(
        "get-category-groups",
        {
            title: "Get Category Groups",
            description:
                "Retrieve all category groups. Useful for understanding the category structure.",
            inputSchema: {},
            outputSchema: {
                groups: z.array(
                    z.object({
                        id: z.string(),
                        name: z.string(),
                        is_income: z.boolean().optional(),
                    })
                ),
                count: z.number(),
            },
        },
        async () => {
            await ensureInitialized();

            const groups = await api.getCategoryGroups();

            const output = {
                groups: groups.map((g: any) => ({
                    id: g.id,
                    name: g.name,
                    is_income: g.is_income,
                })),
                count: groups.length,
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
     * Find matching category by name
     */
    server.registerTool(
        "find-matching-category",
        {
            title: "Find Matching Category",
            description:
                "Search for categories by name using fuzzy matching. Returns categories that closely match the search term.",
            inputSchema: {
                searchTerm: z
                    .string()
                    .describe("The category name to search for"),
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
                        group_id: z.string(),
                        is_income: z.boolean().optional(),
                    })
                ),
                count: z.number(),
            },
        },
        async ({ searchTerm, exactMatch }: any) => {
            await ensureInitialized();

            const allCategories = await api.getCategories();
            const searchLower = searchTerm.toLowerCase();

            let matches;
            if (exactMatch) {
                matches = allCategories.filter(
                    (c: any) => c.name.toLowerCase() === searchLower
                );
            } else {
                // Fuzzy matching: includes substring
                matches = allCategories.filter(
                    (c: any) =>
                        c.name.toLowerCase().includes(searchLower) ||
                        searchLower.includes(c.name.toLowerCase())
                );
            }

            const output = {
                matches: matches.map((c: any) => ({
                    id: c.id,
                    name: c.name,
                    group_id: c.group_id,
                    is_income: c.is_income,
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
     * Get or create category
     */
    server.registerTool(
        "get-or-create-category",
        {
            title: "Get or Create Category",
            description:
                "Find an existing category by name, or create a new one if it doesn't exist. This is the recommended tool for ensuring a category exists.",
            inputSchema: {
                name: z.string().describe("The category name"),
                groupId: z
                    .string()
                    .describe(
                        "The category group ID (required if creating new)"
                    ),
                isIncome: z
                    .boolean()
                    .default(false)
                    .describe("Whether this is an income category"),
            },
            outputSchema: {
                categoryId: z.string(),
                name: z.string(),
                groupId: z.string(),
                created: z.boolean(),
            },
        },
        async ({ name, groupId, isIncome }: any) => {
            await ensureInitialized();

            // First, try to find existing category
            const allCategories = await api.getCategories();
            const existing = allCategories.find(
                (c: any) => c.name.toLowerCase() === name.toLowerCase()
            );

            if (existing) {
                const output = {
                    categoryId: existing.id,
                    name: existing.name,
                    groupId: existing.group_id,
                    created: false,
                };

                return {
                    content: [
                        { type: "text", text: JSON.stringify(output, null, 2) },
                    ],
                    structuredContent: output,
                };
            }

            // Create new category
            const categoryId = await api.createCategory({
                name,
                group_id: groupId,
                is_income: isIncome,
            });

            const output = {
                categoryId,
                name,
                groupId,
                created: true,
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
