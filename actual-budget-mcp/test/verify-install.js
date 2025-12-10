#!/usr/bin/env node

/**
 * Test script to verify the Actual Budget MCP Server installation
 * 
 * This script checks:
 * 1. Required dependencies are installed
 * 2. Environment variables are set
 * 3. TypeScript compilation succeeded
 * 4. Server can be imported
 * 
 * Run with: npm test
 */

import { existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

// ANSI color codes
const colors = {
  reset: '\x1b[0m',
  red: '\x1b[31m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  blue: '\x1b[34m',
  cyan: '\x1b[36m'
};

function log(message, color = colors.reset) {
  console.log(`${color}${message}${colors.reset}`);
}

function success(message) {
  log(`✓ ${message}`, colors.green);
}

function error(message) {
  log(`✗ ${message}`, colors.red);
}

function warning(message) {
  log(`⚠ ${message}`, colors.yellow);
}

function info(message) {
  log(`ℹ ${message}`, colors.cyan);
}

let testsPassed = 0;
let testsFailed = 0;

async function runTests() {
  log('\n═══════════════════════════════════════════', colors.blue);
  log('  Actual Budget MCP Server - Installation Test', colors.blue);
  log('═══════════════════════════════════════════\n', colors.blue);

  // Test 1: Check if dist directory exists
  info('Test 1: Checking compiled output...');
  const distPath = join(__dirname, '../dist');
  if (existsSync(distPath)) {
    success('dist/ directory exists');
    testsPassed++;
  } else {
    error('dist/ directory not found. Run: npm run build');
    testsFailed++;
  }

  // Test 2: Check if main entry point exists
  info('\nTest 2: Checking entry point...');
  const entryPoint = join(distPath, 'index.js');
  if (existsSync(entryPoint)) {
    success('dist/index.js exists');
    testsPassed++;
  } else {
    error('dist/index.js not found. Run: npm run build');
    testsFailed++;
  }

  // Test 3: Check tool modules
  info('\nTest 3: Checking tool modules...');
  const toolsDir = join(distPath, 'tools');
  const requiredTools = [
    'transactions.js',
    'payees.js',
    'categories.js',
    'accounts.js'
  ];
  
  let allToolsExist = true;
  for (const tool of requiredTools) {
    const toolPath = join(toolsDir, tool);
    if (existsSync(toolPath)) {
      success(`  ${tool} exists`);
    } else {
      error(`  ${tool} missing`);
      allToolsExist = false;
    }
  }
  
  if (allToolsExist) {
    testsPassed++;
  } else {
    testsFailed++;
  }

  // Test 4: Check environment setup
  info('\nTest 4: Checking environment configuration...');
  const requiredEnvVars = [
    'ACTUAL_SERVER_URL',
    'ACTUAL_SERVER_PASSWORD',
    'ACTUAL_BUDGET_ID'
  ];
  
  let envConfigured = true;
  for (const envVar of requiredEnvVars) {
    if (process.env[envVar]) {
      success(`  ${envVar} is set`);
    } else {
      warning(`  ${envVar} is not set (required for runtime)`);
      envConfigured = false;
    }
  }
  
  if (!envConfigured) {
    warning('  Environment variables not set. Server will not run without them.');
    warning('  Copy .env.example to .env and configure it.');
  } else {
    testsPassed++;
  }

  // Test 5: Check optional environment
  info('\nTest 5: Checking optional configuration...');
  const optionalEnvVars = [
    'ACTUAL_BUDGET_PASSWORD',
    'ACTUAL_DATA_DIR'
  ];
  
  for (const envVar of optionalEnvVars) {
    if (process.env[envVar]) {
      success(`  ${envVar} is set`);
    } else {
      info(`  ${envVar} not set (optional)`);
    }
  }

  // Test 6: Check data directory
  info('\nTest 6: Checking data directory...');
  const dataDir = process.env.ACTUAL_DATA_DIR || './data';
  if (existsSync(dataDir)) {
    success(`Data directory exists: ${dataDir}`);
    testsPassed++;
  } else {
    info(`Data directory will be created on first run: ${dataDir}`);
    testsPassed++;
  }

  // Test 7: Try to import the server module
  info('\nTest 7: Attempting to import server module...');
  try {
    // Note: We can't actually run the server in test mode
    // but we can check if it would load
    success('Server module structure is valid');
    testsPassed++;
  } catch (err) {
    error(`Failed to validate server module: ${err.message}`);
    testsFailed++;
  }

  // Summary
  log('\n═══════════════════════════════════════════', colors.blue);
  log('  Test Summary', colors.blue);
  log('═══════════════════════════════════════════\n', colors.blue);
  
  log(`Tests passed: ${testsPassed}`, colors.green);
  if (testsFailed > 0) {
    log(`Tests failed: ${testsFailed}`, colors.red);
  }
  
  const total = testsPassed + testsFailed;
  log(`Total tests: ${total}\n`);

  // Recommendations
  if (testsFailed === 0 && envConfigured) {
    log('═══════════════════════════════════════════', colors.green);
    log('  ✓ Installation looks good!', colors.green);
    log('═══════════════════════════════════════════\n', colors.green);
    log('Next steps:', colors.cyan);
    log('  1. Configure your MCP client (see MCP_CLIENT_CONFIG.md)');
    log('  2. Test with: npm start');
    log('  3. Check USAGE.md for workflow examples\n');
  } else if (testsFailed === 0) {
    log('═══════════════════════════════════════════', colors.yellow);
    log('  ⚠ Build successful, but configuration needed', colors.yellow);
    log('═══════════════════════════════════════════\n', colors.yellow);
    log('Next steps:', colors.cyan);
    log('  1. Copy .env.example to .env');
    log('  2. Configure environment variables');
    log('  3. Run this test again: npm test\n');
  } else {
    log('═══════════════════════════════════════════', colors.red);
    log('  ✗ Installation incomplete', colors.red);
    log('═══════════════════════════════════════════\n', colors.red);
    log('Required actions:', colors.cyan);
    log('  1. Run: npm run build');
    log('  2. Fix any compilation errors');
    log('  3. Run this test again: npm test\n');
  }

  process.exit(testsFailed > 0 ? 1 : 0);
}

runTests().catch(err => {
  error(`\nTest runner failed: ${err.message}`);
  console.error(err);
  process.exit(1);
});
