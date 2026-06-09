# Outlet Cloud

Backend hébergé d'[Outlet](https://github.com/Leroy-Florian/Outlet-CLI). Repo **privé** : la CLI `outlet` et le moteur de registre restent publics ; ce repo contient tout ce qui doit vivre côté serveur.

## Périmètre

- **Registres privés** hébergés par organisation (`Outlet.Cloud.Domain/Registry`).
- **Organisations & membres** (`Outlet.Cloud.Domain/Organizations`).
- **Identité / accès** : utilisateurs + personal access tokens (`Outlet.Identity.*`).
- **Abonnement / essai** (à venir) : agrégat `Subscription`, entitlements 100 % serveur.

L'autorisation est **entièrement serveur** : la CLI s'authentifie (`outlet login` → token), l'API décide des droits à chaque requête. Le code copié et la CLI core restent offline, sans dépendance runtime à Outlet.

## Architecture

Hexagonal + DDD strict, mêmes règles que le repo CLI, verrouillées par les **tests d'architecture** (`tests/Outlet.ArchitectureTests`) : layering, isolation des contextes bornés, conventions Domain/Application/Infrastructure, primary constructors, naming. Voir [`CLAUDE.md`](CLAUDE.md).

```
src/Kernel.Shared/            building blocks DDD partagés (copie depuis le repo CLI)
src/Outlet.Identity.{Domain,Application,Infrastructure}
src/Outlet.Cloud.{Domain,Application,Infrastructure,Web}
tests/                        unit + integration + architecture tests
```

## Développement

```bash
dotnet build Outlet.Cloud.slnx -c Release          # 0 warning exigé
dotnet test  Outlet.Cloud.slnx --filter "Category!=Live"
dotnet stryker                                      # mutation testing
```

Requiert le SDK **.NET 10**.
