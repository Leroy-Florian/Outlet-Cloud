import { useEffect, useMemo, useState } from "react"
import { Link, useNavigate } from "react-router-dom"
import {
  acknowledgeAlert,
  getDailyDownloads,
  getDailyTraffic,
  getObjectivesProgress,
  getPortfolio,
  listAlerts,
  listPayments,
  listProducts,
  listProspects,
} from "../api/client"
import { useQuery } from "../hooks/useQuery"
import {
  AlertTypeBadge,
  EmptyState,
  ErrorBanner,
  Loading,
  ObjectiveProgressBar,
  PeriodSelector,
  StatCard,
  TrendBadge,
  formatDateTime,
  formatMoney,
  formatNumber,
  lastDaysRange,
  objectiveMetricLabel,
} from "../components/ui"
import { DailyDownloadsChart, DailyTrafficChart } from "../components/charts"

export const DashboardPage = () => {
  const products = useQuery(listProducts, [])
  const prospects = useQuery(listProspects, [])
  const payments = useQuery(listPayments, [])
  const alerts = useQuery(() => listAlerts({ acknowledged: false }), [])
  const objectives = useQuery(() => getObjectivesProgress(), [])

  const [acknowledging, setAcknowledging] = useState<string | null>(null)
  const [alertError, setAlertError] = useState<string | null>(null)

  const handleAcknowledge = async (alertId: string) => {
    setAcknowledging(alertId)
    setAlertError(null)
    try {
      await acknowledgeAlert(alertId)
      alerts.reload()
    } catch (e) {
      setAlertError(e instanceof Error ? e.message : String(e))
    } finally {
      setAcknowledging(null)
    }
  }

  const [days, setDays] = useState(30)
  const [selectedId, setSelectedId] = useState("")
  const navigate = useNavigate()

  const portfolio = useQuery(() => getPortfolio(days), [days])

  useEffect(() => {
    if (selectedId === "" && products.data !== null && products.data.length > 0) {
      setSelectedId(products.data[0]?.id ?? "")
    }
  }, [products.data, selectedId])

  const range = useMemo(() => lastDaysRange(days), [days])
  const downloads = useQuery(
    () =>
      selectedId === ""
        ? Promise.resolve(null)
        : getDailyDownloads(selectedId, range.from, range.to),
    [selectedId, range],
  )
  const traffic = useQuery(
    () =>
      selectedId === ""
        ? Promise.resolve(null)
        : getDailyTraffic(selectedId, range.from, range.to),
    [selectedId, range],
  )

  const productName = (id: string) => products.data?.find((p) => p.id === id)?.name ?? id

  const openProspects = (prospects.data ?? []).filter(
    (p) => p.stage !== "Won" && p.stage !== "Lost",
  )
  const settledTotal = (payments.data ?? [])
    .filter((p) => p.status === "Settled" && p.currency === "EUR")
    .reduce((sum, p) => sum + p.amount, 0)

  const recentProspects = [...(prospects.data ?? [])]
    .sort((a, b) => b.createdAt.localeCompare(a.createdAt))
    .slice(0, 5)
  const recentPayments = [...(payments.data ?? [])]
    .sort((a, b) => b.createdAt.localeCompare(a.createdAt))
    .slice(0, 5)

  return (
    <>
      <header className="page-header">
        <div>
          <h1 className="page-title">Dashboard</h1>
          <p className="page-subtitle">Vue d'ensemble des produits Outlet.</p>
        </div>
        <div style={{ display: "flex", gap: 16, alignItems: "flex-end" }}>
          <PeriodSelector value={days} onChange={setDays} />
          {(products.data ?? []).length > 0 ? (
            <div className="field">
              <label>Produit analysé</label>
              <select value={selectedId} onChange={(e) => setSelectedId(e.target.value)}>
                {(products.data ?? []).map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name}
                  </option>
                ))}
              </select>
            </div>
          ) : null}
        </div>
      </header>

      {products.error !== null ? <ErrorBanner message={products.error} /> : null}
      {alertError !== null ? <ErrorBanner message={alertError} /> : null}

      <div className="card section">
        <h2 className="card-title">
          Alertes récentes{" "}
          <Link to="/alertes" className="dim" style={{ fontSize: 13 }}>
            tout voir →
          </Link>
        </h2>
        {alerts.loading ? (
          <Loading />
        ) : (alerts.data ?? []).length === 0 ? (
          <EmptyState title="Aucune alerte à traiter">
            Les pics, chutes et jalons détectés apparaîtront ici.
          </EmptyState>
        ) : (
          <table className="table">
            <tbody>
              {[...(alerts.data ?? [])]
                .sort((a, b) => b.triggeredAt.localeCompare(a.triggeredAt))
                .slice(0, 5)
                .map((alert) => (
                  <tr key={alert.id}>
                    <td>
                      <AlertTypeBadge type={alert.type} />
                    </td>
                    <td>
                      {alert.message}
                      <div className="dim">{productName(alert.productId)}</div>
                    </td>
                    <td className="dim">{formatDateTime(alert.triggeredAt)}</td>
                    <td style={{ textAlign: "right" }}>
                      <button
                        className="btn btn-ghost"
                        disabled={acknowledging === alert.id}
                        onClick={() => void handleAcknowledge(alert.id)}
                      >
                        {acknowledging === alert.id ? "…" : "Acquitter"}
                      </button>
                    </td>
                  </tr>
                ))}
            </tbody>
          </table>
        )}
      </div>

      <div className="card section">
        <h2 className="card-title">
          Objectifs du mois{" "}
          <Link to="/objectifs" className="dim" style={{ fontSize: 13 }}>
            tout voir →
          </Link>
        </h2>
        {objectives.loading ? (
          <Loading />
        ) : objectives.error !== null ? (
          <ErrorBanner message={objectives.error} />
        ) : (objectives.data?.objectives ?? []).length === 0 ? (
          <EmptyState title="Aucun objectif ce mois-ci">
            Fixez vos cibles mensuelles dans l'onglet <Link to="/objectifs">Objectifs</Link>.
          </EmptyState>
        ) : (
          <table className="table">
            <tbody>
              {[...(objectives.data?.objectives ?? [])]
                .sort((a, b) => b.progressPercent - a.progressPercent)
                .slice(0, 3)
                .map((o) => (
                  <tr key={o.id}>
                    <td>
                      <strong>{objectiveMetricLabel(o.metric)}</strong>
                      <div className="dim">
                        {o.productId === null ? "Global" : productName(o.productId)}
                      </div>
                    </td>
                    <td className="num">
                      {formatNumber(o.actualValue)} / {formatNumber(o.targetValue)}
                    </td>
                    <td style={{ width: "45%" }}>
                      <ObjectiveProgressBar percent={o.progressPercent} />
                    </td>
                  </tr>
                ))}
            </tbody>
          </table>
        )}
      </div>

      <div className="card-grid">
        <StatCard label="Produits" value={formatNumber((products.data ?? []).length)} />
        <StatCard
          label="Prospects actifs"
          value={formatNumber(openProspects.length)}
          hint={`${formatNumber((prospects.data ?? []).length)} au total`}
        />
        <StatCard label="Paiements" value={formatNumber((payments.data ?? []).length)} />
        <StatCard label="Encaissé (EUR)" value={formatMoney(settledTotal, "EUR")} />
      </div>

      <div className="card section">
        <h2 className="card-title">Portefeuille ({days} j)</h2>
        {portfolio.loading ? (
          <Loading />
        ) : portfolio.error !== null ? (
          <ErrorBanner message={portfolio.error} />
        ) : portfolio.data === null || portfolio.data.products.length === 0 ? (
          <EmptyState title="Aucun produit dans le portefeuille">
            Créez un produit dans l'onglet <Link to="/produits">Produits</Link> pour démarrer.
          </EmptyState>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Produit</th>
                <th className="num">Packages suivis</th>
                <th className="num">Téléchargements ({portfolio.data.periodDays} j)</th>
                <th className="num">Trafic ({portfolio.data.periodDays} j)</th>
                <th className="num">Stars</th>
                <th className="num">Feedbacks ouverts</th>
              </tr>
            </thead>
            <tbody>
              {portfolio.data.products.map((row) => (
                <tr
                  key={row.productId}
                  className="portfolio-row"
                  onClick={() => void navigate(`/produits/${row.productId}`)}
                >
                  <td>
                    <strong>{row.name}</strong>
                    <div className="dim">
                      {formatNumber(row.totalDownloads)} téléchargements au total
                    </div>
                  </td>
                  <td className="num">{formatNumber(row.packageCount)}</td>
                  <td className="num">
                    {formatNumber(row.downloads.currentPeriod)}{" "}
                    <TrendBadge comparison={row.downloads} />
                  </td>
                  <td className="num">
                    {formatNumber(row.pageViews.currentPeriod)}{" "}
                    <TrendBadge comparison={row.pageViews} />
                  </td>
                  <td className="num">{formatNumber(row.latestStars)}</td>
                  <td className="num">
                    {row.openFeedbackCount > 0 ? (
                      <span className="badge badge-amber">
                        {formatNumber(row.openFeedbackCount)}
                      </span>
                    ) : (
                      <span className="dim">0</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {products.loading ? (
        <Loading />
      ) : (products.data ?? []).length === 0 ? (
        <div className="section">
          <EmptyState title="Aucun produit suivi">
            Créez un produit dans l'onglet <Link to="/produits">Produits</Link> pour démarrer.
          </EmptyState>
        </div>
      ) : (
        <div className="two-col section">
          <div className="card">
            <h2 className="card-title">
              Téléchargements — {selectedId === "" ? "" : productName(selectedId)} ({days} j)
            </h2>
            {downloads.loading ? (
              <Loading />
            ) : downloads.error !== null ? (
              <ErrorBanner message={downloads.error} />
            ) : downloads.data === null || downloads.data.totalDownloads === 0 ? (
              <EmptyState title="Aucun téléchargement sur la période">
                Capturez des snapshots depuis la fiche produit pour alimenter ce graphique.
              </EmptyState>
            ) : (
              <DailyDownloadsChart report={downloads.data} />
            )}
          </div>
          <div className="card">
            <h2 className="card-title">
              Trafic — {selectedId === "" ? "" : productName(selectedId)} ({days} j)
            </h2>
            {traffic.loading ? (
              <Loading />
            ) : traffic.error !== null ? (
              <ErrorBanner message={traffic.error} />
            ) : traffic.data === null || traffic.data.totalPageViews === 0 ? (
              <EmptyState title="Aucun trafic enregistré">
                Intégrez le beacon sur votre site (POST /api/traffic) pour suivre les pages vues.
              </EmptyState>
            ) : (
              <DailyTrafficChart days={traffic.data.days} />
            )}
          </div>
        </div>
      )}

      <div className="two-col section">
        <div className="card">
          <h2 className="card-title">Prospects récents</h2>
          {recentProspects.length === 0 ? (
            <EmptyState title="Aucun prospect">
              Le pipeline est vide — ajoutez un prospect depuis l'onglet Prospects.
            </EmptyState>
          ) : (
            <table className="table">
              <tbody>
                {recentProspects.map((p) => (
                  <tr key={p.id}>
                    <td>
                      <strong>{p.name}</strong>
                      <div className="dim">{productName(p.productId)}</div>
                    </td>
                    <td>
                      <span className="badge badge-blue">{p.stage}</span>
                    </td>
                    <td className="dim">{formatDateTime(p.createdAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
        <div className="card">
          <h2 className="card-title">Paiements récents</h2>
          {recentPayments.length === 0 ? (
            <EmptyState title="Aucun paiement">
              Enregistrez un paiement depuis l'onglet Paiements.
            </EmptyState>
          ) : (
            <table className="table">
              <tbody>
                {recentPayments.map((p) => (
                  <tr key={p.id}>
                    <td>
                      <strong>{formatMoney(p.amount, p.currency)}</strong>
                      <div className="dim">{productName(p.productId)}</div>
                    </td>
                    <td>
                      {p.status === "Settled" ? (
                        <span className="badge badge-green">Réglé</span>
                      ) : (
                        <span className="badge badge-amber">{p.status === "Pending" ? "En attente" : p.status}</span>
                      )}
                    </td>
                    <td className="dim">{formatDateTime(p.createdAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </>
  )
}
