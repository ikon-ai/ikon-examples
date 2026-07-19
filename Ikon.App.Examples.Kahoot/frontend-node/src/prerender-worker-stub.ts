// Stand-in for `*?worker` imports in the Node prerender bundle (see vite.config.ssr.js). Workers
// are a client-side render optimization; the prerender path runs the UI store on the main thread,
// so instantiating this is a bug — fail loudly if it ever happens.
export default class PrerenderWorkerStub {
  constructor() {
    throw new Error('Web workers are not available in the prerender bundle');
  }
}
