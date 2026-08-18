"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { MAX_FILE_SIZE_LABEL } from "@/lib/file-size";
import { Labels } from "@/lib/labels";
import type { Product, Section } from "@/lib/api";

import { useProductsData } from "./hooks/useProductsData";
import { useCompanyLogo } from "./hooks/useCompanyLogo";
import { useContainerReorder } from "./hooks/useContainerReorder";
import { useBulkStockToggle } from "./hooks/useBulkStockToggle";
import { useBulkDelete } from "./hooks/useBulkDelete";
import { useBulkAssign } from "./hooks/useBulkAssign";
import { useCreateProduct } from "./hooks/useCreateProduct";
import { useCreateSection } from "./hooks/useCreateSection";
import { useProductActions } from "./hooks/useProductActions";
import { useSectionActions } from "./hooks/useSectionActions";
import { useCatalogGeneration } from "./hooks/useCatalogGeneration";
import { useFilteredRows, type StockFilter } from "./hooks/useFilteredRows";

import { CompanyHeader } from "./components/CompanyHeader";
import { UploadCreateForm } from "./components/UploadCreateForm";
import { ProductRow } from "./components/ProductRow";
import { SectionRow } from "./components/SectionRow";
import { BulkActionsToolbar } from "./components/BulkActionsToolbar";
import { BulkAssignBar } from "./components/BulkAssignBar";
import { BulkDeleteConfirmBar } from "./components/BulkDeleteConfirmBar";
import { CatalogPanel } from "./components/CatalogPanel";

export default function ProductsPage() {
  const router = useRouter();
  const { auth, isLoading: authLoading, logout } = useAuth();

  const [search, setSearch] = useState("");
  const [stockFilter, setStockFilter] = useState<StockFilter>("all");
  const [collapsedSectionIds, setCollapsedSectionIds] = useState<Set<string>>(new Set());

  function toggleSectionCollapsed(sectionId: string) {
    setCollapsedSectionIds((prev) => {
      const next = new Set(prev);
      if (next.has(sectionId)) next.delete(sectionId);
      else next.add(sectionId);
      return next;
    });
  }

  useEffect(() => {
    if (!authLoading && !auth) {
      router.replace("/login");
    }
  }, [auth, authLoading, router]);

  const {
    products,
    setProducts,
    sections,
    setSections,
    isLoading,
    error,
    setError,
    loadProducts,
    loadSections,
  } = useProductsData(auth, logout);

  const { logoUrl, setLogoUrl } = useCompanyLogo(auth);

  const { draggedId, setDraggedId, containerRows, handleDrop, handleMoveToPosition } = useContainerReorder({
    auth,
    products,
    setProducts,
    sections,
    setSections,
    loadProducts,
    loadSections,
    setError,
  });

  const { handleToggleStock, handleBulkStockToggle, handleToggleSectionStock } = useBulkStockToggle({
    auth,
    products,
    setProducts,
    setError,
    loadProducts,
  });

  const { pendingBulkDelete, setPendingBulkDelete, isBulkDeleting, requestBulkDelete, handleConfirmBulkDelete } =
    useBulkDelete({ auth, setProducts, setError });

  const {
    isBulkAssigningSection,
    bulkAssignSelectedIds,
    bulkAssignTargetId,
    setBulkAssignTargetId,
    isApplyingBulkAssign,
    toggleBulkAssignMode,
    toggleBulkAssignSelected,
    selectForBulkAssign,
    handleApplyBulkAssign,
  } = useBulkAssign({ auth, products, loadProducts, setError });

  const { isCreating, uploadProgress, handleCreateProduct } = useCreateProduct({ auth, setProducts, setError });
  const { isCreatingSection, uploadProgress: sectionUploadProgress, handleCreateSection } = useCreateSection({
    auth,
    setSections,
    setError,
  });

  const { confirmingDeleteId, setConfirmingDeleteId, isDeleting, handleUploadSheet, handleDeleteProduct } =
    useProductActions({ auth, setProducts, setError });

  const {
    confirmingDeleteSectionId,
    setConfirmingDeleteSectionId,
    isDeletingSection,
    handleDeleteSection,
    handleAssignSection,
    handleAssignSectionParent,
  } = useSectionActions({ auth, setSections, setProducts, loadProducts, loadSections, setError });

  const { isGenerating, catalogResult, generationProgress, selectableCount, handleGenerateCatalog } = useCatalogGeneration({
    auth,
    products,
    setError,
  });

  const { filteredProducts, filteredTopLevelRows, visibleSectionChildren } = useFilteredRows({
    products,
    search,
    stockFilter,
    containerRows,
  });

  if (authLoading || !auth) {
    return null;
  }

  if (isLoading) {
    return (
      <div
        className="flex w-full flex-1 items-center justify-center px-4 py-10"
        style={{
          backgroundImage:
            "radial-gradient(ellipse 80% 60% at 50% 0%, rgba(240,220,174,0.55) 0%, rgba(160,110,60,0.55) 45%, rgba(90,55,25,0.75) 100%), url('/login-bg.jpg')",
          backgroundSize: "100% 100%, 240%",
          backgroundPosition: "center, center 38%",
          backgroundRepeat: "no-repeat, no-repeat",
          backgroundAttachment: "fixed, fixed",
        }}
      >
        <div className="flex flex-col items-center justify-center gap-3 text-zinc-200">
          <span className="h-8 w-8 animate-spin rounded-full border-4 border-white/20 border-t-[#c9a86a]" />
          <p>{Labels.loadingProducts}</p>
        </div>
      </div>
    );
  }

  function requestBulkDeleteSelected() {
    const targets = products.filter((p) => bulkAssignSelectedIds.has(p.id));
    requestBulkDelete(targets, Labels.selectedProductsLabel(targets.length), toggleBulkAssignMode);
  }

  function requestBulkDeleteAll() {
    requestBulkDelete(
      filteredProducts,
      search.trim()
        ? Labels.filteredProductsLabel(filteredProducts.length)
        : Labels.allProductsLabel(filteredProducts.length)
    );
  }

  function deepSectionProducts(sectionId: string): Product[] {
    const direct = products.filter((p) => p.sectionId === sectionId);
    const childSections = sections.filter((s) => s.parentSectionId === sectionId);
    return [...direct, ...childSections.flatMap((cs) => deepSectionProducts(cs.id))];
  }

  function sectionCheckboxChecked(sectionId: string) {
    const members = deepSectionProducts(sectionId);
    if (members.length === 0) return false;
    return isBulkAssigningSection
      ? members.every((p) => bulkAssignSelectedIds.has(p.id))
      : members.every((p) => p.hasStock);
  }

  async function handleToggleSectionCheckbox(section: { id: string; name: string }) {
    const members = deepSectionProducts(section.id);
    if (members.length === 0) return;
    const nextValue = !sectionCheckboxChecked(section.id);
    if (isBulkAssigningSection) {
      selectForBulkAssign(members.map((p) => p.id), nextValue);
      return;
    }
    await handleToggleSectionStock(members, nextValue, section.name);
  }

  function renderProductRowComponent(product: Product, sectionId: string | null) {
    const siblingRows = containerRows(sectionId);
    const positionMax = siblingRows.length;
    const positionValue = siblingRows.findIndex((r) => r.id === product.id) + 1;

    return (
      <ProductRow
        key={product.id}
        product={product}
        sections={sections}
        positionValue={positionValue}
        positionMax={positionMax}
        isDragged={draggedId === product.id}
        isBulkAssigningSection={isBulkAssigningSection}
        isChecked={isBulkAssigningSection ? bulkAssignSelectedIds.has(product.id) : product.hasStock}
        checkboxTitle={isBulkAssigningSection ? Labels.selectForBulkAssignTitle : Labels.hasStockTitle}
        isDeleting={isDeleting}
        deleteDisabled={isBulkDeleting}
        confirmingDelete={confirmingDeleteId === product.id}
        onDragStart={() => setDraggedId(product.id)}
        onDragEnd={() => setDraggedId(null)}
        onDragOver={(e) => e.preventDefault()}
        onDrop={() => handleDrop(sectionId, product.id)}
        onMoveToPosition={(v) => handleMoveToPosition(sectionId, product.id, v)}
        onToggleCheckbox={() =>
          isBulkAssigningSection ? toggleBulkAssignSelected(product.id) : handleToggleStock(product)
        }
        onAssignSection={(sid) => handleAssignSection(product, sid)}
        onUploadSheet={(file) => handleUploadSheet(product, file)}
        onRequestDelete={() => setConfirmingDeleteId(product.id)}
        onConfirmDelete={() => handleDeleteProduct(product)}
        onCancelDelete={() => setConfirmingDeleteId(null)}
      />
    );
  }

  function renderSectionRowComponent(section: Section, containerId: string | null) {
    const siblingRows = containerRows(containerId);
    const positionMax = siblingRows.length;
    const positionValue = siblingRows.findIndex((r) => r.id === section.id) + 1;
    const parentOptions = sections.filter((s) => s.parentSectionId === null && s.id !== section.id);
    const canHaveParent = !sections.some((s) => s.parentSectionId === section.id);
    const children = visibleSectionChildren(section.id, section.name);

    return (
      <SectionRow
        key={section.id}
        section={section}
        positionValue={positionValue}
        positionMax={positionMax}
        isDragged={draggedId === section.id}
        isChecked={sectionCheckboxChecked(section.id)}
        checkboxTitle={
          isBulkAssigningSection ? Labels.selectAllSectionMembersTitle : Labels.toggleSectionStockTitle
        }
        checkboxDisabled={deepSectionProducts(section.id).length === 0}
        isBulkAssigningSection={isBulkAssigningSection}
        confirmingDelete={confirmingDeleteSectionId === section.id}
        isDeletingSection={isDeletingSection}
        isCollapsed={collapsedSectionIds.has(section.id) && !search.trim()}
        memberCount={deepSectionProducts(section.id).length}
        parentOptions={parentOptions}
        canHaveParent={canHaveParent}
        onDragStart={() => setDraggedId(section.id)}
        onDragEnd={() => setDraggedId(null)}
        onDragOver={(e) => e.preventDefault()}
        onDrop={() => handleDrop(containerId, section.id)}
        onMoveToPosition={(v) => handleMoveToPosition(containerId, section.id, v)}
        onToggleCheckbox={() => handleToggleSectionCheckbox(section)}
        onToggleCollapse={() => toggleSectionCollapsed(section.id)}
        onChangeParent={(parentSectionId) => handleAssignSectionParent(section, parentSectionId)}
        onRequestDelete={() => setConfirmingDeleteSectionId(section.id)}
        onConfirmDelete={() => handleDeleteSection(section)}
        onCancelDelete={() => setConfirmingDeleteSectionId(null)}
      >
        {children.length > 0
          ? children.map((row) =>
              row.type === "section"
                ? renderSectionRowComponent(row.section, section.id)
                : renderProductRowComponent(row.product, section.id)
            )
          : null}
      </SectionRow>
    );
  }

  return (
    <div
      className="w-full flex-1 px-4 py-10"
      style={{
        backgroundImage:
          "radial-gradient(ellipse 80% 60% at 50% 0%, rgba(240,220,174,0.55) 0%, rgba(160,110,60,0.55) 45%, rgba(90,55,25,0.75) 100%), url('/login-bg.jpg')",
        backgroundSize: "100% 100%, 240%",
        backgroundPosition: "center, center 38%",
        backgroundRepeat: "no-repeat, no-repeat",
        backgroundAttachment: "fixed, fixed",
      }}
    >
      <div className="fixed top-4 left-4 z-10">
        <Image src="/vidriera-logo.png" alt={Labels.logoAlt} width={1000} height={245} className="h-11 w-auto" />
      </div>
      <div className="mx-auto w-full max-w-3xl">
        <CompanyHeader
          auth={auth}
          logoUrl={logoUrl}
          onLogoChanged={setLogoUrl}
          onError={setError}
          logout={logout}
        />

        <UploadCreateForm
          label={Labels.productSheetFormLabel}
          fileButtonLabel={Labels.chooseFilesPlural}
          multiple
          maxSizeLabel={MAX_FILE_SIZE_LABEL}
          isSubmitting={isCreating}
          submitTitle={(count) => (count > 1 ? Labels.uploadProductsSubmitTitle(count) : Labels.newProductSubmitTitle)}
          progress={uploadProgress}
          onSubmit={handleCreateProduct}
          marginBottomClassName="mb-3"
          isbnEnabled
        />

        <UploadCreateForm
          label={Labels.sectionCoverFormLabel}
          fileButtonLabel={Labels.chooseFilesPlural}
          multiple
          maxSizeLabel={MAX_FILE_SIZE_LABEL}
          isSubmitting={isCreatingSection}
          submitTitle={(count) => (count > 1 ? Labels.uploadSectionsSubmitTitle(count) : Labels.newSectionSubmitTitle)}
          progress={sectionUploadProgress}
          onSubmit={handleCreateSection}
          marginBottomClassName="mb-6"
        />

        {error && (
          <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700 dark:bg-red-950 dark:text-red-300">
            {error}
          </p>
        )}

        {(products.length > 0 || sections.length > 0) && (
          <BulkActionsToolbar
            search={search}
            onSearchChange={setSearch}
            stockFilter={stockFilter}
            onStockFilterChange={setStockFilter}
            isBulkAssigningSection={isBulkAssigningSection}
            isApplyingBulkAssign={isApplyingBulkAssign}
            hasSections={sections.length > 0}
            onMarkAll={() =>
              isBulkAssigningSection
                ? selectForBulkAssign(filteredProducts.map((p) => p.id), true)
                : handleBulkStockToggle(true, search)
            }
            onUnmarkAll={() =>
              isBulkAssigningSection
                ? selectForBulkAssign(filteredProducts.map((p) => p.id), false)
                : handleBulkStockToggle(false, search)
            }
            onRequestDeleteAll={requestBulkDeleteAll}
            onToggleBulkAssignMode={toggleBulkAssignMode}
          />
        )}

        {isBulkAssigningSection && (
          <BulkAssignBar
            selectedCount={bulkAssignSelectedIds.size}
            sections={sections}
            targetId={bulkAssignTargetId}
            onTargetChange={setBulkAssignTargetId}
            isApplying={isApplyingBulkAssign}
            onApply={handleApplyBulkAssign}
            onDelete={requestBulkDeleteSelected}
          />
        )}

        {pendingBulkDelete && (
          <BulkDeleteConfirmBar
            label={pendingBulkDelete.label}
            isDeleting={isBulkDeleting}
            onConfirm={handleConfirmBulkDelete}
            onCancel={() => setPendingBulkDelete(null)}
          />
        )}

        {products.length === 0 && sections.length === 0 ? (
          <p className="text-zinc-200">{Labels.noProductsOrSectionsYet}</p>
        ) : filteredTopLevelRows.length === 0 ? (
          <p className="mb-8 text-zinc-200">{Labels.noSearchMatches}</p>
        ) : (
          <ul className="mb-6 max-h-[520px] divide-y divide-zinc-200 overflow-y-auto rounded-xl border border-black/10 bg-[#ecdcc0] shadow-lg dark:divide-zinc-800 dark:border-white/10 dark:bg-zinc-900">
            {filteredTopLevelRows.map((row) =>
              row.type === "section"
                ? renderSectionRowComponent(row.section, null)
                : renderProductRowComponent(row.product, null)
            )}
          </ul>
        )}

        <CatalogPanel
          selectableCount={selectableCount}
          isGenerating={isGenerating}
          generationProgress={generationProgress}
          onGenerate={handleGenerateCatalog}
          catalogResult={catalogResult}
        />
      </div>
    </div>
  );
}
