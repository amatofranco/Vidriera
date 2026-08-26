"use client";

import { useState } from "react";
import { downloadOrderExcel, type Order } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";
import { Labels } from "@/lib/labels";
import { Messages, apiErrorMessage } from "@/lib/messages";

function formatDate(iso: string) {
  return new Date(iso).toLocaleString("es-AR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function DetailField({ label, value }: { label: string; value: string | null }) {
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-[11px] font-medium text-zinc-500 dark:text-zinc-400">{label}</span>
      <span className="text-sm text-zinc-900 dark:text-zinc-50">{value || Labels.notProvided}</span>
    </div>
  );
}

export function OrderRow({ order }: { order: Order }) {
  const { auth } = useAuth();
  const [expanded, setExpanded] = useState(false);
  const [isDownloading, setIsDownloading] = useState(false);
  const [downloadError, setDownloadError] = useState<string | null>(null);

  async function handleDownload() {
    if (!auth) return;
    setIsDownloading(true);
    setDownloadError(null);
    try {
      const { blob, fileName } = await downloadOrderExcel(auth.token, order.id);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = fileName;
      a.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      setDownloadError(apiErrorMessage(err, Messages.orderExcelDownloadFailed));
    } finally {
      setIsDownloading(false);
    }
  }

  return (
    <div className="rounded-xl border border-black/10 bg-[#ecdcc0] px-4 py-3 shadow-lg dark:border-white/10 dark:bg-zinc-900">
      <div className="flex w-full flex-wrap items-center justify-between gap-2">
        <button
          type="button"
          onClick={() => setExpanded((prev) => !prev)}
          className="flex flex-1 flex-wrap items-center justify-between gap-2 text-left"
        >
          <div className="flex flex-col">
            <span className="text-sm font-semibold text-zinc-900 dark:text-zinc-50">{order.businessName}</span>
            <span className="text-xs text-zinc-600 dark:text-zinc-400">
              {order.storeName ? `${order.storeName} · ` : ""}
              {formatDate(order.createdAt)}
            </span>
          </div>
          <div className="flex items-center gap-3">
            <span className="text-xs text-zinc-600 dark:text-zinc-400">
              {Labels.orderItemsCount(order.items.length)}
            </span>
            <span className="rounded-md border border-zinc-300 px-2 py-1 text-xs font-medium text-zinc-700 dark:border-zinc-700 dark:text-zinc-300">
              {expanded ? Labels.hideOrderDetail : Labels.viewOrderDetail}
            </span>
          </div>
        </button>
        <button
          type="button"
          onClick={handleDownload}
          disabled={isDownloading}
          className="rounded-md border border-zinc-300 px-2 py-1 text-xs font-medium text-zinc-700 hover:bg-zinc-50 disabled:opacity-50 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800"
        >
          {isDownloading ? Labels.downloadingOrderExcel : Labels.downloadOrderExcelButton}
        </button>
      </div>

      {downloadError && (
        <p className="mt-2 text-xs text-red-600 dark:text-red-400">{downloadError}</p>
      )}

      {expanded && (
        <div className="mt-4 flex flex-col gap-4 border-t border-black/10 pt-4 dark:border-white/10">
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
            <DetailField label={Labels.orderBusinessNameLabel} value={order.businessName} />
            <DetailField label={Labels.orderStoreNameLabel} value={order.storeName} />
            <DetailField label={Labels.orderCuitLabel} value={order.cuit} />
            <DetailField label={Labels.orderVatConditionLabel} value={order.vatCondition} />
            <DetailField label={Labels.orderPhoneLabel} value={order.phone} />
            <DetailField label={Labels.emailFieldLabel} value={order.email} />
            <DetailField label={Labels.orderCityLabel} value={order.city} />
            <DetailField label={Labels.orderProvinceLabel} value={order.province} />
            <DetailField label={Labels.orderCarrierLabel} value={order.carrier} />
            <DetailField label={Labels.orderDeliveryAddressLabel} value={order.deliveryAddress} />
          </div>

          <div className="overflow-x-auto rounded-md border border-zinc-300 dark:border-zinc-700">
            <table className="w-full text-left text-sm">
              <thead className="bg-black/5 dark:bg-white/5">
                <tr>
                  <th className="px-3 py-2 text-xs font-medium text-zinc-600 dark:text-zinc-400">
                    {Labels.orderItemColumn}
                  </th>
                  <th className="px-3 py-2 text-xs font-medium text-zinc-600 dark:text-zinc-400">
                    {Labels.orderCodeColumn}
                  </th>
                  <th className="px-3 py-2 text-right text-xs font-medium text-zinc-600 dark:text-zinc-400">
                    {Labels.orderQuantityColumn}
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-zinc-300 dark:divide-zinc-700">
                {order.items.map((item, index) => (
                  <tr key={index}>
                    <td className="px-3 py-2 text-zinc-900 dark:text-zinc-50">{item.itemName}</td>
                    <td className="px-3 py-2 text-zinc-600 dark:text-zinc-400">{item.code || Labels.notProvided}</td>
                    <td className="px-3 py-2 text-right text-zinc-900 dark:text-zinc-50">{item.quantity}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
