import { cpSync, existsSync, mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const rootDir = dirname(dirname(fileURLToPath(import.meta.url)));
const source = join(rootDir, "node_modules", "monaco-editor", "min", "vs");
const destination = join(rootDir, "public", "monaco-editor", "vs");

if (!existsSync(source)) {
  console.warn("[monaco] monaco-editor package not found in node_modules, skipping asset copy");
  process.exit(0);
}

mkdirSync(dirname(destination), { recursive: true });
cpSync(source, destination, { recursive: true });

console.log(`[monaco] copied editor assets to ${destination}`);
