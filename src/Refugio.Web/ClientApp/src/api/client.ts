export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
    this.name = 'ApiError';
  }
}

const JSON_HEADERS = { 'Content-Type': 'application/json' };

async function request<T>(method: string, url: string, body?: unknown): Promise<T> {
  const res = await fetch(url, {
    method,
    credentials: 'include',
    headers: body !== undefined ? JSON_HEADERS : undefined,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  if (res.status === 401) throw new ApiError(401, 'unauthorized');
  if (!res.ok) {
    let msg = res.statusText;
    try { msg = await res.text(); } catch { /* ignore */ }
    throw new ApiError(res.status, msg);
  }
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

/**
 * POST to a Blazor-style action endpoint that responds with a redirect.
 * Uses redirect:'manual' so fetch returns an opaque redirect (status=0, type='opaqueredirect')
 * rather than following to the HTML page. Any opaque-redirect or 2xx is treated as success.
 */
export async function postAction(url: string): Promise<void> {
  const res = await fetch(url, {
    method: 'POST',
    credentials: 'include',
    redirect: 'manual',
  });
  if (res.type === 'opaqueredirect' || res.ok || res.status === 204) return;
  throw new ApiError(res.status || 0, 'Action failed');
}

export const api = {
  get:    <T>(url: string) => request<T>('GET', url),
  post:   <T>(url: string, body?: unknown) => request<T>('POST', url, body),
  put:    <T>(url: string, body?: unknown) => request<T>('PUT', url, body),
  del:    (url: string) => request<void>('DELETE', url),
};

/** Multipart upload — no JSON headers; returns parsed JSON response. */
export async function upload<T>(url: string, form: FormData): Promise<T> {
  const res = await fetch(url, { method: 'POST', credentials: 'include', body: form });
  if (!res.ok) {
    let msg = res.statusText;
    try { msg = await res.text(); } catch { /* ignore */ }
    throw new ApiError(res.status, msg);
  }
  return res.json() as Promise<T>;
}
