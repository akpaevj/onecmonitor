import path from "node:path";
import { fileURLToPath } from "node:url";
import type { NextConfig } from "next";

const currentDir = path.dirname(fileURLToPath(import.meta.url));

const nextConfig: NextConfig = {
  // Держит production Docker-образ компактным - в рантайме нужны только трассированные файлы и
  // минимальный server.js, полная копия node_modules не требуется. См. oneswiss-web/Dockerfile.
  output: "standalone",
  turbopack: {
    root: currentDir,
  },
};

export default nextConfig;
