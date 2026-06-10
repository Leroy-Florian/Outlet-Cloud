import { useState, type FormEvent } from "react"
import { Link } from "react-router-dom"
import {
  archiveProduct,
  createProduct,
  listProducts,
  updateProduct,
  type ProductDto,
} from "../api/client"
import { useQuery } from "../hooks/useQuery"
import { EmptyState, ErrorBanner, Loading, formatDateTime } from "../components/ui"

const ProductRow = ({
  product,
  onChanged,
  onError,
}: {
  product: ProductDto
  onChanged: () => void
  onError: (message: string) => void
}) => {
  const [editing, setEditing] = useState(false)
  const [name, setName] = useState(product.name)
  const [description, setDescription] = useState(product.description ?? "")
  const [busy, setBusy] = useState(false)

  const run = async (action: () => Promise<unknown>) => {
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

  if (editing) {
    return (
      <tr>
        <td colSpan={5}>
          <div className="form-row">
            <div className="field">
              <label>Nom</label>
              <input value={name} onChange={(e) => setName(e.target.value)} />
            </div>
            <div className="field" style={{ flex: 1 }}>
              <label>Description</label>
              <input value={description} onChange={(e) => setDescription(e.target.value)} />
            </div>
            <button
              className="btn btn-primary"
              disabled={busy || name.trim() === ""}
              onClick={() =>
                void run(() =>
                  updateProduct(product.id, name.trim(), description.trim() || null),
                ).then(() => setEditing(false))
              }
            >
              Enregistrer
            </button>
            <button className="btn" disabled={busy} onClick={() => setEditing(false)}>
              Annuler
            </button>
          </div>
        </td>
      </tr>
    )
  }

  return (
    <tr>
      <td>
        <Link to={`/produits/${product.id}`} style={{ fontWeight: 600 }}>
          {product.name}
        </Link>
        {product.isArchived ? (
          <span className="badge badge-amber" style={{ marginLeft: 8 }}>
            Archivé
          </span>
        ) : null}
        <div className="dim">{product.description ?? "—"}</div>
      </td>
      <td className="num">{product.packages.length}</td>
      <td className="num">{product.repositories.length}</td>
      <td className="dim">{formatDateTime(product.createdAt)}</td>
      <td style={{ textAlign: "right", whiteSpace: "nowrap" }}>
        <button className="btn btn-ghost" disabled={busy} onClick={() => setEditing(true)}>
          Modifier
        </button>
        {!product.isArchived ? (
          <button
            className="btn btn-ghost btn-danger"
            disabled={busy}
            onClick={() => void run(() => archiveProduct(product.id))}
          >
            Archiver
          </button>
        ) : null}
      </td>
    </tr>
  )
}

export const ProductsPage = () => {
  const products = useQuery(listProducts, [])
  const [name, setName] = useState("")
  const [description, setDescription] = useState("")
  const [creating, setCreating] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)

  const handleCreate = async (event: FormEvent) => {
    event.preventDefault()
    setCreating(true)
    setActionError(null)
    try {
      await createProduct(name.trim(), description.trim() || null)
      setName("")
      setDescription("")
      products.reload()
    } catch (e) {
      setActionError(e instanceof Error ? e.message : String(e))
    } finally {
      setCreating(false)
    }
  }

  return (
    <>
      <header className="page-header">
        <div>
          <h1 className="page-title">Produits</h1>
          <p className="page-subtitle">Catalogue des produits suivis par le CRM.</p>
        </div>
      </header>

      <div className="card">
        <h2 className="card-title">Nouveau produit</h2>
        <form className="form-row" onSubmit={(e) => void handleCreate(e)}>
          <div className="field">
            <label>Nom</label>
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Outlet CLI"
              required
            />
          </div>
          <div className="field" style={{ flex: 1 }}>
            <label>Description</label>
            <input
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Description (optionnelle)"
            />
          </div>
          <button className="btn btn-primary" disabled={creating || name.trim() === ""}>
            {creating ? "Création…" : "Créer"}
          </button>
        </form>
      </div>

      {actionError !== null ? <ErrorBanner message={actionError} /> : null}
      {products.error !== null ? <ErrorBanner message={products.error} /> : null}

      <div className="card">
        {products.loading ? (
          <Loading />
        ) : products.data === null || products.data.length === 0 ? (
          <EmptyState title="Aucun produit">
            Créez votre premier produit pour commencer à suivre packages et repositories.
          </EmptyState>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Produit</th>
                <th className="num">Packages</th>
                <th className="num">Repos</th>
                <th>Créé le</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {products.data.map((product) => (
                <ProductRow
                  key={product.id}
                  product={product}
                  onChanged={products.reload}
                  onError={setActionError}
                />
              ))}
            </tbody>
          </table>
        )}
      </div>
    </>
  )
}
