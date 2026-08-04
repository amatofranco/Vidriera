import type { Product } from "@/lib/api";
import type { TopLevelRow } from "./useTopLevelReorder";

export function useFilteredRows({
  products,
  search,
  allTopLevelRows,
  sectionMembers,
}: {
  products: Product[];
  search: string;
  allTopLevelRows: TopLevelRow[];
  sectionMembers: (sectionId: string) => Product[];
}) {
  const searchQuery = search.trim().toLowerCase();
  const matchesSearch = (name: string) => name.toLowerCase().includes(searchQuery);

  const filteredProducts = products.filter((p) => matchesSearch(p.name));

  const filteredTopLevelRows = !searchQuery
    ? allTopLevelRows
    : allTopLevelRows.filter((row) => {
        if (row.type === "product") return matchesSearch(row.product.name);
        return matchesSearch(row.section.name) || sectionMembers(row.section.id).some((m) => matchesSearch(m.name));
      });

  function visibleSectionMembers(sectionId: string, sectionName: string) {
    const members = sectionMembers(sectionId);
    if (!searchQuery || matchesSearch(sectionName)) return members;
    return members.filter((m) => matchesSearch(m.name));
  }

  return { filteredProducts, filteredTopLevelRows, visibleSectionMembers };
}
