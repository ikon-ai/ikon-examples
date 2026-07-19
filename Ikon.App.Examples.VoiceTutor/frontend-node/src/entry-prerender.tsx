// Boot-snapshot prerender CLI. Built by vite.config.ssr.js into a self-contained Node bundle and
// invoked by `ikon app bundle` after the frontend build: for each captured route snapshot it renders
// the UI tree to static HTML through the same React component pipeline the browser uses, and writes
// a crawlable `__routes/<slug>-<hash>.html` page per route. The live app removes the prerendered
// content (everything marked `data-ikon-prerender`) the moment it paints real content.
//
// Usage: node entry-prerender.mjs <input.json>
// Input: { indexHtmlPath, outDir, outputPath, routes: [{ route, snapshotPath, snapshotFile }] }
// Output (outputPath): { version: 1, routes: { "<route>": "__routes/<file>.html" } }
import { createHash } from 'node:crypto';
import { mkdirSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { prerender } from 'react-dom/static';
import { IkonUiRegistry, UiRenderer, createBaseResolvers } from '@ikonai/sdk-react-ui';
import { UiStreamStore, parseUiUpdate } from '@ikonai/sdk-ui';
import * as standardUiModule from '@ikonai/sdk-react-ui-standard';
import { createStandardUiResolvers, registerLucideIconsModule } from '@ikonai/sdk-react-ui-standard';

// Runtime lookup instead of a named import: an app may run this entry against an SDK version that
// predates the export, which must degrade to "no reset styles", not fail the bundle build.
const radixResetStyles = (standardUiModule as { RADIX_RESET_STYLES?: string }).RADIX_RESET_STYLES ?? '';

interface PrerenderRouteInput {
  route: string;
  snapshotPath: string;
  snapshotFile: string;
}

interface PrerenderInput {
  indexHtmlPath: string;
  outDir: string;
  outputPath: string;
  routes: PrerenderRouteInput[];
}

interface BootSnapshotStyle {
  styleId: string;
  css?: string;
  common?: string;
}

interface BootSnapshotFile {
  streamId: string;
  json: string;
  styles?: BootSnapshotStyle[];
}

const PRERENDER_TIMEOUT_MS = 15000;

async function streamToString(stream: ReadableStream<Uint8Array>): Promise<string> {
  const reader = stream.getReader();
  const decoder = new TextDecoder();
  let result = '';
  for (;;) {
    const { done, value } = await reader.read();
    if (done) break;
    result += decoder.decode(value, { stream: true });
  }
  return result + decoder.decode();
}

function withTimeout<T>(promise: Promise<T>, ms: number, what: string): Promise<T> {
  return new Promise<T>((resolvePromise, rejectPromise) => {
    const timer = setTimeout(() => rejectPromise(new Error(`${what} did not settle within ${ms}ms`)), ms);
    promise.then(
      (value) => {
        clearTimeout(timer);
        resolvePromise(value);
      },
      (error: unknown) => {
        clearTimeout(timer);
        rejectPromise(error instanceof Error ? error : new Error(String(error)));
      },
    );
  });
}

/**
 * Imports every chunk this bundle emitted (icons, markdown, charts — the React.lazy targets) so
 * they sit resolved in the module cache before rendering starts. A chunk load initiated mid-render
 * leaves react-dom/static's prerender unsettled; a cache hit resolves on the microtask queue.
 */
async function warmLazyChunks(): Promise<void> {
  const selfPath = fileURLToPath(import.meta.url);
  const bundleDir = dirname(selfPath);

  for (const dir of [bundleDir, join(bundleDir, 'assets')]) {
    let entries: string[];
    try {
      entries = readdirSync(dir);
    } catch {
      continue;
    }
    for (const name of entries) {
      if (!/\.(mjs|js)$/.test(name)) continue;
      const chunkPath = join(dir, name);
      if (chunkPath === selfPath) continue;
      try {
        await import(/* @vite-ignore */ pathToFileURL(chunkPath).href);
      } catch {
        // Not an importable module or environment-bound — the render simply falls back to
        // loading it on demand.
      }
    }
  }
}

function buildRegistry(): IkonUiRegistry {
  const registry = new IkonUiRegistry();
  registry.registerResolvers(createBaseResolvers());
  registry.registerResolvers(createStandardUiResolvers());
  registerLucideIconsModule(registry);
  return registry;
}

async function renderSnapshot(snapshot: BootSnapshotFile, registry: IkonUiRegistry): Promise<string> {
  const store = new UiStreamStore();
  store.apply(parseUiUpdate({ Json: snapshot.json } as Parameters<typeof parseUiUpdate>[0]));
  const element = <UiRenderer store={store} library={registry} />;

  let prelude: ReadableStream<Uint8Array>;
  try {
    ({ prelude } = await withTimeout(prerender(element), PRERENDER_TIMEOUT_MS, 'prerender'));
  } catch {
    // A React.lazy chunk whose load was initiated mid-render can leave prerender unsettled. The
    // first attempt still kicked the imports off, so by the retry they are resolved module records.
    ({ prelude } = await withTimeout(prerender(element), PRERENDER_TIMEOUT_MS, 'prerender retry'));
  }

  return streamToString(prelude);
}

// `</` would terminate the surrounding <style> block from inside CSS text (a stored-XSS vector via
// app-interpolated style content); `<\/` is an equivalent escaped form for CSS string/url contexts.
function escapeCssForStyleTag(css: string): string {
  return css.replace(/<\//g, '<\\/');
}

// Values inlined into a <script> must not be able to terminate the tag or open another one.
function escapeJsonForScriptTag(value: string): string {
  return JSON.stringify(value).replace(/</g, '\\u003c');
}

function routeTitleSuffix(route: string): string {
  const segments = route.split('/').filter(Boolean);
  return segments.length === 0 ? '' : ` — ${segments.join(' · ')}`;
}

function slugForRoute(route: string): string {
  if (route === '/') return 'root';
  const slug = route
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
  return slug.length === 0 ? 'route' : slug.slice(0, 40);
}

function assembleRouteHtml(indexHtml: string, routeInput: PrerenderRouteInput, markup: string, css: string): string {
  const headInjection = [
    `<script>window.__IKON_BOOT_SNAPSHOT_FILE__=${escapeJsonForScriptTag(routeInput.snapshotFile)};window.__IKON_BOOT_SNAPSHOT_ROUTE__=${escapeJsonForScriptTag(routeInput.route)};</script>`,
    // The prerendered content depicts the app's PUBLIC entry view. A visitor with a signed-in
    // session must not see it flash before their app loads (for a deferred-login app it is the
    // guest landing, i.e. the wrong page) — detect the stored session before first paint and hide
    // the block; they get the app's normal connecting experience instead. Runs in <head>, so it
    // executes before any content paints. Crawlers and guests are unaffected.
    `<script data-ikon-prerender>try{var ikonSession=JSON.parse(localStorage.getItem('ikon-auth-session'));if(ikonSession&&ikonSession.provider&&ikonSession.provider!=='anonymous'){document.documentElement.setAttribute('data-ikon-prerender-skip','')}}catch(e){}</script>`,
    `<style data-ikon-prerender>html[data-ikon-prerender-skip] [data-ikon-prerender]{display:none !important}</style>`,
    `<link rel="preload" as="fetch" href="/${routeInput.snapshotFile}" crossorigin="anonymous">`,
    `<style data-ikon-prerender>${escapeCssForStyleTag(css)}</style>`,
  ].join('\n');

  let html = indexHtml;
  html = html.replace(/<title>([^<]*)<\/title>/i, (_, title: string) => `<title>${title}${routeTitleSuffix(routeInput.route)}</title>`);
  // The prerendered page shows real content to no-JS visitors; the template's "JavaScript is
  // required" notice would sit next to it and contradict it.
  html = html.replace(/<noscript>[\s\S]*?<\/noscript>/i, '');
  html = html.replace('</head>', `${headInjection}\n</head>`);
  // The markup lives in a fixed-position sibling of #root, not inside it: React's mount wipes the
  // container it renders into, while the sibling stays painted until the app commits real content
  // and removes everything marked data-ikon-prerender.
  html = html.replace(
    /<div id="root">\s*<\/div>/i,
    `<div id="ikon-prerender" data-ikon-prerender style="position:fixed;inset:0;overflow:hidden">${markup}</div><div id="root"></div>`,
  );
  return html;
}

async function main(): Promise<void> {
  const inputPath = process.argv[2];
  if (!inputPath) {
    console.error('usage: entry-prerender <input.json>');
    process.exit(1);
  }

  const input = JSON.parse(readFileSync(inputPath, 'utf8')) as PrerenderInput;
  const indexHtml = readFileSync(input.indexHtmlPath, 'utf8');
  const registry = buildRegistry();
  await warmLazyChunks();

  const routesDir = join(input.outDir, '__routes');
  mkdirSync(routesDir, { recursive: true });

  const outputRoutes: Record<string, string> = {};

  for (const routeInput of input.routes) {
    try {
      const snapshot = JSON.parse(readFileSync(routeInput.snapshotPath, 'utf8')) as BootSnapshotFile;
      const markup = await renderSnapshot(snapshot, registry);

      if (markup.trim().length === 0) {
        console.warn(`prerender: route '${routeInput.route}' rendered empty markup, skipping`);
        continue;
      }

      const css = [radixResetStyles, ...(snapshot.styles ?? []).map((style) => style.css).filter(Boolean)].join('\n');
      const html = assembleRouteHtml(indexHtml, routeInput, markup, css);
      const htmlHash = createHash('sha256').update(html).digest('hex').slice(0, 12);
      const fileName = `${slugForRoute(routeInput.route)}-${htmlHash}.html`;
      writeFileSync(join(routesDir, fileName), html);
      outputRoutes[routeInput.route] = `__routes/${fileName}`;
      console.log(`prerender: ${routeInput.route} -> __routes/${fileName} (${html.length} bytes)`);
    } catch (error) {
      console.warn(`prerender: route '${routeInput.route}' failed, skipping: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  writeFileSync(input.outputPath, JSON.stringify({ version: 1, routes: outputRoutes }));
  console.log(`prerender: ${Object.keys(outputRoutes).length}/${input.routes.length} routes -> ${input.outputPath}`);
}

// No top-level await: emitted chunks import shared bindings back from this entry chunk, and a
// pending top-level await would deadlock those circular imports (which is exactly what a
// React.lazy chunk load during prerender triggers).
main().catch((error: unknown) => {
  console.error(`prerender failed: ${error instanceof Error ? error.message : String(error)}`);
  process.exitCode = 1;
});
