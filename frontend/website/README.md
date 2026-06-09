# Outlet — Site vitrine

Site vitrine d'[Outlet](https://github.com/Leroy-Florian/Outlet-CLI) : promotion du
registre open source **et** de l'offre payante **Outlet Cloud**, avec la documentation
intégrée au même thème. Construit avec [Astro](https://astro.build/) +
[Tailwind CSS](https://tailwindcss.com/).

## Pages

- `/` — landing : hero, démo CLI/DI, principes, teaser Outlet Cloud
- `/pricing` — Open Source (gratuit) vs Outlet Cloud (early access) + FAQ
- `/cloud` — page produit Outlet Cloud avec formulaire d'early access
- `/docs/*` — documentation migrée depuis `Outlet-CLI/docs` (collection de contenu
  Astro, `src/content/docs/`)

## Développement

```shell
npm install
npm run dev      # serveur de dev avec HMR
npm run build    # build de production dans dist/
npm run preview  # prévisualisation du build
```

## Déploiement

`.github/workflows/deploy.yml` publie sur GitHub Pages à chaque push sur `main`
(`SITE_BASE=/Outlet-Front-/`). Pour le paywall et l'onboarding d'Outlet Cloud, le site
est prêt à passer en SSR : ajouter un adapter Astro (Vercel/Netlify/Node) et migrer le
formulaire d'early access vers une vraie route API.
