# Outlet Constitution

## Core Principles

### I. Ownership / Copier-coller d'abord
Tout code distribué par le registre appartient à l'utilisateur : il est copié dans son repo, éditable librement, et ne crée **aucune dépendance runtime vers Outlet**. Désinstaller Outlet ne casse rien. Outlet n'est pas une librairie — c'est du code qu'on s'approprie.

### II. Pureté des ports
Le contrat (port + DTOs) a **zéro dépendance externe** et reste **minimal et identique** entre adapters — c'est la garantie de swappabilité. Aucune spécificité provider dans un port générique : une feature provider passe par une seconde interface dédiée ou par l'édition de la copie. Côté engine, le hexagone (Domain + Application) ne touche jamais HTTP, JSON, DB, logging, Roslyn ni MSBuild — ces préoccupations vivent derrière des ports, implémentées en Infrastructure (vérifié par `TechnicalDependencyTests`).

### III. Discipline de scope (NON-NÉGOCIABLE)
Une préoccupation à la fois. **Cible v1 = email uniquement** (1 contrat + 2 adapters). Aucun élargissement tant que la tranche v1 n'est pas propre et livrable. Zéro dette : un finding est corrigé dans la session ou explicitement différé avec ticket Linear ; un refactor inachevé est une régression.

### IV. Barre qualité tests
TDD ; tests sociables avec **fakes écrits main** (aucun framework de mock) ; nommage `Should_X_When_Y` ; lane PR hermétique et sans secret (`--filter "Category!=Live"`), lane live nightly non bloquante ; suite de conformité de port rejouée par chaque adapter ; ≥ 90 % de couverture Domain+Application ; mutation Stryker seuils 80/60/50. Tout item registre compile et est testé — **le manifeste ne ment jamais**.

### V. Hexagonal + DDD outillés
Layering strict `Kernel ← Domain ← Application ← Infrastructure ← Cli` ; agrégats sealed à factory `Create` retournant `Result` ; Value Objects validés à la construction ; IDs fortement typés ; erreurs métier via `Result`/`Result<T>` (jamais d'exception métier) ; horloge via `ICurrentDateTimeProvider` ; primary constructors hors Domain ; collection expressions en erreurs de build. **Chaque règle est encodée dans `tests/Outlet.ArchitectureTests/`** — si une règle gêne, on amende la constitution, on ne contourne pas le test.

## Contraintes technologiques

- .NET 10 / C# 14, `Nullable` + `ImplicitUsings`, CPM (`Directory.Packages.props`), solution `.slnx`.
- CLI = `dotnet tool` (`outlet`) au-dessus d'un core engine réutilisable (System.CommandLine).
- Réécriture de namespace via Roslyn `CSharpSyntaxRewriter` (jamais find/replace).
- Preflight via valeurs évaluées MSBuild (`dotnet msbuild -getProperty/-getItem`), jamais le XML brut.
- Distribution : manifestes JSON `*.registry.json` + fichiers servis en HTTP, multi-sources.
- NuGet : deps directes uniquement, versions plancher, détection CPM.
- Frontend (playground futur) : npm workspaces, TypeScript strict, Vite, Vitest, Effect v3, `@outlet/hateoas` + `@outlet/effect-react`.

## Workflow de développement

- Cadrage par Spec Kit (`.specify/`) : spec → plan → tasks → implémentation ; le suivi vit dans Linear (team Hijoxx, projets « Outlet — MVP » et « Outlet — Suite / Roadmap »).
- Gates de merge : `dotnet build` 0 warning, tous les tests verts (dont les 36+ tests d'architecture), lint/test/build front verts.
- CI GitHub Actions : job dotnet + job frontend sur chaque PR ; le manifeste `dist/registry/` est généré et validé en CI, jamais édité à la main.

## Governance

Cette constitution prime sur toute autre pratique. Tout amendement est documenté (PR dédiée), justifié, et accompagné de la mise à jour des tests d'architecture qui l'encodent. Toute revue de PR vérifie la conformité ; la complexité doit être justifiée. `CLAUDE.md` est le guide d'exécution courant et doit rester cohérent avec ce document.

**Version**: 1.0.0 | **Ratified**: 2026-06-05 | **Last Amended**: 2026-06-05
