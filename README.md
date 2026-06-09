# Outlet Platform

Monorepo **privé** du backend d'[Outlet](https://github.com/Leroy-Florian/Outlet-CLI). La CLI `outlet` et le moteur de registre restent publics dans `Outlet-CLI` ; ce repo contient tout ce qui vit côté serveur — plus le site vitrine et le dashboard CRM. La frontière back ↔ CLI est **HTTP/JSON uniquement** (aucun binaire partagé).

## Périmètre

- **Identity** (`src/Identity`) : utilisateurs + personal access tokens.
- **Cloud** (`src/Cloud`) : organisations & membres, registres privés hébergés, abonnement/essai (`Subscription`, entitlements 100 % serveur).
- **CRM** (`src/Crm`) : suivi prospects, paiements, produits, métriques.
- **Hosts** (`src/Hosts`) : composition roots ASP.NET — seul endroit où les contextes sont câblés ensemble.
- **Frontends** (`frontend/website` site Astro, `frontend/crm` dashboard React).

L'autorisation est **entièrement serveur** : la CLI s'authentifie (`outlet login` → token), l'API décide des droits à chaque requête.

## Architecture

Hexagonal + DDD strict, verrouillé par les **tests d'architecture** (`tests/Outlet.ArchitectureTests`) : layering, isolation des contextes bornés (Identity ⊥ Cloud ⊥ Crm, hors Hosts), conventions Domain/Application/Infrastructure, primary constructors, naming. Voir [`CLAUDE.md`](CLAUDE.md).

```
src/Kernel.Shared/Outlet.Kernel.Shared/   LE kernel unique (Result, Mediator, ValueObject…)
src/Identity/Outlet.Identity.{Domain,Application,Infrastructure}
src/Cloud/Outlet.Cloud.{Domain,Application,Infrastructure}
src/Crm/Outlet.Crm.{Domain,Application,Infrastructure}
src/Hosts/{Outlet.Cloud.Web,Outlet.Crm.Web}
tests/                                    unit + integration + architecture tests
frontend/{website,crm}
```

## Développement

```bash
dotnet build Outlet.Platform.slnx -c Release        # 0 warning exigé
dotnet test  Outlet.Platform.slnx --filter "Category!=Live"
dotnet stryker                                       # mutation testing

cd frontend/website && npm ci && npm run dev         # site (Astro)
cd frontend/crm     && npm ci && npm run dev         # dashboard CRM
```

Requiert le SDK **.NET 10** et Node 22.
