import { useState, type FormEvent } from "react"
import {
  deleteObjective,
  getObjectivesProgress,
  listProducts,
  setObjective,
  type ObjectiveMetric,
} from "../api/client"
import { useQuery } from "../hooks/useQuery"
import {
  EmptyState,
  ErrorBanner,
  Loading,
  ObjectiveProgressBar,
  formatNumber,
  objectiveMetricLabel,
} from "../components/ui"

const METRICS: ReadonlyArray<ObjectiveMetric> = ["Downloads", "PageViews", "Revenue", "Prospects"]

/** Mois courant au format "yyyy-MM" attendu par l'API. */
const currentMonth = () => new Date().toISOString().slice(0, 7)

export const ObjectivesPage = () => {
  const [month, setMonth] = useState(currentMonth())
  const progress = useQuery(() => getObjectivesProgress(month), [month])
  const products = useQuery(listProducts, [])

  const [productId, setProductId] = useState("")
  const [metric, setMetric] = useState<ObjectiveMetric>("Downloads")
  const [formMonth, setFormMonth] = useState(currentMonth())
  const [targetValue, setTargetValue] = useState("")
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const productName = (id: string | null) =>
    id === null ? "Global" : (products.data?.find((p) => p.id === id)?.name ?? id)

  const handleSave = async (event: FormEvent) => {
    event.preventDefault()
    setSaving(true)
    setActionError(null)
    try {
      await setObjective({
        productId: productId === "" ? null : productId,
        metric,
        month: formMonth,
        targetValue: Number(targetValue),
      })
      setTargetValue("")
      if (formMonth === month) {
        progress.reload()
      } else {
        setMonth(formMonth)
      }
    } catch (e) {
      setActionError(e instanceof Error ? e.message : String(e))
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (objectiveId: string) => {
    setDeleting(objectiveId)
    setActionError(null)
    try {
      await deleteObjective(objectiveId)
      progress.reload()
    } catch (e) {
      setActionError(e instanceof Error ? e.message : String(e))
    } finally {
      setDeleting(null)
    }
  }

  return (
    <>
      <header className="page-header">
        <div>
          <h1 className="page-title">Objectifs</h1>
          <p className="page-subtitle">Cibles mensuelles par métrique, par produit ou globales.</p>
        </div>
        <div className="field">
          <label>Mois</label>
          <input type="month" value={month} onChange={(e) => setMonth(e.target.value)} />
        </div>
      </header>

      <div className="card">
        <h2 className="card-title">Définir un objectif</h2>
        <p className="dim" style={{ marginTop: 0 }}>
          Un objectif existant pour le même triplet (produit, métrique, mois) est mis à jour.
        </p>
        <form className="form-row" onSubmit={(e) => void handleSave(e)}>
          <div className="field">
            <label>Produit</label>
            <select value={productId} onChange={(e) => setProductId(e.target.value)}>
              <option value="">Global (tous produits)</option>
              {(products.data ?? []).map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name}
                </option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Métrique</label>
            <select value={metric} onChange={(e) => setMetric(e.target.value as ObjectiveMetric)}>
              {METRICS.map((m) => (
                <option key={m} value={m}>
                  {objectiveMetricLabel(m)}
                </option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Mois</label>
            <input
              type="month"
              value={formMonth}
              onChange={(e) => setFormMonth(e.target.value)}
              required
            />
          </div>
          <div className="field">
            <label>Cible</label>
            <input
              type="number"
              step="0.01"
              min="0"
              value={targetValue}
              onChange={(e) => setTargetValue(e.target.value)}
              required
              style={{ width: 130 }}
            />
          </div>
          <button className="btn btn-primary" disabled={saving || targetValue === ""}>
            {saving ? "Enregistrement…" : "Enregistrer"}
          </button>
        </form>
      </div>

      {actionError !== null ? <ErrorBanner message={actionError} /> : null}
      {progress.error !== null ? <ErrorBanner message={progress.error} /> : null}

      <div className="card section">
        <h2 className="card-title">Progression — {month}</h2>
        {progress.loading ? (
          <Loading />
        ) : (progress.data?.objectives ?? []).length === 0 ? (
          <EmptyState title="Aucun objectif sur ce mois">
            Définissez votre première cible mensuelle ci-dessus.
          </EmptyState>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Métrique</th>
                <th>Portée</th>
                <th className="num">Réalisé / Cible</th>
                <th style={{ width: "40%" }}>Progression</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {(progress.data?.objectives ?? []).map((o) => (
                <tr key={o.id}>
                  <td>
                    <strong>{objectiveMetricLabel(o.metric)}</strong>
                  </td>
                  <td>
                    {o.productId === null ? (
                      <span className="badge badge-violet">Global</span>
                    ) : (
                      <span className="badge badge-blue">{productName(o.productId)}</span>
                    )}
                  </td>
                  <td className="num">
                    {formatNumber(o.actualValue)} / {formatNumber(o.targetValue)}
                  </td>
                  <td>
                    <ObjectiveProgressBar percent={o.progressPercent} />
                  </td>
                  <td style={{ textAlign: "right" }}>
                    <button
                      className="btn btn-ghost"
                      disabled={deleting === o.id}
                      onClick={() => void handleDelete(o.id)}
                    >
                      {deleting === o.id ? "…" : "Supprimer"}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </>
  )
}
