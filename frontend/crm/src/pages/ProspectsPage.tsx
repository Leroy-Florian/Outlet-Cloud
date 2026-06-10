import { useState, type FormEvent } from "react"
import {
  advanceProspectStage,
  createProspect,
  getProspectPipelineStats,
  listOrganizations,
  listProducts,
  listProspects,
  updateProspect,
  type ProspectDto,
} from "../api/client"
import { useQuery } from "../hooks/useQuery"
import { EmptyState, ErrorBanner, Loading, StatCard, formatMoney, formatNumber } from "../components/ui"

const STAGES = ["New", "Contacted", "Qualified", "Won", "Lost"] as const

const STAGE_LABELS: Record<string, string> = {
  New: "Nouveau",
  Contacted: "Contacté",
  Qualified: "Qualifié",
  Won: "Gagné",
  Lost: "Perdu",
}

/** Étape « suivante » naturelle du pipeline (Won/Lost = terminal). */
const nextStage = (stage: string): string | null => {
  switch (stage) {
    case "New":
      return "Contacted"
    case "Contacted":
      return "Qualified"
    case "Qualified":
      return "Won"
    default:
      return null
  }
}

const ProspectCard = ({
  prospect,
  productName,
  onChanged,
  onError,
}: {
  prospect: ProspectDto
  productName: string
  onChanged: () => void
  onError: (message: string) => void
}) => {
  const [busy, setBusy] = useState(false)
  const [editing, setEditing] = useState(false)
  const [valueInput, setValueInput] = useState(
    prospect.estimatedValue === null ? "" : String(prospect.estimatedValue),
  )
  const [companyInput, setCompanyInput] = useState(prospect.company ?? "")
  const target = nextStage(prospect.stage)

  const run = async (action: () => Promise<void>) => {
    setBusy(true)
    try {
      await action()
      onChanged()
    } catch (e) {
      onError(e instanceof Error ? e.message : String(e))
    } finally {
      setBusy(false)
    }
  }

  const advance = (to: string) => run(() => advanceProspectStage(prospect.id, to))

  const save = () => {
    const parsed = valueInput.trim() === "" ? null : Number(valueInput.replace(",", "."))
    if (parsed !== null && (Number.isNaN(parsed) || parsed < 0)) {
      onError("Valeur estimée invalide.")
      return
    }
    void run(async () => {
      await updateProspect(prospect.id, {
        estimatedValue: parsed,
        estimatedValueCurrency: parsed === null ? null : "EUR",
        company: companyInput.trim() === "" ? null : companyInput.trim(),
      })
      setEditing(false)
    })
  }

  return (
    <div className="prospect-card">
      <strong>{prospect.name}</strong>
      <div className="dim">{prospect.email}</div>
      {prospect.company !== null ? <div className="dim">{prospect.company}</div> : null}
      <div className="dim">Produit : {productName}</div>
      {prospect.estimatedValue !== null ? (
        <div style={{ marginTop: 6 }}>
          <span className="badge badge-green">
            {formatMoney(prospect.estimatedValue, prospect.estimatedValueCurrency ?? "EUR")}
          </span>
        </div>
      ) : null}
      {editing ? (
        <div style={{ marginTop: 8, display: "grid", gap: 6 }}>
          <div className="field">
            <label>Valeur estimée (EUR)</label>
            <input
              inputMode="decimal"
              value={valueInput}
              onChange={(e) => setValueInput(e.target.value)}
              placeholder="2500"
            />
          </div>
          <div className="field">
            <label>Société</label>
            <input value={companyInput} onChange={(e) => setCompanyInput(e.target.value)} />
          </div>
          <div style={{ display: "flex", gap: 6 }}>
            <button className="btn btn-primary" disabled={busy} onClick={save}>
              Enregistrer
            </button>
            <button className="btn btn-ghost" disabled={busy} onClick={() => setEditing(false)}>
              Annuler
            </button>
          </div>
        </div>
      ) : (
        <div style={{ marginTop: 8, display: "flex", gap: 6, flexWrap: "wrap" }}>
          {target !== null ? (
            <button className="btn btn-ghost" disabled={busy} onClick={() => void advance(target)}>
              → {STAGE_LABELS[target]}
            </button>
          ) : null}
          <button className="btn btn-ghost" disabled={busy} onClick={() => setEditing(true)}>
            Modifier
          </button>
          {prospect.stage !== "Lost" && prospect.stage !== "Won" ? (
            <button
              className="btn btn-ghost btn-danger"
              disabled={busy}
              onClick={() => void advance("Lost")}
            >
              Perdu
            </button>
          ) : null}
        </div>
      )}
    </div>
  )
}

export const ProspectsPage = () => {
  const prospects = useQuery(listProspects, [])
  const products = useQuery(listProducts, [])
  const organizations = useQuery(listOrganizations, [])
  const stats = useQuery(getProspectPipelineStats, [])

  const reloadAll = () => {
    prospects.reload()
    stats.reload()
  }

  const [name, setName] = useState("")
  const [email, setEmail] = useState("")
  const [company, setCompany] = useState("")
  const [productId, setProductId] = useState("")
  const [organizationId, setOrganizationId] = useState("")
  const [creating, setCreating] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)

  const productName = (id: string) => products.data?.find((p) => p.id === id)?.name ?? id

  const handleCreate = async (event: FormEvent) => {
    event.preventDefault()
    setCreating(true)
    setActionError(null)
    try {
      await createProspect({
        productId,
        organizationId: organizationId === "" ? null : organizationId,
        name: name.trim(),
        email: email.trim(),
        company: company.trim() || null,
      })
      setName("")
      setEmail("")
      setCompany("")
      reloadAll()
    } catch (e) {
      setActionError(e instanceof Error ? e.message : String(e))
    } finally {
      setCreating(false)
    }
  }

  const byStage = (stage: string) =>
    (prospects.data ?? []).filter((p) => p.stage === stage)

  return (
    <>
      <header className="page-header">
        <div>
          <h1 className="page-title">Prospects</h1>
          <p className="page-subtitle">Pipeline commercial par étape.</p>
        </div>
      </header>

      {stats.error !== null ? <ErrorBanner message={stats.error} /> : null}
      {stats.data !== null ? (
        <div className="pipeline-stats">
          <StatCard
            label="Prospects"
            value={formatNumber(stats.data.totalProspects)}
            hint="Pipeline complet"
          />
          <StatCard
            label="Valeur estimée totale"
            value={formatMoney(stats.data.totalEstimatedValue, "EUR")}
          />
          {stats.data.stages.map((s) => (
            <StatCard
              key={s.stage}
              label={STAGE_LABELS[s.stage] ?? s.stage}
              value={formatNumber(s.count)}
              hint={`${formatMoney(s.totalEstimatedValue, "EUR")}${
                s.conversionRateToNext === null
                  ? ""
                  : ` · conversion ${(s.conversionRateToNext * 100).toLocaleString("fr-FR", { maximumFractionDigits: 1 })} %`
              }`}
            />
          ))}
        </div>
      ) : null}

      <div className="card">
        <h2 className="card-title">Nouveau prospect</h2>
        <form className="form-row" onSubmit={(e) => void handleCreate(e)}>
          <div className="field">
            <label>Produit</label>
            <select value={productId} onChange={(e) => setProductId(e.target.value)} required>
              <option value="">— choisir —</option>
              {(products.data ?? []).map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name}
                </option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Nom</label>
            <input value={name} onChange={(e) => setName(e.target.value)} required />
          </div>
          <div className="field">
            <label>Email</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>
          <div className="field">
            <label>Société</label>
            <input value={company} onChange={(e) => setCompany(e.target.value)} />
          </div>
          <div className="field">
            <label>Organisation</label>
            <select value={organizationId} onChange={(e) => setOrganizationId(e.target.value)}>
              <option value="">— aucune —</option>
              {(organizations.data ?? []).map((o) => (
                <option key={o.id} value={o.id}>
                  {o.name}
                </option>
              ))}
            </select>
          </div>
          <button
            className="btn btn-primary"
            disabled={creating || productId === "" || name.trim() === "" || email.trim() === ""}
          >
            {creating ? "Création…" : "Ajouter"}
          </button>
        </form>
      </div>

      {actionError !== null ? <ErrorBanner message={actionError} /> : null}
      {prospects.error !== null ? <ErrorBanner message={prospects.error} /> : null}

      <div className="section">
        {prospects.loading ? (
          <Loading />
        ) : (prospects.data ?? []).length === 0 ? (
          <EmptyState title="Aucun prospect">
            Ajoutez votre premier prospect pour démarrer le pipeline.
          </EmptyState>
        ) : (
          <div className="stage-board">
            {STAGES.map((stage) => {
              const items = byStage(stage)
              return (
                <div className="stage-column" key={stage}>
                  <h3 className="stage-column-title">
                    {STAGE_LABELS[stage]} <span>{items.length}</span>
                  </h3>
                  {items.length === 0 ? (
                    <p className="dim">Vide</p>
                  ) : (
                    items.map((prospect) => (
                      <ProspectCard
                        key={prospect.id}
                        prospect={prospect}
                        productName={productName(prospect.productId)}
                        onChanged={reloadAll}
                        onError={setActionError}
                      />
                    ))
                  )}
                </div>
              )
            })}
          </div>
        )}
      </div>
    </>
  )
}
