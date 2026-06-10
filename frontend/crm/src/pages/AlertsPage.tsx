import { useState } from "react"
import {
  acknowledgeAlert,
  evaluateAlerts,
  listAlerts,
  listProducts,
  type AlertDto,
} from "../api/client"
import { useQuery } from "../hooks/useQuery"
import {
  AlertTypeBadge,
  EmptyState,
  ErrorBanner,
  Loading,
  formatDateTime,
} from "../components/ui"

const AlertsTable = ({
  alerts,
  productName,
  acknowledging,
  onAcknowledge,
}: {
  alerts: ReadonlyArray<AlertDto>
  productName: (id: string) => string
  acknowledging: string | null
  onAcknowledge: (alertId: string) => void
}) => (
  <table className="table">
    <thead>
      <tr>
        <th>Type</th>
        <th>Message</th>
        <th>Produit</th>
        <th>Déclenchée le</th>
        <th />
      </tr>
    </thead>
    <tbody>
      {alerts.map((alert) => (
        <tr key={alert.id}>
          <td>
            <AlertTypeBadge type={alert.type} />
          </td>
          <td>{alert.message}</td>
          <td>{productName(alert.productId)}</td>
          <td className="dim">{formatDateTime(alert.triggeredAt)}</td>
          <td style={{ textAlign: "right" }}>
            {alert.acknowledged ? (
              <span className="dim">Acquittée</span>
            ) : (
              <button
                className="btn btn-ghost"
                disabled={acknowledging === alert.id}
                onClick={() => onAcknowledge(alert.id)}
              >
                {acknowledging === alert.id ? "…" : "Acquitter"}
              </button>
            )}
          </td>
        </tr>
      ))}
    </tbody>
  </table>
)

export const AlertsPage = () => {
  const products = useQuery(listProducts, [])
  const [productFilter, setProductFilter] = useState("")

  const alerts = useQuery(
    () => listAlerts(productFilter === "" ? {} : { productId: productFilter }),
    [productFilter],
  )

  const [acknowledging, setAcknowledging] = useState<string | null>(null)
  const [evaluating, setEvaluating] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [evaluationNotice, setEvaluationNotice] = useState<string | null>(null)

  const productName = (id: string) => products.data?.find((p) => p.id === id)?.name ?? id

  const handleAcknowledge = async (alertId: string) => {
    setAcknowledging(alertId)
    setActionError(null)
    try {
      await acknowledgeAlert(alertId)
      alerts.reload()
    } catch (e) {
      setActionError(e instanceof Error ? e.message : String(e))
    } finally {
      setAcknowledging(null)
    }
  }

  const handleEvaluate = async (productId: string) => {
    setEvaluating(true)
    setActionError(null)
    setEvaluationNotice(null)
    try {
      const created = await evaluateAlerts(productId)
      setEvaluationNotice(
        created.length === 0
          ? "Évaluation terminée : aucune nouvelle alerte."
          : `Évaluation terminée : ${created.length} nouvelle(s) alerte(s).`,
      )
      alerts.reload()
    } catch (e) {
      setActionError(e instanceof Error ? e.message : String(e))
    } finally {
      setEvaluating(false)
    }
  }

  const all = alerts.data ?? []
  const unacknowledged = all.filter((a) => !a.acknowledged)
  const acknowledged = all.filter((a) => a.acknowledged)

  return (
    <>
      <header className="page-header">
        <div>
          <h1 className="page-title">Alertes</h1>
          <p className="page-subtitle">
            Pics, chutes, jalons et échecs de capture détectés sur vos produits.
          </p>
        </div>
        <div className="field">
          <label>Produit</label>
          <select value={productFilter} onChange={(e) => setProductFilter(e.target.value)}>
            <option value="">Tous les produits</option>
            {(products.data ?? []).map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
        </div>
      </header>

      <div className="card">
        <h2 className="card-title">Évaluer maintenant</h2>
        {(products.data ?? []).length === 0 ? (
          <EmptyState title="Aucun produit">
            Créez un produit pour pouvoir évaluer ses alertes.
          </EmptyState>
        ) : (
          <div className="chip-list">
            {(products.data ?? [])
              .filter((p) => productFilter === "" || p.id === productFilter)
              .map((p) => (
                <button
                  key={p.id}
                  className="btn"
                  disabled={evaluating}
                  onClick={() => void handleEvaluate(p.id)}
                >
                  {evaluating ? "…" : `Évaluer ${p.name}`}
                </button>
              ))}
          </div>
        )}
      </div>

      {evaluationNotice !== null ? <div className="notice-banner">{evaluationNotice}</div> : null}
      {actionError !== null ? <ErrorBanner message={actionError} /> : null}
      {alerts.error !== null ? <ErrorBanner message={alerts.error} /> : null}

      <div className="card section">
        <h2 className="card-title">À traiter ({unacknowledged.length})</h2>
        {alerts.loading ? (
          <Loading />
        ) : unacknowledged.length === 0 ? (
          <EmptyState title="Aucune alerte à traiter">
            Tout est calme — lancez « Évaluer maintenant » après une capture de snapshots.
          </EmptyState>
        ) : (
          <AlertsTable
            alerts={unacknowledged}
            productName={productName}
            acknowledging={acknowledging}
            onAcknowledge={(alertId) => void handleAcknowledge(alertId)}
          />
        )}
      </div>

      {acknowledged.length > 0 ? (
        <div className="card section">
          <h2 className="card-title">Acquittées ({acknowledged.length})</h2>
          <AlertsTable
            alerts={acknowledged}
            productName={productName}
            acknowledging={acknowledging}
            onAcknowledge={(alertId) => void handleAcknowledge(alertId)}
          />
        </div>
      ) : null}
    </>
  )
}
