import type { Product } from "@/lib/api";
import type { ContainerRow } from "./useContainerReorder";

export type StockFilter = "all" | "visible" | "hidden";

export function useFilteredRows({
  products,
  search,
  stockFilter,
  containerRows,
}: {
  products: Product[];
  search: string;
  stockFilter: StockFilter;
  containerRows: (containerId: string | null) => ContainerRow[];
}) {
  const searchQuery = search.trim().toLowerCase();
  const hasActiveFilter = searchQuery.length > 0 || stockFilter !== "all";

  const matchesSearch = (name: string) => name.toLowerCase().includes(searchQuery);
  const matchesStock = (p: Product) =>
    stockFilter === "all" ? true : stockFilter === "visible" ? p.hasStock : !p.hasStock;
  const matchesProduct = (p: Product) => matchesSearch(p.name) && matchesStock(p);

  const filteredProducts = products.filter(matchesProduct);

  function sectionMatches(sectionId: string, sectionName: string): boolean {
    if (matchesSearch(sectionName) && stockFilter === "all") return true;
    return containerRows(sectionId).some((row) =>
      row.type === "product" ? matchesProduct(row.product) : sectionMatches(row.id, row.section.name)
    );
  }

  const allTopLevelRows = containerRows(null);

  const filteredTopLevelRows = !hasActiveFilter
    ? allTopLevelRows
    : allTopLevelRows.filter((row) => {
        if (row.type === "product") return matchesProduct(row.product);
        return sectionMatches(row.id, row.section.name);
      });

  function visibleSectionChildren(sectionId: string, sectionName: string): ContainerRow[] {
    const children = containerRows(sectionId);
    if (!hasActiveFilter || (matchesSearch(sectionName) && stockFilter === "all")) return children;
    return children.filter((row) =>
      row.type === "product" ? matchesProduct(row.product) : sectionMatches(row.id, row.section.name)
    );
  }

  return { filteredProducts, filteredTopLevelRows, visibleSectionChildren };
}
