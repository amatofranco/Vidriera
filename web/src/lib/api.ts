const API_URL = process.env.NEXT_PUBLIC_API_URL;

export class ApiError extends Error {
  constructor(message: string, public status: number) {
    super(message);
  }
}

async function request<T>(
  path: string,
  options: RequestInit & { token?: string } = {}
): Promise<T> {
  const { token, headers, ...rest } = options;

  const response = await fetch(`${API_URL}${path}`, {
    ...rest,
    headers: {
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...headers,
    },
  });

  if (!response.ok) {
    let message = `Error ${response.status}`;
    try {
      const body = await response.json();
      message = body.detail ?? body.title ?? message;
    } catch {
      // sin body JSON, nos quedamos con el mensaje genérico
    }
    throw new ApiError(message, response.status);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export interface LoginResult {
  token: string;
  userId: string;
  companyId: string;
  companyName: string;
  name: string;
  email: string;
}

export function login(email: string, password: string) {
  return request<LoginResult>("/api/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });
}

export interface Product {
  id: string;
  name: string;
  code: string | null;
  hasStock: boolean;
  hasSheet: boolean;
  sectionId: string | null;
  sortOrder: number;
}

export function getProducts(token: string) {
  return request<Product[]>("/api/products", { token });
}

export interface Section {
  id: string;
  name: string;
  sortOrder: number;
}

export function getSections(token: string) {
  return request<Section[]>("/api/sections", { token });
}

export function createSection(token: string, file: File, name?: string) {
  const formData = new FormData();
  formData.append("file", file);
  if (name) formData.append("name", name);

  return request<Section>("/api/sections", {
    method: "POST",
    token,
    body: formData,
  });
}

export function deleteSection(token: string, sectionId: string) {
  return request<void>(`/api/sections/${sectionId}`, {
    method: "DELETE",
    token,
  });
}

export function assignProductSection(token: string, productId: string, sectionId: string | null) {
  return request<void>(`/api/products/${productId}/section`, {
    method: "PUT",
    token,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ sectionId }),
  });
}

export function reorderSectionProducts(token: string, sectionId: string, productIds: string[]) {
  return request<void>(`/api/sections/${sectionId}/products/reorder`, {
    method: "PUT",
    token,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ productIds }),
  });
}

export interface TopLevelItem {
  type: "section" | "product";
  id: string;
}

export function updateStock(token: string, productId: string, hasStock: boolean) {
  return request<void>(`/api/products/${productId}/stock`, {
    method: "PUT",
    token,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ hasStock }),
  });
}

export function createProduct(token: string, file: File, name?: string) {
  const formData = new FormData();
  formData.append("file", file);
  if (name) formData.append("name", name);

  return request<Product>("/api/products", {
    method: "POST",
    token,
    body: formData,
  });
}

export function deleteProduct(token: string, productId: string) {
  return request<void>(`/api/products/${productId}`, {
    method: "DELETE",
    token,
  });
}

export function reorderTopLevel(token: string, items: TopLevelItem[]) {
  return request<void>("/api/products/reorder", {
    method: "PUT",
    token,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ items }),
  });
}

export function uploadSheet(token: string, productId: string, file: File) {
  const formData = new FormData();
  formData.append("file", file);

  return request<void>(`/api/products/${productId}/sheet`, {
    method: "POST",
    token,
    body: formData,
  });
}

export function uploadCompanyLogo(token: string, file: File) {
  const formData = new FormData();
  formData.append("file", file);

  return request<void>("/api/company/logo", {
    method: "POST",
    token,
    body: formData,
  });
}

export async function fetchCompanyLogoUrl(token: string): Promise<string | null> {
  const response = await fetch(`${API_URL}/api/company/logo`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!response.ok) {
    return null;
  }

  const blob = await response.blob();
  return URL.createObjectURL(blob);
}

export interface GenerateCatalogResult {
  id: string;
  url: string;
  expiresAt: string | null;
}

export function generateCatalog(token: string, productIds: string[]) {
  return request<GenerateCatalogResult>("/api/catalogs", {
    method: "POST",
    token,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ productIds }),
  });
}
