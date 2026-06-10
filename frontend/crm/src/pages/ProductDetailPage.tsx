import { useMemo, useState } from "react"
import { Link, useParams } from "react-router-dom"
import {
  captureSnapshots,
  getAnalyticsSummary,
  getDailyDownloads,
  getDailyTraffic,
  getRepositoryHistory,
  listProducts,
  trackPackage,
  trackRepository,
  untrackPackage,
  untrackRepository,
  type PackageRegistry,
  type ProductDto,
} from "../api/client"
import { useQuery } from "../hooks/useQuery"
import {
  EmptyState,
  ErrorBanner,
  Loading,
  PeriodSelector,
  StatCard,
  TrendBadge,
  formatNumber,
  lastDaysRange,
} from "../components/ui"
import {
  DailyDownloadsChart,
  DailyTrafficChart,
  RepositoryHistoryChart,
} from "../components/charts"
import { HealthCard } from "../components/HealthCard"

const RepositoryHistorySection = ({
  productId,
  repository,
}: {
  productId: string
  repository: string
}) => {
  const history = useQuery(
    () => getRepositoryHistory(productId, repository),
    [productId, repository],
  )

  return (
    <div className="card section">
      <h2 className="card-title">Historique GitHub — {repository}</h2>
      {history.loading ? (
        <Loading />
      ) : history.error !== null ? (
        <ErrorBanner message={history.error} />
      ) : history.data === null || history.data.length === 0 ? (
        <EmptyState title="Aucun snapshot GitHub">
          Cliquez sur « Capturer un snapshot » pour commencer à suivre stars et issues.
        </EmptyState>
      ) : (
        <RepositoryHistoryChart history={history.data} />
      )}
    </div>
  )
}

const TrackingSection = ({
  product,
  latestVersions,
  onChanged,
  onError,
}: {
  product: ProductDto
  /** Dernière version connue par package ("Registry:packageId"), issue du résumé analytics. */
  latestVersions: ReadonlyMap<string, string>
  onChanged: () => void
  onError: (message: string) => void
}) => {
  const [registry, setRegistry] = useState<PackageRegistry>("NuGet")
  const [packageId, setPackageId] = useState("")
  const [repository, setRepository] = useState("")
  const [busy, setBusy] = useState(false)

  const run = async (action: () => Promise<unknown>, after?: () => void) => {
    setBusy(true)
    try {
      await action()
      after?.()
      onChanged()
    } catch (e) {
      onError(e instanceof Error ? e.message : String(e))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="two-col section">
      <div className="card">
        <h2 className="card-title">Packages suivis</h2>
        {product.packages.length === 0 ? (
          <EmptyState title="Aucun package suivi">
            Ajoutez un package NuGet ou npm pour suivre ses téléchargements.
          </EmptyState>
        ) : (
          <div className="chip-list">
            {product.packages.map((pkg) => (
              <span className="chip" key={`${pkg.registry}:${pkg.packageId}`}>
                <span className="badge badge-blue">{pkg.registry}</span>
                {pkg.packageId}
                {latestVersions.has(`${pkg.registry}:${pkg.packageId}`) ? (
                  <span className="badge badge-violet" title="Dernière version publiée">
                    v{latestVersions.get(`${pkg.registry}:${pkg.packageId}`)}
                  </span>
                ) : null}
                <button
                  title="Ne plus suivre"
                  disabled={busy}
                  onClick={() =>
                    void run(() => untrackPackage(product.id, pkg.registry, pkg.packageId))
                  }
                >
                  ✕
                </button>
              </span>
            ))}
          </div>
        )}
        <div className="form-row" style={{ marginTop: 14 }}>
          <div className="field">
            <label>Registre</label>
            <select value={registry} onChange={(e) => setRegistry(e.target.value as PackageRegistry)}>
              <option value="NuGet">NuGet</option>
              <option value="Npm">npm</option>
            </select>
          </div>
          <div className="field" style={{ flex: 1 }}>
            <label>Package</label>
            <input
              value={packageId}
              onChange={(e) => setPackageId(e.target.value)}
              placeholder="Outlet.Cli"
            />
          </div>
          <button
            className="btn btn-primary"
            disabled={busy || packageId.trim() === ""}
            onClick={() =>
              void run(
                () => trackPackage(product.id, registry, packageId.trim()),
                () => setPackageId(""),
              )
            }
          >
            Ajouter
          </button>
        </div>
      </div>

      <div className="card">
        <h2 className="card-title">Repositories suivis</h2>
        {product.repositories.length === 0 ? (
          <EmptyState title="Aucun repository suivi">
            Ajoutez un repository GitHub (owner/nom) pour suivre stars et issues.
          </EmptyState>
        ) : (
          <div className="chip-list">
            {product.repositories.map((repo) => (
              <span className="chip" key={repo}>
                {repo}
                <button
                  title="Ne plus suivre"
                  disabled={busy}
                  onClick={() => void run(() => untrackRepository(product.id, repo))}
                >
                  ✕
                </button>
              </span>
            ))}
          </div>
        )}
        <div className="form-row" style={{ marginTop: 14 }}>
          <div className="field" style={{ flex: 1 }}>
            <label>Repository (owner/nom)</label>
            <input
              value={repository}
              onChange={(e) => setRepository(e.target.value)}
              placeholder="outlet-dev/outlet-cli"
            />
          </div>
          <button
            className="btn btn-primary"
            disabled={busy || !repository.includes("/")}
            onClick={() =>
              void run(
                () => trackRepository(product.id, repository.trim()),
                () => setRepository(""),
              )
            }
          >
            Ajouter
          </button>
        </div>
      </div>
    </div>
  )
}

export const ProductDetailPage = () => {
  const { productId } = useParams<{ productId: string }>()
  const id = productId ?? ""

  const products = useQuery(listProducts, [])
  const product = products.data?.find((p) => p.id === id) ?? null

  const [days, setDays] = useState(30)
  const range = useMemo(() => lastDaysRange(days), [days])
  const summary = useQuery(() => getAnalyticsSummary(id, days), [id, days])
  const downloads = useQuery(() => getDailyDownloads(id, range.from, range.to), [id, range])
  const traffic = useQuery(() => getDailyTraffic(id, range.from, range.to), [id, range])

  const [capturing, setCapturing] = useState(false)
  const [captureNotice, setCaptureNotice] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const reloadAnalytics = () => {
    summary.reload()
    downloads.reload()
    traffic.reload()
  }

  const handleCapture = async () => {
    setCapturing(true)
    setCaptureNotice(null)
    setActionError(null)
    try {
      const reports = await captureSnapshots(id)
      const ok = reports.filter((r) => r.succeeded).length
      const failed = reports.filter((r) => !r.succeeded)
      if (failed.length > 0) {
        setActionError(
          `Snapshot partiel : ${failed.map((f) => `${f.target} (${f.error ?? "erreur"})`).join(", ")}`,
        )
      }
      setCaptureNotice(`Snapshot capturé : ${ok}/${reports.length} source(s) mises à jour.`)
      reloadAnalytics()
    } catch (e) {
      setActionError(e instanceof Error ? e.message : String(e))
    } finally {
      setCapturing(false)
    }
  }

  if (products.loading) {
    return <Loading />
  }
  if (product === null) {
    return (
      <>
        <ErrorBanner message="Produit introuvable." />
        <Link className="btn" to="/produits">
          ← Retour aux produits
        </Link>
      </>
    )
  }

  const trafficIsEmpty = traffic.data !== null && traffic.data.totalPageViews === 0

  return (
    <>
      <header className="page-header">
        <div>
          <h1 className="page-title">{product.name}</h1>
          <p className="page-subtitle">{product.description ?? "Analytics produit"}</p>
        </div>
        <div style={{ display: "flex", gap: 16, alignItems: "center" }}>
          <PeriodSelector value={days} onChange={setDays} />
          <button className="btn btn-primary" disabled={capturing} onClick={() => void handleCapture()}>
            {capturing ? "Capture en cours…" : "Capturer un snapshot"}
          </button>
        </div>
      </header>

      {captureNotice !== null ? <div className="notice-banner">{captureNotice}</div> : null}
      {actionError !== null ? <ErrorBanner message={actionError} /> : null}

      {summary.loading ? (
        <Loading />
      ) : summary.error !== null ? (
        <ErrorBanner message={summary.error} />
      ) : summary.data !== null ? (
        <div className="card-grid">
          <StatCard
            label="Téléchargements totaux"
            value={formatNumber(summary.data.totalDownloads)}
          />
          <StatCard
            label={`Téléchargements ${summary.data.periodDays} j`}
            value={
              <>
                {formatNumber(summary.data.downloads.currentPeriod)}{" "}
                <TrendBadge comparison={summary.data.downloads} />
              </>
            }
            hint={`7 j : ${formatNumber(summary.data.downloadsLast7Days)} · 30 j : ${formatNumber(summary.data.downloadsLast30Days)}`}
          />
          <StatCard
            label={`Trafic ${summary.data.periodDays} j`}
            value={
              <>
                {formatNumber(summary.data.pageViews.currentPeriod)}{" "}
                <TrendBadge comparison={summary.data.pageViews} />
              </>
            }
            hint={`7 j : ${formatNumber(summary.data.pageViewsLast7Days)} · 30 j : ${formatNumber(summary.data.pageViewsLast30Days)}`}
          />
          <StatCard
            label="Stars GitHub"
            value={formatNumber(summary.data.repositories.reduce((s, r) => s + r.stars, 0))}
          />
          <StatCard
            label="Issues ouvertes"
            value={formatNumber(summary.data.repositories.reduce((s, r) => s + r.openIssues, 0))}
          />
        </div>
      ) : null}

      <HealthCard productId={product.id} />

      <div className="card section">
        <h2 className="card-title">Nouveaux téléchargements par jour ({days} j, par source)</h2>
        {downloads.loading ? (
          <Loading />
        ) : downloads.error !== null ? (
          <ErrorBanner message={downloads.error} />
        ) : downloads.data === null || downloads.data.totalDownloads === 0 ? (
          <EmptyState title="Aucun téléchargement sur la période">
            Suivez un package puis capturez des snapshots réguliers : les deltas quotidiens
            apparaîtront ici.
          </EmptyState>
        ) : (
          <DailyDownloadsChart report={downloads.data} />
        )}
      </div>

      <div className="card section">
        <h2 className="card-title">Trafic quotidien ({days} j)</h2>
        {traffic.loading ? (
          <Loading />
        ) : traffic.error !== null ? (
          <ErrorBanner message={traffic.error} />
        ) : traffic.data === null || trafficIsEmpty ? (
          <EmptyState title="Aucun trafic enregistré">
            Intégrez le beacon sur votre site (POST /api/traffic) pour suivre les pages vues.
          </EmptyState>
        ) : (
          <>
            <DailyTrafficChart days={traffic.data.days} />
            <div className="two-col" style={{ marginTop: 16 }}>
              <div>
                <h3 className="card-title">Top pages</h3>
                <ul className="rank-list">
                  {traffic.data.topPaths.map((p) => (
                    <li key={p.key}>
                      <span className="key">{p.key}</span>
                      <span className="count">{formatNumber(p.count)}</span>
                    </li>
                  ))}
                </ul>
              </div>
              <div>
                <h3 className="card-title">Top sources (referrers)</h3>
                {traffic.data.topReferrers.length === 0 ? (
                  <p className="muted">Aucun referrer enregistré.</p>
                ) : (
                  <ul className="rank-list">
                    {traffic.data.topReferrers.map((r) => (
                      <li key={r.key}>
                        <span className="key">{r.key}</span>
                        <span className="count">{formatNumber(r.count)}</span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </div>
          </>
        )}
      </div>

      {product.repositories.map((repo) => (
        <RepositoryHistorySection key={repo} productId={product.id} repository={repo} />
      ))}

      <TrackingSection
        product={product}
        latestVersions={
          new Map(
            (summary.data?.packages ?? [])
              .filter((pkg) => pkg.latestVersion !== null)
              .map((pkg) => [`${pkg.registry}:${pkg.packageId}`, pkg.latestVersion as string]),
          )
        }
        onChanged={() => {
          products.reload()
          reloadAnalytics()
        }}
        onError={setActionError}
      />
    </>
  )
}
