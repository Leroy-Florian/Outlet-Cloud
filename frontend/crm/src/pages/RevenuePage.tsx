import { getRevenueMetrics } from "../api/client"
import { useQuery } from "../hooks/useQuery"
import {
  EmptyState,
  ErrorBanner,
  Loading,
  StatCard,
  formatMoney,
} from "../components/ui"
import { MonthlyRevenueChart } from "../components/charts"

const MONTHS = 12

const monthLabel = (month: string) => {
  const [year, m] = month.split("-")
  return new Date(Number(year), Number(m) - 1, 1).toLocaleDateString("fr-FR", {
    month: "long",
    year: "numeric",
  })
}

export const RevenuePage = () => {
  const metrics = useQuery(() => getRevenueMetrics(MONTHS), [])

  if (metrics.loading) {
    return <Loading />
  }
  if (metrics.error !== null) {
    return <ErrorBanner message={metrics.error} />
  }
  if (metrics.data === null) {
    return null
  }

  const { primaryCurrency, series, mrr, churnMonths, currencyTotals } = metrics.data
  const current = series.at(-1)
  const best = series.reduce<(typeof series)[number] | null>(
    (acc, point) => (acc === null || point.total > acc.total ? point : acc),
    null,
  )
  const hasRevenue = series.some((point) => point.total > 0)

  return (
    <>
      <header className="page-header">
        <div>
          <h1 className="page-title">Revenus</h1>
          <p className="page-subtitle">
            Revenus mensuels sur {metrics.data.months} mois (paiements réglés, {primaryCurrency}).
          </p>
        </div>
      </header>

      <div className="card-grid">
        <StatCard
          label="Revenu du mois"
          value={formatMoney(current?.total ?? 0, primaryCurrency)}
          hint={current === undefined ? "—" : monthLabel(current.month)}
        />
        <StatCard
          label="MRR (approx.)"
          value={formatMoney(mrr, primaryCurrency)}
          hint="Paiements récurrents du mois en cours"
        />
        <StatCard
          label="Meilleur mois"
          value={formatMoney(best?.total ?? 0, primaryCurrency)}
          hint={best === null || best.total === 0 ? "—" : monthLabel(best.month)}
        />
        <StatCard
          label="Mois en repli (récurrent)"
          value={churnMonths}
          hint="Proxy de churn : mois où le récurrent baisse"
        />
      </div>

      <div className="card section">
        <h2 className="card-title">
          Revenus mensuels ({metrics.data.months} mois, {primaryCurrency}) — récurrent vs one-shot,
          cumul
        </h2>
        {hasRevenue ? (
          <MonthlyRevenueChart series={series} currency={primaryCurrency} />
        ) : (
          <EmptyState title="Aucun revenu sur la période">
            Enregistrez et réglez des paiements dans l'onglet Paiements pour alimenter ce
            graphique.
          </EmptyState>
        )}
      </div>

      <div className="card section">
        <h2 className="card-title">Totaux par devise</h2>
        {currencyTotals.length === 0 ? (
          <EmptyState title="Aucun paiement réglé">
            Les totaux par devise apparaîtront dès le premier paiement réglé.
          </EmptyState>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Devise</th>
                <th className="num">Total réglé</th>
              </tr>
            </thead>
            <tbody>
              {currencyTotals.map((c) => (
                <tr key={c.currency}>
                  <td>
                    {c.currency}{" "}
                    {c.currency === primaryCurrency ? (
                      <span className="badge badge-blue">primaire</span>
                    ) : null}
                  </td>
                  <td className="num">{formatMoney(c.total, c.currency)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </>
  )
}
