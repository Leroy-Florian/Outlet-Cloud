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
