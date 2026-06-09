---
title: What is Outlet?
section: Introduction
order: 1
---

Outlet is a **copy-paste registry of backend infrastructure for .NET** — the same idea
as [shadcn/ui](https://ui.shadcn.com/), but for backend concerns such as email, cache,
resilience and storage.

The core idea: for each **concern** we expose a **generic port** (a minimal business
interface) and several **interchangeable adapters** (one per provider or library). You
**copy the code into your repository and own it** — you can swap providers behind the
same port and edit the code freely.

> **This is not a library you take as a dependency.** It is code you make your own. The
> name is the metaphor: a *port* (the socket) into which you *plug* a provider (the
> adapter).

## Design principles

- **Ownership / copy-paste** — no runtime dependency on Outlet. Removing Outlet breaks
  nothing in your codebase.
- **Contract / adapter separation** — the contract (port + DTOs) has zero external
  dependencies and lives on the application/domain side; the adapter depends on the
  contract plus the provider library. Your app never references the concrete adapter.
- **Minimal, identical generic port** across adapters — this is what guarantees
  swappability. No provider specifics leak into the generic port.
- **Specifics next to the generic** — a provider-specific feature goes through a second,
  dedicated interface (same adapter class) or by editing your copy — never into the
  shared port.
- **Explicit DI** — every adapter ships an `AddXxx()` extension. No assembly scanning.
- **Thin adapters** — resilience (retry / circuit breaker) is a separate concern composed
  *on top of* the port, never baked into the adapter.

## Scope of v1

The first slice targets **email only**: one port (`IEmailSender`) and two adapters (for
example SMTP and SendGrid). The goal is that swapping providers comes down to changing a
single `AddXxx()` line.

## How it is distributed

- A **remote registry** (a manifest plus files served over HTTP), designed to be
  multi-source from day one so private company registries are possible later.
- A **CLI** (`outlet`, a global `dotnet tool`) built on a reusable core engine.
- An explicit JSON **manifest per item** (`*.registry.json`), validated and generated in
  CI, so the manifest never lies about what an item contains.

Continue to [Getting started](/docs/getting-started).
