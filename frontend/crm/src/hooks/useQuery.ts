import { useCallback, useEffect, useState } from "react"

export interface QueryState<T> {
  readonly data: T | null
  readonly loading: boolean
  readonly error: string | null
  readonly reload: () => void
}

/** Hook fetch minimal : charge `fn` au montage et à chaque changement de deps. */
export const useQuery = <T>(fn: () => Promise<T>, deps: ReadonlyArray<unknown>): QueryState<T> => {
  const [data, setData] = useState<T | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [version, setVersion] = useState(0)

  const reload = useCallback(() => setVersion((v) => v + 1), [])

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(null)
    fn()
      .then((result) => {
        if (!cancelled) {
          setData(result)
          setLoading(false)
        }
      })
      .catch((e: unknown) => {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : String(e))
          setLoading(false)
        }
      })
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, version])

  return { data, loading, error, reload }
}
