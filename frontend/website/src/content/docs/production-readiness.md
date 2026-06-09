---
title: Production readiness
section: Contributing
order: 4
---

Prove the adapters hold up under intensity **without a real production** — provoke the
failure modes deliberately, hermetically, reproducibly. A big prod only reveals defects by
accident; we cause them on purpose in tests.

## What actually breaks at scale (platform usage, not business logic)

- `HttpClient`/`SmtpClient` misuse → **socket exhaustion**;
- no timeout → hangs; `CancellationToken` ignored → no backpressure;
- sync-over-async / per-call allocations → throughput collapse;
- not thread-safe as a singleton → corruption under concurrency;
- 429 / `Retry-After` swallowed.

## Checklist (gate, per adapter)

| Check | SMTP (`SmtpEmailSender`) | SendGrid (`SendGridEmailSender`) | Covered by |
|---|---|---|---|
| Resource lifecycle, no socket exhaustion | `SmtpClient` created per send and disposed (`using`) | `SendGridClient` created per send (SDK owns the `HttpClient`) | concurrency test (50 parallel, delivered == 50) |
| Timeouts + cancellation honored | `CancellationToken` passed to every MailKit call; OCE propagates | token passed to `SendEmailAsync`; OCE propagates | conformance `Should_HonorCancellation` |
| Typed error mapping (no leaking exceptions) | connect/protocol errors → `EmailResult.Failure` | non-2xx → `EmailResult.Failure` | conformance reject + unreachable test |
| 429 / Retry-After surfaced | n/a (SMTP) | status + `Retry-After` included in the failure message | `SendGrid_Should_SurfaceRateLimit_When_429WithRetryAfter` |
| Stateless / concurrency-safe | no shared mutable state | no shared mutable state | concurrency test |
| Thin adapter (resilience composed over, not embedded) | ✅ | ✅ | design principle |

## Hermetic tests (no prod)

- **Concurrency** — `Smtp_Should_HandleManyConcurrentSends_WithoutLossOrCorruption`: 50
  parallel `SendAsync` against the in-process SMTP server; all succeed, each delivered
  exactly once (no deadlock, no corruption, sockets opened+closed cleanly).
- **Graceful degradation** — `Smtp_Should_DegradeGracefully_When_ServerUnreachable`:
  unreachable endpoint → fast typed failure, not a hang or a throw.
- **Throttling** — `SendGrid_Should_SurfaceRateLimit_When_429WithRetryAfter`: a 429 with
  `Retry-After` becomes a typed failure that carries the hint.

## Design principle

**Adapters stay thin.** Resilience (retry / circuit breaker) is a *separate* concern (Polly,
Linear HIJ-505) **composed over** the port — never embedded in an adapter, so swappability is
preserved.

## Honest limit + safety nets

The provider's true behaviour at scale (real rate-limit tails, latency) is only proven live →
**nightly sandbox** lane + dogfooding (Outlet uses its own adapters for its infra).

## Deferred (→ Suite)

Throughput/allocation benchmarks (**BenchmarkDotNet** alloc/call, **NBomber**
throughput/percentiles), long-running soak, historized perf dashboards, advanced port
observability.
