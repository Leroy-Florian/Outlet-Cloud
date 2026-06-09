import { Effect } from "effect"

export class ApiError {
  readonly _tag = "ApiError"
  constructor(
    readonly status: number,
    readonly message: string,
  ) {}
}

export const fetchJson = <T>(path: string): Effect.Effect<T, ApiError> =>
  Effect.tryPromise({
    try: async () => {
      const response = await fetch(path)
      if (!response.ok) {
        throw new ApiError(response.status, `Request to ${path} failed`)
      }
      return (await response.json()) as T
    },
    catch: (error) =>
      error instanceof ApiError ? error : new ApiError(0, String(error)),
  })

export interface TrackedPackageDto {
  readonly registry: string
  readonly packageId: string
}

export interface ProductDto {
  readonly id: string
  readonly name: string
  readonly description: string | null
  readonly packages: ReadonlyArray<TrackedPackageDto>
  readonly repositories: ReadonlyArray<string>
  readonly createdAt: string
}

export interface OrganizationDto {
  readonly id: string
  readonly name: string
  readonly website: string | null
  readonly createdAt: string
}

export interface ProspectDto {
  readonly id: string
  readonly productId: string
  readonly organizationId: string | null
  readonly name: string
  readonly email: string
  readonly company: string | null
  readonly stage: string
  readonly createdAt: string
}

export interface DownloadTrendPointDto {
  readonly capturedAt: string
  readonly totalDownloads: number
  readonly delta: number
}

export interface RepositorySnapshotDto {
  readonly repository: string
  readonly openIssues: number
  readonly stars: number
  readonly forks: number
  readonly capturedAt: string
}

export interface PaymentDto {
  readonly id: string
  readonly productId: string
  readonly organizationId: string | null
  readonly amount: number
  readonly currency: string
  readonly source: string
  readonly externalReference: string
  readonly status: string
  readonly createdAt: string
}

export const listProducts = fetchJson<ReadonlyArray<ProductDto>>("/api/products/")
export const listOrganizations = fetchJson<ReadonlyArray<OrganizationDto>>("/api/organizations/")
export const listProspects = fetchJson<ReadonlyArray<ProspectDto>>("/api/prospects/")
export const listPayments = fetchJson<ReadonlyArray<PaymentDto>>("/api/payments/")
export const getDownloadTrend = (
  productId: string,
  registry: string,
  packageId: string,
) =>
  fetchJson<ReadonlyArray<DownloadTrendPointDto>>(
    `/api/products/${productId}/packages/${registry}/${encodeURIComponent(packageId)}/trend`,
  )
export const getRepositoryHistory = (productId: string, repository: string) =>
  fetchJson<ReadonlyArray<RepositorySnapshotDto>>(
    `/api/products/${productId}/repositories/${repository}/history`,
  )
