"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth-context";
import { Labels } from "@/lib/labels";
import { Messages, apiErrorMessage } from "@/lib/messages";
import {
  createOrderFormField,
  deleteOrderFormField,
  getOrderFormFields,
  reorderOrderFormFields,
  updateOrderFormField,
  type OrderFormField,
} from "@/lib/api";
import { CompanyHeader } from "../items/components/CompanyHeader";
import { useCompanyLogo } from "../items/hooks/useCompanyLogo";
import { DragHandle, DeleteConfirmActions } from "../items/components/RowControls";

const FIELD_TYPES: { value: string; label: string }[] = [
  { value: "FreeText", label: Labels.orderFieldTypeFreeText },
  { value: "Name", label: Labels.orderFieldTypeName },
  { value: "Email", label: Labels.orderFieldTypeEmail },
  { value: "Cuit", label: Labels.orderFieldTypeCuit },
  { value: "Phone", label: Labels.orderFieldTypePhone },
  { value: "Province", label: Labels.orderFieldTypeProvince },
  { value: "VatCondition", label: Labels.orderFieldTypeVatCondition },
];

function fieldTypeLabel(fieldType: string) {
  return FIELD_TYPES.find((t) => t.value === fieldType)?.label ?? fieldType;
}

export default function OrderFormPage() {
  const router = useRouter();
  const { auth, isLoading: authLoading, logout } = useAuth();

  useEffect(() => {
    if (!authLoading && !auth) {
      router.replace("/login");
    } else if (!authLoading && auth && !auth.showOrders) {
      router.replace("/items");
    }
  }, [auth, authLoading, router]);

  const { logoUrl, setLogoUrl, isLoading: isLogoLoading } = useCompanyLogo(auth);

  const [fields, setFields] = useState<OrderFormField[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [draggedId, setDraggedId] = useState<string | null>(null);
  const [confirmingDeleteId, setConfirmingDeleteId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const [newLabel, setNewLabel] = useState("");
  const [newType, setNewType] = useState(FIELD_TYPES[0].value);
  const [newRequired, setNewRequired] = useState(false);
  const [isCreating, setIsCreating] = useState(false);

  useEffect(() => {
    if (!auth) return;

    async function load(token: string) {
      setIsLoading(true);
      try {
        const result = await getOrderFormFields(token);
        setFields(result);
      } catch (err) {
        setError(apiErrorMessage(err, Messages.orderFormFieldsLoadFailed));
      } finally {
        setIsLoading(false);
      }
    }

    load(auth.token);
  }, [auth]);

  async function handleAdd(e: React.FormEvent) {
    e.preventDefault();
    if (!auth || !newLabel.trim()) return;
    setIsCreating(true);
    setError(null);
    try {
      const created = await createOrderFormField(auth.token, newLabel.trim(), newType, newRequired);
      setFields((prev) => [...prev, created]);
      setNewLabel("");
      setNewType(FIELD_TYPES[0].value);
      setNewRequired(false);
    } catch (err) {
      setError(apiErrorMessage(err, Messages.orderFormFieldSaveFailed));
    } finally {
      setIsCreating(false);
    }
  }

  async function handleToggleRequired(field: OrderFormField) {
    if (!auth) return;
    const nextRequired = !field.isRequired;
    setFields((prev) => prev.map((f) => (f.id === field.id ? { ...f, isRequired: nextRequired } : f)));
    try {
      await updateOrderFormField(auth.token, field.id, field.label, field.fieldType, nextRequired);
    } catch (err) {
      setFields((prev) => prev.map((f) => (f.id === field.id ? { ...f, isRequired: field.isRequired } : f)));
      setError(apiErrorMessage(err, Messages.orderFormFieldSaveFailed));
    }
  }

  async function handleDelete(field: OrderFormField) {
    if (!auth) return;
    setIsDeleting(true);
    try {
      await deleteOrderFormField(auth.token, field.id);
      setFields((prev) => prev.filter((f) => f.id !== field.id));
      setConfirmingDeleteId(null);
    } catch (err) {
      setError(apiErrorMessage(err, Messages.orderFormFieldDeleteFailed));
    } finally {
      setIsDeleting(false);
    }
  }

  function handleDrop(targetId: string) {
    if (!auth || !draggedId || draggedId === targetId) {
      setDraggedId(null);
      return;
    }

    setFields((prev) => {
      const next = [...prev];
      const fromIndex = next.findIndex((f) => f.id === draggedId);
      const toIndex = next.findIndex((f) => f.id === targetId);
      if (fromIndex === -1 || toIndex === -1) return prev;

      const [moved] = next.splice(fromIndex, 1);
      next.splice(toIndex, 0, moved);

      reorderOrderFormFields(
        auth.token,
        next.map((f) => f.id)
      ).catch((err) => setError(apiErrorMessage(err, Messages.orderFormFieldReorderFailed)));

      return next;
    });
    setDraggedId(null);
  }

  if (authLoading || !auth || !auth.showOrders) {
    return null;
  }

  if (isLoading || isLogoLoading) {
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
        <span className="h-8 w-8 animate-spin rounded-full border-4 border-white/20 border-t-[#c9a86a]" />
      </div>
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
      <div className="mx-auto w-full max-w-2xl">
        <CompanyHeader auth={auth} logoUrl={logoUrl} onLogoChanged={setLogoUrl} onError={setError} logout={logout} />

        <p className="mb-4 text-sm text-zinc-200">{Labels.orderFormFieldsHint}</p>

        {error && (
          <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700 dark:bg-red-950 dark:text-red-300">
            {error}
          </p>
        )}

        {fields.length > 0 && (
          <ul className="mb-6 divide-y divide-zinc-200 rounded-xl border border-black/10 bg-[#ecdcc0] shadow-lg dark:divide-zinc-800 dark:border-white/10 dark:bg-zinc-900">
            {fields.map((field) => (
              <li
                key={field.id}
                onDragOver={(e) => e.preventDefault()}
                onDrop={() => handleDrop(field.id)}
                className={`flex items-center gap-3 px-4 py-3 ${draggedId === field.id ? "opacity-40" : ""}`}
              >
                <DragHandle onDragStart={() => setDraggedId(field.id)} onDragEnd={() => setDraggedId(null)} />
                <div className="flex flex-1 flex-col">
                  <span className="text-sm text-zinc-900 dark:text-zinc-50">{field.label}</span>
                  <span className="text-xs text-zinc-600 dark:text-zinc-400">
                    {Labels.fieldTypeLabel}: {fieldTypeLabel(field.fieldType)}
                  </span>
                </div>
                <label className="flex items-center gap-1.5 text-xs text-zinc-700 dark:text-zinc-300">
                  <input
                    type="checkbox"
                    checked={field.isRequired}
                    onChange={() => handleToggleRequired(field)}
                    style={{ accentColor: "#c9a86a" }}
                  />
                  {Labels.fieldRequiredLabel}
                </label>
                {confirmingDeleteId === field.id ? (
                  <DeleteConfirmActions
                    question={Labels.confirmDeleteOrderFieldQuestion}
                    isBusy={isDeleting}
                    onConfirm={() => handleDelete(field)}
                    onCancel={() => setConfirmingDeleteId(null)}
                  />
                ) : (
                  <button
                    onClick={() => setConfirmingDeleteId(field.id)}
                    className="text-xs text-red-600 underline hover:text-red-500 dark:text-red-400"
                  >
                    {Labels.delete}
                  </button>
                )}
              </li>
            ))}
          </ul>
        )}

        <form
          onSubmit={handleAdd}
          className="flex flex-wrap items-end gap-3 rounded-xl border border-black/10 bg-[#ecdcc0] px-4 py-3 shadow-lg dark:border-white/10 dark:bg-zinc-900"
        >
          <div className="flex flex-col gap-1">
            <span className="text-xs font-medium text-zinc-600 dark:text-zinc-400">{Labels.addOrderFormFieldTitle}</span>
            <input
              type="text"
              value={newLabel}
              onChange={(e) => setNewLabel(e.target.value)}
              placeholder={Labels.fieldLabelPlaceholder}
              className="w-48 rounded-md border border-zinc-300 px-3 py-1.5 text-sm text-zinc-900 outline-none focus:border-zinc-500 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
            />
          </div>
          <div className="flex flex-col gap-1">
            <span className="text-xs font-medium text-zinc-600 dark:text-zinc-400">{Labels.fieldTypeLabel}</span>
            <select
              value={newType}
              onChange={(e) => setNewType(e.target.value)}
              className="rounded-md border border-zinc-300 px-3 py-1.5 text-sm text-zinc-900 outline-none focus:border-zinc-500 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
            >
              {FIELD_TYPES.map((t) => (
                <option key={t.value} value={t.value}>
                  {t.label}
                </option>
              ))}
            </select>
          </div>
          <label className="flex items-center gap-1.5 pb-1.5 text-xs text-zinc-700 dark:text-zinc-300">
            <input
              type="checkbox"
              checked={newRequired}
              onChange={(e) => setNewRequired(e.target.checked)}
              style={{ accentColor: "#c9a86a" }}
            />
            {Labels.fieldRequiredLabel}
          </label>
          <button
            type="submit"
            disabled={isCreating || !newLabel.trim()}
            className="rounded-md bg-[#8a5a35] px-4 py-1.5 text-sm text-white transition-colors hover:bg-[#a06b41] disabled:opacity-50"
          >
            {isCreating ? Labels.addingField : Labels.addFieldButton}
          </button>
        </form>
      </div>
    </div>
  );
}
