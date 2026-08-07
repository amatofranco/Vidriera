import type { Product } from "@/lib/api";
import type { ContainerRow } from "./useContainerReorder";

export function useFilteredRows({
  products,
  search,
  containerRows,
}: {
  products: Product[];
  search: string;
  containerRows: (containerId: string | null) => ContainerRow[];
}) {
  const searchQuery = search.trim().toLowerCase();
  const matchesSearch = (name: string) => name.toLowerCase().includes(searchQuery);

  const filteredProducts = products.filter((p) => matchesSearch(p.name));

  function sectionMatchesSearch(sectionId: string, sectionName: string): boolean {
    if (matchesSearch(sectionName)) return true;
    return containerRows(sectionId).some((row) =>
      row.type === "product" ? matchesSearch(row.product.name) : sectionMatchesSearch(row.id, row.section.name)
    );
  }

  const allTopLevelRows = containerRows(null);

  const filteredTopLevelRows = !searchQuery
    ? allTopLevelRows
    : allTopLevelRows.filter((row) => {
        if (row.type === "product") return matchesSearch(row.product.name);
        return sectionMatchesSearch(row.id, row.section.name);
      });

  function visibleSectionChildren(sectionId: string, sectionName: string): ContainerRow[] {
    const children = containerRows(sectionId);
    if (!searchQuery || matchesSearch(sectionName)) return children;
    return children.filter((row) =>
      row.type === "product" ? matchesSearch(row.product.name) : sectionMatchesSearch(row.id, row.section.name)
    );
  }

  return { filteredProducts, filteredTopLevelRows, visibleSectionChildren };
}
