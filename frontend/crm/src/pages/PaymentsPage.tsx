import { useState, type FormEvent } from "react"
import {
  listOrganizations,
  listPayments,
  listProducts,
  recordPayment,
  settlePayment,
} from "../api/client"
import { useQuery } from "../hooks/useQuery"
import {
  EmptyState,
  ErrorBanner,
  Loading,
  formatDateTime,
  formatMoney,
} from "../components/ui"

const statusBadge = (status: string) => {
  switch (status) {
    case "Settled":
      return <span className="badge badge-green">Réglé</span>
    case "Pending":
      return <span className="badge badge-amber">En attente</span>
    default:
      return <span className="badge">{status}</span>
  }
}

export const PaymentsPage = () => {
  const payments = useQuery(listPayments, [])
  const products = useQuery(listProducts, [])
  const organizations = useQuery(listOrganizations, [])

  const [productId, setProductId] = useState("")
  const [organizationId, setOrganizationId] = useState("")
  const [amount, setAmount] = useState("")
  const [currency, setCurrency] = useState("EUR")
  const [source, setSource] = useState("Stripe")
  const [reference, setReference] = useState("")
  const [creating, setCreating] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [settling, setSettling] = useState<string | null>(null)

  const productName = (id: string) => products.data?.find((p) => p.id === id)?.name ?? id
  const organizationName = (id: string | null) =>
    id === null ? "—" : (organizations.data?.find((o) => o.id === id)?.name ?? id)

  const handleCreate = async (event: FormEvent) => {
    event.preventDefault()
    setCreating(true)
    setActionError(null)
    try {
      await recordPayment({
        productId,
        organizationId: organizationId === "" ? null : organizationId,
        amount: Number(amount),
        currency,
        source: source.trim(),
        externalReference: reference.trim(),
      })
      setAmount("")
      setReference("")
      payments.reload()
    } catch (e) {
      setActionError(e instanceof Error ? e.message : String(e))
    } finally {
      setCreating(false)
    }
  }

  const handleSettle = async (paymentId: string) => {
    setSettling(paymentId)
    setActionError(null)
    try {
      await settlePayment(paymentId)
      payments.reload()
    } catch (e) {
      setActionError(e instanceof Error ? e.message : String(e))
    } finally {
      setSettling(null)
    }
  }

  return (
    <>
      <header className="page-header">
        <div>
          <h1 className="page-title">Paiements</h1>
          <p className="page-subtitle">Paiements enregistrés et règlements.</p>
        </div>
      </header>

      <div className="card">
        <h2 className="card-title">Enregistrer un paiement</h2>
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
          <div className="field">
            <label>Montant</label>
            <input
              type="number"
              step="0.01"
              min="0"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              required
              style={{ width: 110 }}
            />
          </div>
          <div className="field">
            <label>Devise</label>
            <select value={currency} onChange={(e) => setCurrency(e.target.value)}>
              <option value="EUR">EUR</option>
              <option value="USD">USD</option>
            </select>
          </div>
          <div className="field">
            <label>Source</label>
            <input value={source} onChange={(e) => setSource(e.target.value)} required />
          </div>
          <div className="field" style={{ flex: 1 }}>
            <label>Référence externe</label>
            <input
              value={reference}
              onChange={(e) => setReference(e.target.value)}
              placeholder="pi_3Nx…"
              required
            />
          </div>
          <button
            className="btn btn-primary"
            disabled={creating || productId === "" || amount === ""}
          >
            {creating ? "Enregistrement…" : "Enregistrer"}
          </button>
        </form>
      </div>

      {actionError !== null ? <ErrorBanner message={actionError} /> : null}
      {payments.error !== null ? <ErrorBanner message={payments.error} /> : null}

      <div className="card">
        {payments.loading ? (
          <Loading />
        ) : (payments.data ?? []).length === 0 ? (
          <EmptyState title="Aucun paiement">
            Enregistrez votre premier paiement (Stripe, GitHub Sponsors…) ci-dessus.
          </EmptyState>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Produit</th>
                <th>Organisation</th>
                <th className="num">Montant</th>
                <th>Source</th>
                <th>Statut</th>
                <th>Date</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {(payments.data ?? []).map((payment) => (
                <tr key={payment.id}>
                  <td>{productName(payment.productId)}</td>
                  <td>{organizationName(payment.organizationId)}</td>
                  <td className="num">{formatMoney(payment.amount, payment.currency)}</td>
                  <td>
                    {payment.source}
                    <div className="dim">{payment.externalReference}</div>
                  </td>
                  <td>{statusBadge(payment.status)}</td>
                  <td className="dim">{formatDateTime(payment.createdAt)}</td>
                  <td style={{ textAlign: "right" }}>
                    {payment.status === "Pending" ? (
                      <button
                        className="btn btn-ghost"
                        disabled={settling === payment.id}
                        onClick={() => void handleSettle(payment.id)}
                      >
                        {settling === payment.id ? "…" : "Régler"}
                      </button>
                    ) : null}
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
