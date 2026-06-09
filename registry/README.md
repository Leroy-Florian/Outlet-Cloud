# Registre interne Outlet-SSO

Items de registre **privés** servis à la CLI publique `outlet` comme une **source supplémentaire**.
On garde ici le code « copier-collable » lié à l'identité/SSO sans l'exposer dans le repo public de la CLI.

> Modèle Outlet : chaque préoccupation = un **port générique** (`outlet:contract`) + des
> **adapters interchangeables** (`outlet:adapter`). L'utilisateur **copie le code et le possède** —
> aucune dépendance runtime à Outlet. Cf. `registry-item.schema.json` pour le format de manifeste.

## Préoccupation `auth`

Authentification machine par **Personal Access Token** (bearer `Authorization: Bearer …`).
C'est la *colle de consommation* côté host : le bounded context Identity (agrégats, EF Core,
migrations) reste, lui, une **librairie/service** dont on dépend — il n'est volontairement pas
distribué en copier-coller.

| Item | Type | Contenu |
|---|---|---|
| `auth-pat-abstractions` | `outlet:contract` | `IPersonalAccessTokenAuthenticator`, `AuthenticatedToken`, `IPersonalAccessTokenStore`, `StoredAccessToken` — zéro dépendance externe. |
| `auth-pat-sha256` | `outlet:adapter` | `Sha256PersonalAccessTokenAuthenticator` + `TokenHashing` + Options + `AddSha256PersonalAccessTokenAuthentication()`. BCL uniquement (SHA-256, `TimeProvider`). |

### Brancher l'adapter

```csharp
// 1. ta persistance des tokens, projetée sur le port de lookup
services.AddScoped<IPersonalAccessTokenStore, MyTokenStore>();

// 2. une horloge (testable)
services.AddSingleton(TimeProvider.System);

// 3. l'adapter SHA-256
services.AddSha256PersonalAccessTokenAuthentication();
```

Swapper de stratégie de hash/validation = un autre adapter `auth-pat-*` derrière le même contrat.

## Build & publication

Outil unique : [`tools/registry/publish.sh`](../tools/registry/publish.sh).

```bash
# Plan AMORÇAGE (git-raw) : valide les manifestes + génère dist/registry/
#   (registry.json agrégé + fichiers), servable tel quel par le CLI.
./tools/registry/publish.sh build

# Plan DISTRIBUTION : build, puis pousse chaque item vers l'API Cloud
#   (login -> résolution org par slug -> POST /organizations/{id}/registry/items),
#   contrats avant adapters pour que les dépendances résolvent.
OUTLET_CLOUD_URL=https://cloud.outlet.dev \
OUTLET_CLOUD_EMAIL=ci@acme.test OUTLET_CLOUD_PASSWORD=*** OUTLET_ORG_SLUG=acme \
  ./tools/registry/publish.sh publish

./tools/registry/publish.sh publish --dry-run   # liste ce qui serait poussé, sans réseau
```

- `dist/registry/` est **committé** : c'est la source statique git-raw qui amorce les
  consommateurs (dont Cloud) *avant* qu'un serveur Cloud n'existe. La CI échoue si
  `dist/` est obsolète → relancer `build` et committer.
- La publication CI tourne via [`.github/workflows/publish-registry.yml`](../.github/workflows/publish-registry.yml)
  sur push de la branche par défaut. Secrets requis : `OUTLET_CLOUD_URL`,
  `OUTLET_CLOUD_EMAIL`, `OUTLET_CLOUD_PASSWORD`, `OUTLET_ORG_SLUG`.
- Le compte de service doit être **Owner/Admin** de l'org et au plan **Pro**
  (le management Cloud est une offre payante ; le registre public reste gratuit).
