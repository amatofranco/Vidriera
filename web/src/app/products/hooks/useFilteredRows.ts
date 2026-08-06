import type { Product } from "@/lib/api";
import type { TopLevelRow } from "./useTopLevelReorder";
import type { SectionChildRow } from "./useSectionReorder";

export function useFilteredRows({
  products,
  search,
  allTopLevelRows,
  sectionChildren,
}: {
  products: Product[];
  search: string;
  allTopLevelRows: TopLevelRow[];
  sectionChildren: (sectionId: string) => SectionChildRow[];
}) {
  const searchQuery = search.trim().toLowerCase();
  const matchesSearch = (name: string) => name.toLowerCase().includes(searchQuery);

  const filteredProducts = products.filter((p) => matchesSearch(p.name));

  function sectionMatchesSearch(sectionId: string, sectionName: string): boolean {
    if (matchesSearch(sectionName)) return true;
    return sectionChildren(sectionId).some((row) =>
      row.type === "product" ? matchesSearch(row.product.name) : sectionMatchesSearch(row.id, row.section.name)
    );
  }

  const filteredTopLevelRows = !searchQuery
    ? allTopLevelRows
    : allTopLevelRows.filter((row) => {
        if (row.type === "product") return matchesSearch(row.product.name);
        return sectionMatchesSearch(row.id, row.section.name);
      });

  function visibleSectionChildren(sectionId: string, sectionName: string): SectionChildRow[] {
    const children = sectionChildren(sectionId);
    if (!searchQuery || matchesSearch(sectionName)) return children;
    return children.filter((row) =>
      row.type === "product" ? matchesSearch(row.product.name) : sectionMatchesSearch(row.id, row.section.name)
    );
  }

  return { filteredProducts, filteredTopLevelRows, visibleSectionChildren };
}
