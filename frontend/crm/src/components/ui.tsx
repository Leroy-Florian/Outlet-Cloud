import type { ReactNode } from "react"

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

/** Bornes [aujourd'hui - 29 j, aujourd'hui] au format yyyy-MM-dd attendu par DateOnly. */
export const last30DaysRange = (): { from: string; to: string } => {
  const to = new Date()
  const from = new Date()
  from.setDate(to.getDate() - 29)
  const fmt = (d: Date) => d.toISOString().slice(0, 10)
  return { from: fmt(from), to: fmt(to) }
}
