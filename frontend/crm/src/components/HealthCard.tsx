import type { ProductHealthDto } from "../api/client"
import { EmptyState, ErrorBanner, Loading } from "./ui"
import { useQuery } from "../hooks/useQuery"
import { getProductHealth } from "../api/client"

/** Couleur du score : vert ≥ 80, bleu ≥ 60, jaune ≥ 40, rouge sinon. */
const scoreColor = (score: number) =>
  score >= 80 ? "var(--green)" : score >= 60 ? "var(--blue)" : score >= 40 ? "var(--amber)" : "var(--red)"

const numberFormat = new Intl.NumberFormat("fr-FR", { maximumFractionDigits: 1 })

const formatInput = (value: number | null, suffix = "") =>
  value === null ? "n/a" : `${numberFormat.format(value)}${suffix}`

const COMPONENTS: ReadonlyArray<{
  key: keyof ProductHealthDto["components"]
  label: string
  explain: string
}> = [
  {
    key: "releaseFreshness",
    label: "Fraîcheur des releases",
    explain: "Jours écoulés depuis la dernière release publiée",
  },
  {
    key: "downloadTrend",
    label: "Tendance des téléchargements",
    explain: "Variation des téléchargements vs période précédente",
  },
  {
    key: "repoActivity",
    label: "Activité du repository",
    explain: "Croissance des stars et des issues ouvertes",
  },
  {
    key: "snapshotReliability",
    label: "Fiabilité des captures",
    explain: "Échecs récents de capture de snapshots",
  },
]

const ComponentBar = ({
  label,
  explain,
  value,
}: {
  label: string
  explain: string
  value: number
}) => (
  <div title={explain}>
    <div className="health-component-label">
      <span>{label}</span>
      <span className="num">{value} / 100</span>
    </div>
    <div className="progress-track">
      <div
        className="progress-fill"
        style={{ width: `${Math.min(Math.max(value, 0), 100)}%`, background: scoreColor(value) }}
      />
    </div>
  </div>
)

const HealthDetails = ({ health }: { health: ProductHealthDto }) => {
  const color = scoreColor(health.total)
  const angle = Math.min(Math.max(health.total, 0), 100) * 3.6
  return (
    <>
      <div className="health-layout">
        <div
          className="health-ring"
          style={{
            background: `conic-gradient(${color} ${angle}deg, var(--card-hover) ${angle}deg)`,
          }}
          role="img"
          aria-label={`Score de santé : ${health.total} sur 100`}
        >
          <div className="health-ring-inner">
            <span className="health-total" style={{ color }}>
              {health.total}
            </span>
            <span className="health-total-max">/ 100</span>
            <span className="badge" style={{ color, borderColor: color }}>
              {health.label}
            </span>
          </div>
        </div>
        <div className="health-components">
          {COMPONENTS.map((c) => (
            <ComponentBar
              key={c.key}
              label={c.label}
              explain={c.explain}
              value={health.components[c.key]}
            />
          ))}
        </div>
      </div>
      <div className="health-inputs">
        <span className="badge" title="Jours depuis la dernière release publiée">
          Dernière release : {formatInput(health.inputs.daysSinceLatestRelease, " j")}
        </span>
        <span className="badge" title="Variation des téléchargements vs période précédente">
          Téléchargements : {formatInput(health.inputs.downloadsPercentChange, " %")}
        </span>
        <span className="badge" title="Croissance des issues ouvertes">
          Issues ouvertes : {formatInput(health.inputs.openIssuesGrowthPercent, " %")}
        </span>
        <span className="badge" title="Croissance des stars GitHub">
          Stars : {formatInput(health.inputs.starsGrowthPercent, " %")}
        </span>
        <span className="badge" title="Échecs de capture de snapshots récents">
          Échecs de capture récents : {health.inputs.recentCaptureFailures}
        </span>
      </div>
    </>
  )
}

/** Carte « Santé » d'un produit : score pondéré 0–100 + sous-scores et signaux bruts. */
export const HealthCard = ({ productId }: { productId: string }) => {
  const health = useQuery(() => getProductHealth(productId), [productId])

  return (
    <div className="card section">
      <h2 className="card-title">Santé</h2>
      {health.loading ? (
        <Loading />
      ) : health.error !== null ? (
        <ErrorBanner message={health.error} />
      ) : health.data === null ? (
        <EmptyState title="Score de santé indisponible">
          Capturez des snapshots pour alimenter le score de santé.
        </EmptyState>
      ) : (
        <HealthDetails health={health.data} />
      )}
    </div>
  )
}
