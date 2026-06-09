---
title: Getting started
section: Introduction
order: 2
---

> **Early but functional** — The installation engine and the CLI are implemented and
> tested end to end: `init`, `add`, `list`, `remove`, `diff` and `update` all work. The
> first registry concern — **email** (`email-abstractions`, `email-smtp`,
> `email-sendgrid`) — is real, compiled and tested.
>
> What is **not done yet** is distribution: the `outlet` tool is not published on NuGet
> yet, and no public registry is hosted. Until then you run the CLI from source and
> point it at your own registry source.

## Requirements

- **.NET 10 SDK** (the projects target .NET 10 / C# 14).
- **Node 22+** only if you want to build this documentation site or the frontend
  packages.

## Run the CLI from source

Until the global tool is published, run the CLI straight from the repository:

```bash
# List the items available across the configured registries
dotnet run --project src/Outlet.Cli -- list

# Initialize outlet.json from the detected project/solution
dotnet run --project src/Outlet.Cli -- init

# Copy an item (and its dependencies) into the project
dotnet run --project src/Outlet.Cli -- add email-smtp
```

Other commands that already work:

```bash
outlet remove <item>   # delete an installed item's files, clean unused NuGet packages
outlet diff <item>     # show how your local copy differs from the registry version
outlet update <item>   # update an item, preserving local edits (conflicts → <file>.outlet-new)
```

## The mental model

1. You pick a **concern** (today: email).
2. `init` writes `outlet.json` (routing + lockfile) from your detected project.
3. `add` the **contract** (the generic port + DTOs) — zero external dependencies — then an
   **adapter** for the provider you want (for example `email-smtp` or `email-sendgrid`).
   Outlet resolves dependencies, rewrites namespaces with Roslyn, writes the files at the
   routed target, and adds the NuGet packages (CPM-aware).
4. You wire it up with the adapter's `AddXxx()` extension in your composition root.
5. To switch providers later, `add` a different adapter and change one `AddXxx()` line —
   the port stays the same. Because you own the code, `diff`/`update` keep your local
   edits when a new registry version lands.

## Build everything locally

```bash
dotnet build Outlet.slnx -c Release          # 0 warnings expected
dotnet test Outlet.slnx --filter "Category!=Live"
```

For the project conventions and architecture rules, see the
[Testing strategy](/docs/testing) and [Production readiness](/docs/production-readiness) pages.
