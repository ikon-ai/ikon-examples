// Build config for the boot-snapshot prerender bundle: a self-contained Node CLI that renders
// captured route snapshots to static HTML at `ikon app bundle` time (SEO + instant first paint).
// Run via `npm run build:prerender`; the bundler tool invokes the built entry per deployment.
import react from '@vitejs/plugin-react';
import { resolve } from 'node:path';
import process from 'node:process';
import { defineConfig, loadEnv } from 'vite';

const __dirname = import.meta.dirname;

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'VITE_');
  const isIkonInternal = (process.env.VITE_IS_IKON_INTERNAL || env.VITE_IS_IKON_INTERNAL) === 'true';
  const platformTypescriptPath = process.env.VITE_IKON_PLATFORM_TYPESCRIPT_PATH || env.VITE_IKON_PLATFORM_TYPESCRIPT_PATH;

  const alias = [
    // The SDK packages import their web workers with Vite's `?worker` suffix, which cannot exist
    // in a Node SSR bundle. The prerender path never instantiates a worker, so every worker
    // module resolves to a throwing stub.
    { find: /^.*\?worker$/, replacement: resolve(__dirname, 'src/prerender-worker-stub.ts') },
  ];

  if (isIkonInternal && platformTypescriptPath) {
    // Source mode (--platform-repo): resolve the SDK packages from the platform repo's sources,
    // mirroring vite.config.js.
    for (const [name, path] of [
      ['@ikonai/sdk', 'sdk/sdk/src/index.ts'],
      ['@ikonai/sdk-libopus', 'sdk/sdk-libopus/src/index.ts'],
      ['@ikonai/sdk-react-ui', 'sdk/sdk-react-ui/src/index.ts'],
      ['@ikonai/sdk-react-ui-standard', 'sdk/sdk-react-ui-standard/src/index.ts'],
      ['@ikonai/sdk-react-ui-rive', 'sdk/sdk-react-ui-rive/src/index.ts'],
      ['@ikonai/sdk-ui', 'sdk/sdk-ui/src/index.ts'],
      ['@ikonai/configs', 'shared/configs/src/index.ts'],
      ['@ikonai/protocol', 'shared/protocol/src/index.ts'],
    ]) {
      alias.push({ find: name, replacement: resolve(platformTypescriptPath, path) });
    }
  }

  return {
  plugins: [react()],
  resolve: {
    alias,
    // One react copy no matter where the SDK sources resolve from — two copies in the bundle would
    // break hooks at runtime.
    dedupe: ['react', 'react-dom'],
  },
  build: {
    ssr: resolve(__dirname, 'src/entry-prerender.tsx'),
    outDir: resolve(__dirname, 'build/prerender'),
    emptyOutDir: true,
    target: 'node20',
    rollupOptions: {
      output: {
        format: 'es',
        entryFileNames: 'entry-prerender.mjs',
        chunkFileNames: 'assets/[name]-[hash].mjs',
      },
    },
  },
  ssr: {
    // Bundle everything (react, the SDK packages, lazy chunks) so the output runs with plain
    // `node` and needs nothing from node_modules at execution time.
    noExternal: true,
  },
  };
});
