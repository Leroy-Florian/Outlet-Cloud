import { useState } from "react"
import {
  dismissFeedback,
  getFeedbackInbox,
  getNps,
  listProducts,
  resolveFeedback,
  triageFeedback,
  type FeedbackCategory,
  type FeedbackItemDto,
  type FeedbackStatus,
} from "../api/client"
import { useQuery } from "../hooks/useQuery"
import { EmptyState, ErrorBanner, Loading, StatCard, formatDateTime } from "../components/ui"

const NPS_DAYS = 90

const STATUS_LABELS: Record<string, string> = {
  New: "Nouveau",
  Triaged: "Trié",
  Resolved: "Résolu",
  Dismissed: "Rejeté",
}

const CATEGORY_LABELS: Record<string, string> = {
  Bug: "Bug",
  FeatureRequest: "Feature",
  Question: "Question",
  Other: "Autre",
}

const CATEGORIES: ReadonlyArray<FeedbackCategory> = ["Bug", "FeatureRequest", "Question", "Other"]

const STATUSES: ReadonlyArray<FeedbackStatus> = ["New", "Triaged", "Resolved", "Dismissed"]

/** Badge visuel par catégorie : Bug rouge, Feature violet, Question bleu. */
const CategoryBadge = ({ category }: { category: string }) => {
  const className =
    category === "Bug"
      ? "badge badge-red"
      : category === "FeatureRequest"
        ? "badge badge-violet"
        : category === "Question"
          ? "badge badge-blue"
          : "badge"
  return <span className={className}>{CATEGORY_LABELS[category] ?? category}</span>
}

/** Chip de score NPS : 9-10 vert (promoteur), 7-8 jaune (passif), 0-6 rouge (détracteur). */
const ScoreChip = ({ score }: { score: number }) => {
  const className =
    score >= 9 ? "badge badge-green" : score >= 7 ? "badge badge-amber" : "badge badge-red"
  return (
    <span className={className} title="Score NPS (0-10)">
      {score}/10
    </span>
  )
}

const npsFormat = new Intl.NumberFormat("fr-FR", { maximumFractionDigits: 0 })

const StatusBadge = ({ status }: { status: string }) => {
  const className =
    status === "New"
      ? "badge badge-amber"
      : status === "Triaged"
        ? "badge badge-blue"
        : status === "Resolved"
          ? "badge badge-green"
          : "badge"
  return <span className={className}>{STATUS_LABELS[status] ?? status}</span>
}

/** Actions de transition autorisées selon le statut courant. */
const FeedbackActions = ({
  item,
  busy,
  onAction,
}: {
  item: FeedbackItemDto
  busy: boolean
  onAction: (action: () => Promise<void>) => void
}) => (
  <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }}>
    {item.status === "New" ? (
      <button
        className="btn btn-ghost"
        disabled={busy}
        onClick={() => onAction(() => triageFeedback(item.id))}
      >
        Trier
      </button>
    ) : null}
    {item.status === "New" || item.status === "Triaged" ? (
      <>
        <button
          className="btn btn-ghost"
          disabled={busy}
          onClick={() => onAction(() => resolveFeedback(item.id))}
        >
          Résoudre
        </button>
        <button
          className="btn btn-ghost btn-danger"
          disabled={busy}
          onClick={() => onAction(() => dismissFeedback(item.id))}
        >
          Rejeter
        </button>
      </>
    ) : null}
  </div>
)

export const FeedbackPage = () => {
  const [productId, setProductId] = useState("")
  const [status, setStatus] = useState<FeedbackStatus | "">("")
  const [category, setCategory] = useState<FeedbackCategory | "">("")
  const [busy, setBusy] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)

  const products = useQuery(listProducts, [])
  const inbox = useQuery(
    () =>
      getFeedbackInbox({
        productId: productId === "" ? undefined : productId,
        status: status === "" ? undefined : status,
        category: category === "" ? undefined : category,
      }),
    [productId, status, category],
  )

  const globalNps = useQuery(() => getNps(undefined, NPS_DAYS), [])
  const productNps = useQuery(
    () => (productId === "" ? Promise.resolve(null) : getNps(productId, NPS_DAYS)),
    [productId],
  )

  const productName = (id: string) => products.data?.find((p) => p.id === id)?.name ?? id

  const npsValue = (nps: number | null) => (nps === null ? "—" : npsFormat.format(nps))
  const npsHint = (report: { promoters: number; passives: number; detractors: number }) =>
    `${report.promoters} promoteur(s) · ${report.passives} passif(s) · ${report.detractors} détracteur(s)`

  const runAction = (action: () => Promise<void>) => {
    setBusy(true)
    setActionError(null)
    action()
      .then(() => inbox.reload())
      .catch((e: unknown) => setActionError(e instanceof Error ? e.message : String(e)))
      .finally(() => setBusy(false))
  }

  const counts = inbox.data?.counts ?? null

  const chip = (label: string, value: number, target: FeedbackStatus | "") => (
    <button
      key={label}
      type="button"
      className={status === target ? "count-chip active" : "count-chip"}
      onClick={() => setStatus(target)}
    >
      {label} <strong>{value}</strong>
    </button>
  )

  return (
    <>
      <header className="page-header">
        <div>
          <h1 className="page-title">Feedback</h1>
          <p className="page-subtitle">Boîte de réception des retours produits.</p>
        </div>
      </header>

      {actionError !== null ? <ErrorBanner message={actionError} /> : null}
      {inbox.error !== null ? <ErrorBanner message={inbox.error} /> : null}
      {globalNps.error !== null ? <ErrorBanner message={globalNps.error} /> : null}
      {productNps.error !== null ? <ErrorBanner message={productNps.error} /> : null}

      <div className="card-grid">
        <StatCard
          label={`NPS global (${NPS_DAYS} j)`}
          value={globalNps.data === null ? "…" : npsValue(globalNps.data.nps)}
          {...(globalNps.data === null ? {} : { hint: npsHint(globalNps.data) })}
        />
        {productId !== "" ? (
          <StatCard
            label={`NPS — ${productName(productId)} (${NPS_DAYS} j)`}
            value={productNps.data === null ? "…" : npsValue(productNps.data.nps)}
            {...(productNps.data === null ? {} : { hint: npsHint(productNps.data) })}
          />
        ) : null}
      </div>

      {counts !== null ? (
        <div className="count-chips">
          {chip("Tous", counts.total, "")}
          {chip(STATUS_LABELS.New ?? "Nouveau", counts.new, "New")}
          {chip(STATUS_LABELS.Triaged ?? "Trié", counts.triaged, "Triaged")}
          {chip(STATUS_LABELS.Resolved ?? "Résolu", counts.resolved, "Resolved")}
          {chip(STATUS_LABELS.Dismissed ?? "Rejeté", counts.dismissed, "Dismissed")}
        </div>
      ) : null}

      <div className="card">
        <div className="form-row">
          <div className="field">
            <label>Produit</label>
            <select value={productId} onChange={(e) => setProductId(e.target.value)}>
              <option value="">— tous —</option>
              {(products.data ?? []).map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name}
                </option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Statut</label>
            <select
              value={status}
              onChange={(e) => setStatus(e.target.value as FeedbackStatus | "")}
            >
              <option value="">— tous —</option>
              {STATUSES.map((s) => (
                <option key={s} value={s}>
                  {STATUS_LABELS[s]}
                </option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Catégorie</label>
            <select
              value={category}
              onChange={(e) => setCategory(e.target.value as FeedbackCategory | "")}
            >
              <option value="">— toutes —</option>
              {CATEGORIES.map((c) => (
                <option key={c} value={c}>
                  {CATEGORY_LABELS[c]}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      <div className="card section">
        {inbox.loading ? (
          <Loading />
        ) : inbox.data === null || inbox.data.items.length === 0 ? (
          <EmptyState title="Aucun feedback">
            Aucun retour ne correspond aux filtres. Les feedbacks arrivent via POST /api/feedback.
          </EmptyState>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Produit</th>
                <th>Catégorie</th>
                <th>Message</th>
                <th>Score</th>
                <th>Source</th>
                <th>Reçu le</th>
                <th>Statut</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {inbox.data.items.map((item) => (
                <tr key={item.id}>
                  <td>{productName(item.productId)}</td>
                  <td>
                    <CategoryBadge category={item.category} />
                  </td>
                  <td className="feedback-message">
                    {item.message}
                    {item.reporterEmail !== null ? (
                      <div className="dim">{item.reporterEmail}</div>
                    ) : null}
                  </td>
                  <td>
                    {item.score !== null ? (
                      <ScoreChip score={item.score} />
                    ) : (
                      <span className="dim">—</span>
                    )}
                  </td>
                  <td className="dim">{item.sourceApp}</td>
                  <td className="dim">{formatDateTime(item.receivedAt)}</td>
                  <td>
                    <StatusBadge status={item.status} />
                  </td>
                  <td>
                    <FeedbackActions item={item} busy={busy} onAction={runAction} />
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
