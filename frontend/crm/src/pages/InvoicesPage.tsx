import { useState, type FormEvent } from "react"
import {
  cancelInvoice,
  createInvoice,
  issueInvoice,
  listInvoices,
  payInvoice,
  type InvoiceDto,
  type InvoiceStatus,
} from "../api/client"
import { useQuery } from "../hooks/useQuery"
import {
  EmptyState,
  ErrorBanner,
  Loading,
  formatDateTime,
  formatMoney,
} from "../components/ui"

const STATUS_OPTIONS: ReadonlyArray<{ value: "" | InvoiceStatus; label: string }> = [
  { value: "", label: "Tous les statuts" },
  { value: "Draft", label: "Brouillon" },
  { value: "Issued", label: "Émise" },
  { value: "Paid", label: "Payée" },
  { value: "Cancelled", label: "Annulée" },
]

/** Badge de statut : Draft gris, Émise bleue, Payée verte, Annulée rouge. */
const statusBadge = (status: string) => {
  switch (status) {
    case "Draft":
      return <span className="badge">Brouillon</span>
    case "Issued":
      return <span className="badge badge-blue">Émise</span>
    case "Paid":
      return <span className="badge badge-green">Payée</span>
    case "Cancelled":
      return <span className="badge badge-red">Annulée</span>
    default:
      return <span className="badge">{status}</span>
  }
}

interface LineDraft {
  description: string
  quantity: string
  unitPrice: string
  currency: string
}

const emptyLine = (): LineDraft => ({
  description: "",
  quantity: "1",
  unitPrice: "",
  currency: "EUR",
})

const PayAction = ({
  invoice,
  busy,
  onPay,
}: {
  invoice: InvoiceDto
  busy: boolean
  onPay: (paymentId: string | null) => void
}) => {
  const [paymentId, setPaymentId] = useState("")
  return (
    <span style={{ display: "inline-flex", gap: 6, alignItems: "center" }}>
      <input
        value={paymentId}
        onChange={(e) => setPaymentId(e.target.value)}
        placeholder="Id paiement (optionnel)"
        style={{ width: 180 }}
        aria-label={`Identifiant de paiement pour ${invoice.invoiceNumber}`}
      />
      <button
        className="btn btn-ghost"
        disabled={busy}
        onClick={() => onPay(paymentId.trim() === "" ? null : paymentId.trim())}
      >
        {busy ? "…" : "Encaisser"}
      </button>
    </span>
  )
}

export const InvoicesPage = () => {
  const [status, setStatus] = useState<"" | InvoiceStatus>("")
  const invoices = useQuery(
    () => listInvoices(status === "" ? undefined : status),
    [status],
  )

  const [customerName, setCustomerName] = useState("")
  const [customerEmail, setCustomerEmail] = useState("")
  const [customerAddress, setCustomerAddress] = useState("")
  const [lines, setLines] = useState<ReadonlyArray<LineDraft>>([emptyLine()])
  const [creating, setCreating] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [actingOn, setActingOn] = useState<string | null>(null)

  const updateLine = (index: number, patch: Partial<LineDraft>) =>
    setLines((current) => [...current.map((l, i) => (i === index ? { ...l, ...patch } : l))])

  const removeLine = (index: number) =>
    setLines((current) => [...current.filter((_, i) => i !== index)])

  const linesValid = lines.every(
    (l) => l.description.trim() !== "" && l.quantity !== "" && l.unitPrice !== "",
  )

  const handleCreate = async (event: FormEvent) => {
    event.preventDefault()
    setCreating(true)
    setActionError(null)
    setNotice(null)
    try {
      const created = await createInvoice({
        customerName: customerName.trim(),
        customerEmail: customerEmail.trim() === "" ? null : customerEmail.trim(),
        customerAddress: customerAddress.trim() === "" ? null : customerAddress.trim(),
        lines: lines.map((l) => ({
          description: l.description.trim(),
          quantity: Number(l.quantity),
          unitPrice: Number(l.unitPrice),
          currency: l.currency,
        })),
      })
      setNotice(`Facture ${created.invoiceNumber} créée en brouillon.`)
      setCustomerName("")
      setCustomerEmail("")
      setCustomerAddress("")
      setLines([emptyLine()])
      invoices.reload()
    } catch (e) {
      setActionError(e instanceof Error ? e.message : String(e))
    } finally {
      setCreating(false)
    }
  }

  const runAction = async (invoiceId: string, action: () => Promise<void>) => {
    setActingOn(invoiceId)
    setActionError(null)
    try {
      await action()
      invoices.reload()
    } catch (e) {
      setActionError(e instanceof Error ? e.message : String(e))
    } finally {
      setActingOn(null)
    }
  }

  return (
    <>
      <header className="page-header">
        <div>
          <h1 className="page-title">Factures</h1>
          <p className="page-subtitle">Création, émission et encaissement des factures.</p>
        </div>
        <div className="field">
          <label>Statut</label>
          <select value={status} onChange={(e) => setStatus(e.target.value as "" | InvoiceStatus)}>
            {STATUS_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>
                {o.label}
              </option>
            ))}
          </select>
        </div>
      </header>

      <div className="card">
        <h2 className="card-title">Créer une facture</h2>
        <form onSubmit={(e) => void handleCreate(e)}>
          <div className="form-row">
            <div className="field" style={{ flex: 1 }}>
              <label>Client</label>
              <input
                value={customerName}
                onChange={(e) => setCustomerName(e.target.value)}
                required
              />
            </div>
            <div className="field" style={{ flex: 1 }}>
              <label>Email (optionnel)</label>
              <input
                type="email"
                value={customerEmail}
                onChange={(e) => setCustomerEmail(e.target.value)}
              />
            </div>
            <div className="field" style={{ flex: 1 }}>
              <label>Adresse (optionnelle)</label>
              <input
                value={customerAddress}
                onChange={(e) => setCustomerAddress(e.target.value)}
              />
            </div>
          </div>

          <h3 className="card-title" style={{ marginTop: 16 }}>Lignes</h3>
          {lines.map((line, index) => (
            <div className="form-row" key={index}>
              <div className="field" style={{ flex: 1 }}>
                <label>Description</label>
                <input
                  value={line.description}
                  onChange={(e) => updateLine(index, { description: e.target.value })}
                  required
                />
              </div>
              <div className="field">
                <label>Quantité</label>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  value={line.quantity}
                  onChange={(e) => updateLine(index, { quantity: e.target.value })}
                  required
                  style={{ width: 90 }}
                />
              </div>
              <div className="field">
                <label>Prix unitaire</label>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  value={line.unitPrice}
                  onChange={(e) => updateLine(index, { unitPrice: e.target.value })}
                  required
                  style={{ width: 110 }}
                />
              </div>
              <div className="field">
                <label>Devise</label>
                <select
                  value={line.currency}
                  onChange={(e) => updateLine(index, { currency: e.target.value })}
                >
                  <option value="EUR">EUR</option>
                  <option value="USD">USD</option>
                </select>
              </div>
              <button
                type="button"
                className="btn btn-ghost"
                disabled={lines.length === 1}
                onClick={() => removeLine(index)}
                title="Supprimer la ligne"
              >
                ✕
              </button>
            </div>
          ))}
          <div className="form-row" style={{ marginTop: 8 }}>
            <button
              type="button"
              className="btn"
              onClick={() => setLines((current) => [...current, emptyLine()])}
            >
              + Ajouter une ligne
            </button>
            <button
              className="btn btn-primary"
              disabled={creating || customerName.trim() === "" || !linesValid}
            >
              {creating ? "Création…" : "Créer la facture"}
            </button>
          </div>
        </form>
      </div>

      {notice !== null ? <div className="notice-banner">{notice}</div> : null}
      {actionError !== null ? <ErrorBanner message={actionError} /> : null}
      {invoices.error !== null ? <ErrorBanner message={invoices.error} /> : null}

      <div className="card section">
        {invoices.loading ? (
          <Loading />
        ) : (invoices.data ?? []).length === 0 ? (
          <EmptyState title="Aucune facture">
            Créez votre première facture ci-dessus, puis émettez-la pour l'envoyer au client.
          </EmptyState>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Numéro</th>
                <th>Client</th>
                <th className="num">Total</th>
                <th>Statut</th>
                <th>Créée le</th>
                <th>Émise le</th>
                <th>Payée le</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {(invoices.data ?? []).map((invoice) => (
                <tr key={invoice.id}>
                  <td>
                    <strong>{invoice.invoiceNumber}</strong>
                    <div className="dim">
                      {invoice.lines.length} ligne{invoice.lines.length > 1 ? "s" : ""}
                    </div>
                  </td>
                  <td>
                    {invoice.customerName}
                    {invoice.customerEmail !== null ? (
                      <div className="dim">{invoice.customerEmail}</div>
                    ) : null}
                  </td>
                  <td className="num">{formatMoney(invoice.total, invoice.currency)}</td>
                  <td>{statusBadge(invoice.status)}</td>
                  <td className="dim">{formatDateTime(invoice.createdAt)}</td>
                  <td className="dim">
                    {invoice.issuedAt === null ? "—" : formatDateTime(invoice.issuedAt)}
                  </td>
                  <td className="dim">
                    {invoice.paidAt === null ? "—" : formatDateTime(invoice.paidAt)}
                  </td>
                  <td style={{ textAlign: "right", whiteSpace: "nowrap" }}>
                    {invoice.status === "Draft" ? (
                      <button
                        className="btn btn-ghost"
                        disabled={actingOn === invoice.id}
                        onClick={() => void runAction(invoice.id, () => issueInvoice(invoice.id))}
                      >
                        {actingOn === invoice.id ? "…" : "Émettre"}
                      </button>
                    ) : null}
                    {invoice.status === "Issued" ? (
                      <PayAction
                        invoice={invoice}
                        busy={actingOn === invoice.id}
                        onPay={(paymentId) =>
                          void runAction(invoice.id, () => payInvoice(invoice.id, paymentId))
                        }
                      />
                    ) : null}
                    {invoice.status === "Draft" || invoice.status === "Issued" ? (
                      <button
                        className="btn btn-ghost"
                        disabled={actingOn === invoice.id}
                        onClick={() => void runAction(invoice.id, () => cancelInvoice(invoice.id))}
                      >
                        {actingOn === invoice.id ? "…" : "Annuler"}
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
