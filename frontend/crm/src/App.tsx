import { Effect } from "effect"
import { useEffect, useState } from "react"
import {
  listOrganizations,
  listPayments,
  listProducts,
  listProspects,
  type OrganizationDto,
  type PaymentDto,
  type ProductDto,
  type ProspectDto,
} from "./api/client"

const useApi = <T,>(effect: Effect.Effect<T, unknown>, initial: T): T => {
  const [value, setValue] = useState(initial)

  useEffect(() => {
    void Effect.runPromise(
      effect.pipe(Effect.catchAll(() => Effect.succeed(initial))),
    ).then(setValue)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return value
}

export const App = () => {
  const products = useApi<ReadonlyArray<ProductDto>>(listProducts, [])
  const organizations = useApi<ReadonlyArray<OrganizationDto>>(listOrganizations, [])
  const prospects = useApi<ReadonlyArray<ProspectDto>>(listProspects, [])
  const payments = useApi<ReadonlyArray<PaymentDto>>(listPayments, [])

  const productName = (id: string) =>
    products.find((p) => p.id === id)?.name ?? id
  const organizationName = (id: string | null) =>
    id === null ? null : (organizations.find((o) => o.id === id)?.name ?? id)

  return (
    <main style={{ fontFamily: "system-ui", margin: "2rem auto", maxWidth: 960 }}>
      <h1>CRM produits</h1>

      <section>
        <h2>Produits ({products.length})</h2>
        <ul>
          {products.map((p) => (
            <li key={p.id}>
              <strong>{p.name}</strong>
              {p.description ? ` — ${p.description}` : null}
              {" · "}
              {p.packages.length} package(s), {p.repositories.length} repo(s)
            </li>
          ))}
        </ul>
      </section>

      <section>
        <h2>Prospects ({prospects.length})</h2>
        <ul>
          {prospects.map((p) => (
            <li key={p.id}>
              [{productName(p.productId)}] {p.name} — {p.email} —{" "}
              <strong>{p.stage}</strong>
              {organizationName(p.organizationId) !== null
                ? ` (${organizationName(p.organizationId)})`
                : null}
            </li>
          ))}
        </ul>
      </section>

      <section>
        <h2>Paiements ({payments.length})</h2>
        <ul>
          {payments.map((p) => (
            <li key={p.id}>
              [{productName(p.productId)}] {p.amount} {p.currency} via {p.source}{" "}
              — <strong>{p.status}</strong>
              {organizationName(p.organizationId) !== null
                ? ` (${organizationName(p.organizationId)})`
                : null}
            </li>
          ))}
        </ul>
      </section>
    </main>
  )
}
