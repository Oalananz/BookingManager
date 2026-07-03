export interface ApiEnvelope<T> {
  data: T;
}

export interface PageMeta {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface Paged<T> {
  data: T[];
  meta: PageMeta;
}
