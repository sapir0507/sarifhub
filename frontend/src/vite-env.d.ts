/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** "hash" for static demo hosting; anything else uses browser history routing. */
  readonly VITE_ROUTER?: string;
}
