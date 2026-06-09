import { defineConfig } from 'astro/config'
import tailwindcss from '@tailwindcss/vite'

// Served at the domain root by default; override with SITE_BASE when hosted
// under a sub-path (e.g. '/Outlet-Front-/' on GitHub Pages project sites).
const base = process.env.SITE_BASE ?? '/'

export default defineConfig({
  site: process.env.SITE_URL ?? 'https://leroy-florian.github.io',
  base,
  trailingSlash: 'never',
  vite: {
    plugins: [tailwindcss()],
  },
  markdown: {
    shikiConfig: {
      theme: 'github-dark-default',
    },
  },
})
