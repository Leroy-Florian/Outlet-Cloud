import { useEffect, useMemo, useState } from "react"
import { Link } from "react-router-dom"
import {
  getDailyDownloads,
  getDailyTraffic,
  listPayments,
  listProducts,
  listProspects,
} from "../api/client"
import { useQuery } from "../hooks/useQuery"
import {
  EmptyState,
  ErrorBanner,
  Loading,
  StatCard,
  formatDateTime,
  formatMoney,
  formatNumber,
  last30DaysRange,
} from "../components/ui"
import { DailyDownloadsChart, DailyTrafficChart } from "../components/charts"

export const DashboardPage = () => {
  const products = useQuery(listProducts, [])
  const prospects = useQuery(listProspects, [])
  const payments = useQuery(listPayments, [])

  const [selectedId, setSelectedId] = useState("")

  useEffect(() => {
    if (selectedId === "" && products.data !== null && products.data.length > 0) {
      setSelectedId(products.data[0]?.id ?? "")
    }
  }, [products.data, selectedId])

  const range = useMemo(last30DaysRange, [])
  const downloads = useQuery(
    () =>
      selectedId === ""
        ? Promise.resolve(null)
        : getDailyDownloads(selectedId, range.from, range.to),
    [selectedId],
  )
  const traffic = useQuery(
    () =>
      selectedId === ""
        ? Promise.resolve(null)
        : getDailyTraffic(selectedId, range.from, range.to),
    [selectedId],
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
      </header>

      {products.error !== null ? <ErrorBanner message={products.error} /> : null}

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
              Téléchargements — {selectedId === "" ? "" : productName(selectedId)} (30 j)
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
              Trafic — {selectedId === "" ? "" : productName(selectedId)} (30 j)
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
