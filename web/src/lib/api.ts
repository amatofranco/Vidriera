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
  plan: string | null;
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
  hasStock: boolean;
  hasSheet: boolean;
  sectionId: string | null;
  sortOrder: number;
  code: string | null;
  price: number | null;
}

export function getProducts(token: string) {
  return request<Product[]>("/api/products", { token });
}

export interface Section {
  id: string;
  name: string;
  sortOrder: number;
  parentSectionId: string | null;
}

export function getSections(token: string) {
  return request<Section[]>("/api/sections", { token });
}

export function createSection(token: string, file?: File, name?: string, parentSectionId?: string) {
  const formData = new FormData();
  if (file) formData.append("file", file);
  if (name) formData.append("name", name);
  if (parentSectionId) formData.append("parentSectionId", parentSectionId);

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

export function assignSectionParent(token: string, sectionId: string, parentSectionId: string | null) {
  return request<void>(`/api/sections/${sectionId}/parent`, {
    method: "PUT",
    token,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ parentSectionId }),
  });
}

export interface TopLevelItem {
  type: "section" | "product";
  id: string;
}

export function reorderSectionChildren(token: string, sectionId: string, items: TopLevelItem[]) {
  return request<void>(`/api/sections/${sectionId}/children/reorder`, {
    method: "PUT",
    token,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ items }),
  });
}

export function updateStock(token: string, productId: string, hasStock: boolean) {
  return request<void>(`/api/products/${productId}/stock`, {
    method: "PUT",
    token,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ hasStock }),
  });
}

export function updateName(token: string, productId: string, name: string) {
  return request<void>(`/api/products/${productId}/name`, {
    method: "PUT",
    token,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ name }),
  });
}

export function updatePrice(token: string, productId: string, price: number | null) {
  return request<void>(`/api/products/${productId}/price`, {
    method: "PUT",
    token,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ price }),
  });
}

export function updateCode(token: string, productId: string, code: string | null) {
  return request<void>(`/api/products/${productId}/code`, {
    method: "PUT",
    token,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ code }),
  });
}

export function createProduct(token: string, file: File, name?: string, code?: string, price?: number) {
  const formData = new FormData();
  formData.append("file", file);
  if (name) formData.append("name", name);
  if (code) formData.append("code", code);
  if (price != null) formData.append("price", String(price));

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

export interface ImportPricesResult {
  updatedCount: number;
  notFoundCodes: string[];
}

export function importPrices(token: string, file: File) {
  const formData = new FormData();
  formData.append("file", file);

  return request<ImportPricesResult>("/api/products/import-prices", {
    method: "POST",
    token,
    body: formData,
  });
}

export async function downloadPriceImportTemplate(token: string): Promise<Blob> {
  const response = await fetch(`${API_URL}/api/products/import-prices/template`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!response.ok) {
    throw new ApiError(`Error ${response.status}`, response.status);
  }

  return response.blob();
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
}

export interface CatalogGenerationProgress {
  stage: "downloading" | "rasterizing";
  current: number;
  total: number;
}

export async function generateCatalog(
  token: string,
  onProgress?: (progress: CatalogGenerationProgress) => void,
  showPrices?: boolean
): Promise<GenerateCatalogResult> {
  const response = await fetch(`${API_URL}/api/catalogs${showPrices ? "?showPrices=true" : ""}`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });

  if (!response.body) {
    throw new ApiError(`Error ${response.status}`, response.status);
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    buffer += decoder.decode(value, { stream: true });

    let newlineIndex;
    while ((newlineIndex = buffer.indexOf("\n")) >= 0) {
      const line = buffer.slice(0, newlineIndex);
      buffer = buffer.slice(newlineIndex + 1);
      if (!line.trim()) continue;

      const payload = JSON.parse(line);
      if (payload.type === "progress") {
        onProgress?.({ stage: payload.stage, current: payload.current, total: payload.total });
      } else if (payload.type === "result") {
        return payload.data as GenerateCatalogResult;
      } else if (payload.type === "error") {
        throw new ApiError(payload.message, payload.status);
      }
    }
  }

  throw new ApiError("La generación del catálogo no devolvió un resultado.", response.status);
}

export function getCurrentCatalog(token: string) {
  return request<GenerateCatalogResult | null>("/api/catalogs/current", { token });
}

export interface OrderItem {
  productName: string;
  code: string | null;
  quantity: number;
}

export interface Order {
  id: string;
  createdAt: string;
  businessName: string;
  storeName: string | null;
  cuit: string;
  vatCondition: string | null;
  phone: string | null;
  email: string;
  city: string | null;
  province: string | null;
  carrier: string | null;
  deliveryAddress: string | null;
  items: OrderItem[];
}

export function getOrders(token: string) {
  return request<Order[]>("/api/orders", { token });
}
