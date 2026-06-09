// Prefixes internal hrefs with the configured base path so links keep working
// when the site is served under a sub-path (GitHub Pages project sites).
export function url(path: string): string {
  const base = import.meta.env.BASE_URL.replace(/\/$/, '')
  return `${base}${path === '/' ? '' : path}` || '/'
}
