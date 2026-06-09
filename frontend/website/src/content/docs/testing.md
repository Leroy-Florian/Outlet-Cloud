---
title: Testing strategy
section: Contributing
order: 3
---

High confidence on the adapters (the product's core) **without** slow CI. Two surfaces,
two treatments.

## 1. Tooling (Core + CLI)

Pure logic — resolution, Roslyn rewriting, NuGet/CPM editing, MSBuild detection, manifest
(de)serialization. **Fast, zero network**, hand-written fakes (no mocking framework). This is
where most correctness lives. Runs on every PR.

## 2. Adapters — three levels

| Level | What | When | How (here) |
|---|---|---|---|
| **A** | Provider HTTP boundary mocked | every PR | in-process HTTP stub (`TestHttpServer`) standing in for the SendGrid SDK endpoint |
| **B** | Real protocol via a local double | every PR | in-process SMTP server (`TestSmtpServer`) — real SMTP, no Docker |
| **C** | Real provider network | nightly / on-label | SendGrid sandbox, Mailtrap… secrets required, slow/flaky → quarantined |

> Testcontainers (smtp4dev / maildev / Redis / Azurite) is the canonical level-B tool where
> Docker is available. This repo's CI runner has no Docker, so level B uses **hand-rolled
> in-process doubles** — same goal (real protocol, hermetic), zero infrastructure.

## Port conformance suite

`EmailSenderConformanceTests` encodes "**every `IEmailSender` must behave this way**"
(accepts → success + delivered once; server rejects → typed failure, never throws; honors
cancellation). **Each adapter runs the same suite** against its own double
(`SmtpEmailSenderConformanceTests`, `SendGridEmailSenderConformanceTests`) — swappability is
*tested*, not asserted.

## CI lanes

- **PR lane** (`.github/workflows/ci.yml`, blocking, fast, hermetic):
  `dotnet test --filter "Category!=Live"` — tooling + levels A & B. No secrets, no external
  network. Also regenerates `dist/registry` and fails if it drifts.
- **Nightly lane** (`.github/workflows/nightly.yml`, non-blocking): `--filter "Category=Live"`
  against a provider sandbox, secrets exposed **only** here (never on fork PRs).

Mark live tests with `[Trait("Category", "Live")]`.

## TFM matrix (HIJ-509)

The registry sources are compiled against **every** declared `targetFramework`
(`net8.0`/`net9.0`/`net10.0`) by the multi-targeted `Outlet.Registry.Email.Compat`
project — so the manifest's compatibility claim is *verified at build*, not asserted.
It is part of the solution, so the PR lane's `dotnet build` covers it.

On the install side, `outlet add` runs a **TFM pre-check**
(`TargetFrameworkCompatibility`): before writing anything it compares each item's
`targetFrameworks` with the target project's evaluated `TargetFramework(s)` and refuses,
with a clear message, an item the project's framework cannot compile.
