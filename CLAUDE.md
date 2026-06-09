# Outlet Platform — Guide projet (CLAUDE.md)

> Repo **privé** = **monorepo de tout le back Outlet**. La CLI `outlet` et le moteur de registre (`Outlet.Core.*`) restent **publics** dans le repo `Outlet-CLI` ; la frontière back ↔ CLI est **HTTP/JSON uniquement** (aucun binaire partagé — la CLI garde sa propre copie du Kernel). Ce repo héberge : registres privés hébergés, organisations, identité/accès, abonnement/essai, le **CRM**, le contenu registre dogfoodé (`registry/` + pipeline `tools/registry/`) et les **frontends** (`frontend/website` site Astro, `frontend/crm` dashboard React).

## Ce qu'est Outlet Cloud

Le **service backend hébergé** d'Outlet. Là où la CLI et le code copié restent **offline et sans dépendance**, Outlet Cloud ajoute ce qui ne peut vivre que côté serveur :

- **Registres privés** : héberger un catalogue d'items privé à une organisation (`PublishedItem`), poussé/consommé via l'API authentifiée.
- **Organisations & membres** : `Organization`, `Membership`, rôles, politique d'accès au registre.
- **Identité / accès** : `User` + `PersonalAccessToken` (la CLI s'authentifie via `outlet login` → token ; l'API décide des droits à **chaque** requête).
- **Abonnement / essai** (à venir, voir Roadmap) : agrégat `Subscription` + VO `TrialPeriod`, entitlements décidés 100 % serveur.

**Pilier d'archi non négociable** : l'autorisation est **100 % serveur**. Le code copié et la CLI core ne dépendent jamais du cloud — l'essai/abonnement ne touche jamais l'ADN d'Outlet (ownership, copier-coller, zéro dépendance runtime).

## Contextes bornés (bounded contexts)

Le back est découpé en trois contextes isolés (cf. `BoundedContextIsolationTests`) — ils communiquent par **id / strings**, jamais en important les types de l'autre :

- **Identity** (`Outlet.Identity.*`, `src/Identity/`) — utilisateurs + personal access tokens. Les scopes traversent la frontière en `string`, jamais en type Cloud.
- **Cloud** (`Outlet.Cloud.*`, `src/Cloud/`) — organisations, memberships, registres publiés, abonnement.
- **Crm** (`Outlet.Crm.*`, `src/Crm/`) — prospects, paiements, produits, métriques.

Les **hosts** (`src/Hosts/`) sont les composition roots : seuls endroits où les contextes sont câblés ensemble (`Outlet.Cloud.Web` compose Identity + Cloud ; `Outlet.Crm.Web` compose le Crm). Identity émet un user → `Guid` → Cloud le reçoit comme `MemberUserId.From(guid)` ; le host fait le pont via primitives.

## Architecture technique (hexagonal + DDD)

### Layering (vérifié par `LayeredArchitectureTests`)

| Couche | Peut dépendre de |
|---|---|
| `Outlet.Kernel.Shared` | rien (jamais d'une couche métier) — **LE kernel unique** du back |
| `Outlet.{Identity,Cloud,Crm}.Domain` | Kernel uniquement |
| `Outlet.{Identity,Cloud,Crm}.Application` | son Domain + Kernel |
| `Outlet.{Identity,Cloud,Crm}.Infrastructure` | son Application/Domain + Kernel |
| `src/Hosts/*` | plusieurs contextes (composition roots) |

### Langage du domaine

- **Agrégats** : `Organization` (membres, slug, politique d'accès), `PublishedItem` (registre privé), `User`, `PersonalAccessToken`. À venir : `Subscription`.
- **Value Objects** : `OrganizationId/Name/Slug`, `MemberUserId`, `PublishedItemId`, `RegistryItemName`, `EmailAddress`, `UserId`, `TokenScope`, `TokenHash` — `sealed`, ctor privé + factory `From(...)`/`Create(...)` qui valide.
- **Ports (Application/Ports/)** : `IOrganizationRepository`, `IPublishedItemRepository`, … Les ports acceptent des VOs, pas des primitives (Tell, Don't Ask).
- **Use cases** : `{Action}{Entity}UseCase` implémentant `IUseCase<TCommand[, TResult]>`, retournent `Result`/`Result<T>`.
- **Domain events** : `{Entity}{Action}Event` (ex. `OrganizationCreatedEvent`, `MemberAddedEvent`).

### Règles par couche (toutes vérifiées par les tests d'architecture)

**Domain** :
- AUCUNE dépendance technique : pas de HTTP, JSON, DB, logging (`TechnicalDependencyTests`).
- Pas de `DateTime.Now`/`UtcNow` → injecter `ICurrentDateTimeProvider` (`DateTimeProviderConventionTests`).
- Synchrone uniquement, classes `sealed`, pas de setters publics, IDs fortement typés (`SealedAndSynchronousDomainTests`).
- Agrégats : ctor privé + factory statique `Create` retournant l'agrégat ou `Result<T>` (`DddAggregateTests`).
- Les agrégats se référencent par ID, jamais par objet.
- Exceptions domaine `sealed`, suffixe `Exception`.

**Application** :
- Mêmes interdits techniques que Domain (HTTP/JSON/DB/logging).
- Use cases : jamais d'exception pour une erreur métier → `Result`/`Result<T>` (`UseCaseConventionTests`).
- Commands/queries = records immuables.
- Pas de logique métier (déléguer au Domain).

**Infrastructure** :
- Implémente les ports, `sealed`, primary constructors.
- C'est ICI (et seulement ici) que vivent EF Core, Npgsql, ASP.NET Identity.

**Web** :
- Composition root : DI explicite (`AddMediator()`, `AddHandlersFromAssembly(...)`, `AddCloudWeb()`).
- Mince : endpoint → `IMediator` → mappe `Result` vers HTTP status + body. Autorisation à chaque requête.

### Style C# (build = gate)

- **.NET 10, C# 14**, `ImplicitUsings`, `Nullable`, `EnforceCodeStyleInBuild` (Directory.Build.props).
- **Collection expressions obligatoires** : IDE0300→0306 en `error` (.editorconfig). `[.. xs.Where(...)]`, jamais `.ToList()` assigné.
- **Primary constructors obligatoires hors Domain** (`PrimaryConstructorConventionTests`). Opt-out rare : commentaire `// non-primary: <raison>` au-dessus du ctor. Ctors privés/protégés (factories) exemptés.
- **CPM** : toute version de package vit dans `Directory.Packages.props`, jamais dans un csproj.
- Interfaces préfixées `I`, naming vérifié par `NamingConventionTests`.

## Stratégie de tests

- **Fakes écrits main uniquement** — AUCUN framework de mock (pas de Moq/NSubstitute).
- **Nommage** : `Should_<Effet>_When_<Condition>`. Pas de commentaires Given/When/Then — séparation visuelle par lignes vides.
- **Persistance** : tests hermétiques sur SQLite in-memory (provider EF), jamais de vraie base en PR.
- **API** : intégration in-process via `Microsoft.AspNetCore.Mvc.Testing` (`Outlet.Cloud.Web.IntegrationTests`).
- **Lane CI** : hermétique et rapide, sans secret (`dotnet test --filter "Category!=Live"`).
- **Barre qualité** : ≥ 90 % de couverture Domain+Application ; mutation score Stryker ≥ 80 (seuils 80/60/50, `stryker-config.json`). Un BC inachevé est une régression.
- **Tests d'architecture** = non négociables : ils encodent ce document. Si un test d'archi gêne, on discute de la règle, on ne contourne pas le test.

## Conventions de nommage

- Port : `IOrganizationRepository` · impl : `EfCore{Entity}Repository` · event : `{Entity}{Action}Event` · ID : `{Entity}Id`.
- Use case : `{Action}{Entity}UseCase`.

## Roadmap — Abonnement / essai (Subscription)

Modèle retenu : **essai Pro frictionless, temps limité** (pas de free tier permanent).

- Accès complet au Pro pendant l'essai (**14 jours**, paramétrable), **sans carte**, inscription email → essai immédiat.
- Machine à états (un seul agrégat `Subscription`, VO `TrialPeriod`) :

```
Trialing ──convert──▶ Active
   │
   └──expire──▶ Suspended (lecture seule, données conservées ~30 j)
                   │
                   ├──paye──▶ Active   (réactivation instantanée)
                   └──N jours──▶ Expired (purge, après préavis)
```

- **Entitlements** partagés entre essai et plan payant (`can_host_private_registry`, `max_private_items`, `analytics`…) ; seules les valeurs changent. Quand le vrai billing arrive (Stripe…), rien à arracher.
- **Anti-abus = politique remplaçable** (`ITrialEligibilityPolicy`), démarrage en *soft enforcement* (email vérifié, blocage domaines jetables, 1 essai/domaine, rate-limit). On resserre la clé quand l'ICP sera connu — la machine à états et les entitlements ne bougent pas.
- **Impact CLI (repo public, minimal)** : `outlet login`/`whoami` (plan + jours restants), lecture du statut d'entitlement, messages d'erreur clairs (`outlet add` vers registre privé en essai expiré → message + exit code non nul).
- Cycle de vie e-mails : J0 bienvenue · J-3/J-1 rappels · jour J expiration · préavis de purge.

### État d'implémentation (branchement actuel)

- **Ancrage compte** : `Subscription` est porté par un `AccountId` (= l'utilisateur Identity, croisé en GUID). Un registre privé d'org est hébergé **sous le plan de son Owner** (`Organization.OwnerId`).
- **Inscription** (`/auth/register`) démarre l'essai 14 j (frictionless). **`/billing/subscribe`** → `Convert` (Active), **`/billing/cancel`** → `Cancel` (Suspended, lecture seule).
- **Autorisation serveur** via `SubscriptionEntitlementResolver` (expiration paresseuse) : l'UI de gestion (`/organizations/*`) exige un abonnement en cours (402 sinon) ; `PublishItemUseCase` gate sur le plan de l'Owner + quota `MaxPrivateItems` ; le pull PAT (`registry.json`) reste lisible en Suspended, bloqué en Expired.
- L'ancien flag `UserPlan` (Identity) a été **supprimé** : Identity reste découplé du billing.

## Garde-fous pour toute contribution

- **Autorisation 100 % serveur** : aucune décision d'entitlement côté CLI.
- Ne jamais coupler les contextes bornés (Identity ↔ Cloud passent par id/string).
- **Zéro dette** : un finding est corrigé dans la session ou explicitement différé avec ticket. Un refactor inachevé est une régression.

## Commandes utiles

```bash
dotnet build Outlet.Platform.slnx -c Release       # 0 warning exigé (IDE03xx = erreurs)
dotnet test Outlet.Platform.slnx --filter "Category!=Live"
dotnet stryker                                      # mutation testing (stryker-config.json)

cd frontend/website && npm ci && npm run build      # site Astro
cd frontend/crm     && npm ci && npm run build      # dashboard CRM
./tools/registry/publish.sh                         # build + publish du registre produit
```
