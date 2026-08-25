import type { Item } from "@/lib/api";
import type { ContainerRow } from "./useContainerReorder";

export type StockFilter = "all" | "visible" | "hidden";

export function useFilteredRows({
  items,
  search,
  stockFilter,
  containerRows,
}: {
  items: Item[];
  search: string;
  stockFilter: StockFilter;
  containerRows: (containerId: string | null) => ContainerRow[];
}) {
  const searchQuery = search.trim().toLowerCase();
  const hasActiveFilter = searchQuery.length > 0 || stockFilter !== "all";

  const matchesSearch = (name: string) => name.toLowerCase().includes(searchQuery);
  const matchesStock = (p: Item) =>
    stockFilter === "all" ? true : stockFilter === "visible" ? p.hasStock : !p.hasStock;
  const matchesItem = (p: Item) => matchesSearch(p.name) && matchesStock(p);

  const filteredItems = items.filter(matchesItem);

  function sectionMatches(sectionId: string, sectionName: string): boolean {
    if (matchesSearch(sectionName) && stockFilter === "all") return true;
    return containerRows(sectionId).some((row) =>
      row.type === "item" ? matchesItem(row.item) : sectionMatches(row.id, row.section.name)
    );
  }

  const allTopLevelRows = containerRows(null);

  const filteredTopLevelRows = !hasActiveFilter
    ? allTopLevelRows
    : allTopLevelRows.filter((row) => {
        if (row.type === "item") return matchesItem(row.item);
        return sectionMatches(row.id, row.section.name);
      });

  function visibleSectionChildren(sectionId: string, sectionName: string): ContainerRow[] {
    const children = containerRows(sectionId);
    if (!hasActiveFilter || (matchesSearch(sectionName) && stockFilter === "all")) return children;
    return children.filter((row) =>
      row.type === "item" ? matchesItem(row.item) : sectionMatches(row.id, row.section.name)
    );
  }

  return { filteredItems, filteredTopLevelRows, visibleSectionChildren };
}
