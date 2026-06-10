import type { ReactNode } from "react"
import type { PeriodComparisonDto } from "../api/client"

export const StatCard = ({
  label,
  value,
  hint,
}: {
  label: string
  value: ReactNode
  hint?: string
}) => (
  <div className="stat-card">
    <div className="stat-label">{label}</div>
    <div className="stat-value">{value}</div>
    {hint !== undefined ? <div className="stat-hint">{hint}</div> : null}
  </div>
)

export const EmptyState = ({ title, children }: { title: string; children?: ReactNode }) => (
  <div className="empty-state">
    <strong>{title}</strong>
    {children}
  </div>
)

export const ErrorBanner = ({ message }: { message: string }) => (
  <div className="error-banner">{message}</div>
)

export const Loading = ({ label = "Chargement…" }: { label?: string }) => (
  <div className="loading">{label}</div>
)

const numberFormat = new Intl.NumberFormat("fr-FR")

export const formatNumber = (value: number) => numberFormat.format(value)

export const formatDate = (iso: string) =>
  new Date(iso).toLocaleDateString("fr-FR", { day: "2-digit", month: "short" })

export const formatDateTime = (iso: string) =>
  new Date(iso).toLocaleString("fr-FR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  })

export const formatMoney = (amount: number, currency: string) =>
  new Intl.NumberFormat("fr-FR", { style: "currency", currency }).format(amount)

/** Bornes [aujourd'hui - (days-1), aujourd'hui] au format yyyy-MM-dd attendu par DateOnly. */
export const lastDaysRange = (days: number): { from: string; to: string } => {
  const to = new Date()
  const from = new Date()
  from.setDate(to.getDate() - (days - 1))
  const fmt = (d: Date) => d.toISOString().slice(0, 10)
  return { from: fmt(from), to: fmt(to) }
}

/** Bornes [aujourd'hui - 29 j, aujourd'hui] au format yyyy-MM-dd attendu par DateOnly. */
export const last30DaysRange = () => lastDaysRange(30)

const percentFormat = new Intl.NumberFormat("fr-FR", {
  maximumFractionDigits: 1,
  signDisplay: "always",
})

/**
 * Badge « % vs période précédente » : vert ↗ en hausse, rouge ↘ en baisse,
 * neutre quand c'est stable ou sans baseline (percentChange null).
 */
export const TrendBadge = ({ comparison }: { comparison: PeriodComparisonDto }) => {
  if (comparison.percentChange === null) {
    return <span className="badge">—</span>
  }
  const className =
    comparison.direction === "Up"
      ? "badge badge-green"
      : comparison.direction === "Down"
        ? "badge badge-red"
        : "badge"
  const arrow = comparison.direction === "Up" ? "↗" : comparison.direction === "Down" ? "↘" : "→"
  return (
    <span className={className} title={`Période précédente : ${formatNumber(comparison.previousPeriod)}`}>
      {arrow} {percentFormat.format(comparison.percentChange)} %
    </span>
  )
}

/**
 * Badge de type d'alerte : pic vert, chute rouge, jalon de stars jaune,
 * échec de capture gris (badge neutre).
 */
export const AlertTypeBadge = ({ type }: { type: string }) => {
  switch (type) {
    case "DownloadsSpike":
      return <span className="badge badge-green">↗ Pic de téléchargements</span>
    case "DownloadsDrop":
      return <span className="badge badge-red">↘ Chute de téléchargements</span>
    case "StarsMilestone":
      return <span className="badge badge-amber">★ Jalon de stars</span>
    case "SnapshotFailure":
      return <span className="badge">Échec de capture</span>
    default:
      return <span className="badge">{type}</span>
  }
}

/** Libellés FR des métriques d'objectif (contrat : enum sérialisé en string). */
export const objectiveMetricLabel = (metric: string) => {
  switch (metric) {
    case "Downloads":
      return "Téléchargements"
    case "PageViews":
      return "Pages vues"
    case "Revenue":
      return "Revenu"
    case "Prospects":
      return "Prospects"
    default:
      return metric
  }
}

const rawPercentFormat = new Intl.NumberFormat("fr-FR", { maximumFractionDigits: 1 })

/**
 * Barre de progression d'objectif : la barre est plafonnée à 100 %, le texte
 * affiche le pourcentage brut ; vert dès que l'objectif est atteint (≥ 100 %).
 */
export const ObjectiveProgressBar = ({ percent }: { percent: number }) => {
  const clipped = Math.min(Math.max(percent, 0), 100)
  const complete = percent >= 100
  return (
    <div className="progress-row">
      <div className="progress-track">
        <div
          className={complete ? "progress-fill complete" : "progress-fill"}
          style={{ width: `${clipped}%` }}
        />
      </div>
      <span className={complete ? "progress-percent complete" : "progress-percent"}>
        {rawPercentFormat.format(percent)} %
      </span>
    </div>
  )
}

export const PERIOD_OPTIONS = [7, 30, 90] as const

/** Sélecteur de période global (7/30/90 j) — pilote le paramètre `days` des analytics. */
export const PeriodSelector = ({
  value,
  onChange,
}: {
  value: number
  onChange: (days: number) => void
}) => (
  <div className="period-selector" role="group" aria-label="Période">
    {PERIOD_OPTIONS.map((days) => (
      <button
        key={days}
        type="button"
        className={value === days ? "period-option active" : "period-option"}
        onClick={() => onChange(days)}
      >
        {days} j
      </button>
    ))}
  </div>
)
