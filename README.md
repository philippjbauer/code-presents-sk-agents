# AI Agents in Semantic Kernel – Webinar Code

**Instructor:** Philipp Bauer, Senior Software Architect at CODE Consulting  
**Webinar Date:** December 10, 2025

## Overview

This repository contains the code samples and practical implementations from the webinar "Building Intelligent Multi-Agent Systems with Microsoft's Semantic Kernel." The session demonstrates how to orchestrate collaborative AI agents in .NET, featuring persistent vector memory and MCP tool integration.

**Target Audience:**

-   C#/.NET developers
-   AI engineers interested in agent orchestration
-   Anyone looking to build production-ready multi-agent systems with Semantic Kernel

## Key Topics Covered

-   Multi-agent orchestration patterns in .NET
-   Integrating persistent vector memory for contextual AI
-   Using MCP tools for agent collaboration
-   Practical, production-focused code examples

## Repository Structure

-   `Agents/` – C# agent implementations (AccountAgent, CategoryAgent, etc.)
-   `actual-budget-mcp/` – MCP tool integration (TypeScript/Node.js)
-   `ChatModes/` – Chat orchestration modes and logic
-   `Configuration/` – App configuration files
-   `Helpers/` – Utility and formatting helpers
-   `Models/` – Data models for agents and chat
-   `Plugins/` – Example plugin integrations
-   `Program.cs` – Main entry point for the .NET application

## Getting Started

1. **.NET Solution:**

    - Open `CODE.Presents.SemanticKernel.sln` in Visual Studio or VS Code.
    - Restore NuGet packages and build the solution.
    - Run `Program.cs` to start the agent system.

2. **MCP Tools (Node.js):**
    - Navigate to `actual-budget-mcp/`.
    - Run `npm install` to install dependencies.
    - Use the scripts in `src/tools/` for MCP integration.

## Prerequisites

-   [.NET 10.0+](https://dotnet.microsoft.com/)
-   [Node.js 18+](https://nodejs.org/)

## Learning Outcomes

-   Build and orchestrate multiple AI agents in C#
-   Integrate vector memory for persistent context
-   Connect agents to external tools (MCP) for real-world workflows
-   Apply best practices for scalable, production-ready agent systems

## More Resources

-   [Microsoft Semantic Kernel Documentation](https://aka.ms/semantic-kernel)
-   [CODE Consulting](https://www.codemag.com/)

---

_For questions or feedback, please contact Philipp Bauer via CODE Consulting._
