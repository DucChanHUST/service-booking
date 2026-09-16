"use client";

import React, { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { apiRoutes } from "@/lib/api-routes";
import type {
  CreateServiceRequest,
  Service,
  UpdateServiceRequest,
} from "@/types/service";

import AuthGuard from "@/components/auth/AuthGuard";

interface ServiceListResponse {
  items: Service[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

const emptyForm: CreateServiceRequest = {
  name: "",
  description: "",
  durationMinutes: 60,
  price: 0,
};

function AdminServicesPageContent() {
  const [services, setServices] = useState<Service[]>([]);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [form, setForm] = useState<CreateServiceRequest>(emptyForm);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function loadServices() {
    try {
      setLoading(true);
      setError("");

      const result = await api.get<ServiceListResponse>(
        apiRoutes.services.list({
          page: 1,
          pageSize: 50,
          search: search.trim() || undefined,
        }),
      );

      setServices(result.items);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load services.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadServices();
  }, []);

  function resetForm() {
    setForm(emptyForm);
    setEditingId(null);
  }

  function startEdit(service: Service) {
    setEditingId(service.id);

    setForm({
      name: service.name,
      description: service.description ?? "",
      durationMinutes: service.durationMinutes,
      price: service.price,
    });
  }

  async function handleSubmit(event: React.SubmitEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!form.name.trim()) {
      setError("Service name is required.");
      return;
    }

    if (form.durationMinutes <= 0) {
      setError("Duration must be greater than 0.");
      return;
    }

    if (form.price < 0) {
      setError("Price cannot be negative.");
      return;
    }

    try {
      setSaving(true);
      setError("");

      if (editingId) {
        const body: UpdateServiceRequest = {
          ...form,
          isActive:
            services.find((service) => service.id === editingId)?.isActive ??
            true,
        };

        await api.put(apiRoutes.services.update(editingId), body);
      } else {
        await api.post(apiRoutes.services.create, form);
      }

      resetForm();
      await loadServices();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save service.");
    } finally {
      setSaving(false);
    }
  }

  async function toggleActive(service: Service) {
    try {
      setError("");

      const body: UpdateServiceRequest = {
        name: service.name,
        description: service.description ?? "",
        durationMinutes: service.durationMinutes,
        price: service.price,
        isActive: !service.isActive,
      };

      await api.put(apiRoutes.services.update(service.id), body);

      await loadServices();
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to update service.",
      );
    }
  }

  return (
    <main className="mx-auto max-w-7xl px-4 py-10 sm:px-6 lg:px-8">
      <header className="mb-8">
        <p className="text-sm font-bold uppercase tracking-[0.16em] text-(--brand)">
          Admin workspace
        </p>
        <h1 className="mt-3 text-4xl font-bold tracking-tight text-foreground">
          Service management
        </h1>
        <p className="mt-3 text-base leading-7 text-(--muted)">
          Create, update, and control the services customers can book.
        </p>
      </header>

      <div className="grid items-start gap-6 xl:grid-cols-[minmax(280px,0.75fr)_minmax(0,1.5fr)]">
        <section className="rounded-2xl border border-(--border) bg-white p-6 sm:p-8">
          <div className="mb-6">
            <p className="text-xs font-bold uppercase tracking-[0.16em] text-(--muted)">
              Service details
            </p>
            <h2 className="mt-2 text-2xl font-bold tracking-tight text-foreground">
              {editingId ? "Edit service" : "Create a service"}
            </h2>
          </div>

          <form onSubmit={handleSubmit} className="space-y-5">
            <div>
              <label
                htmlFor="service-name"
                className="mb-2 block text-sm font-semibold text-foreground"
              >
                Name
              </label>
              <input
                id="service-name"
                type="text"
                placeholder="e.g. Consultation"
                value={form.name}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    name: event.target.value,
                  }))
                }
                className="w-full rounded-xl border border-(--border) px-4 py-3 text-sm outline-none transition focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed]"
              />
            </div>

            <div>
              <label
                htmlFor="service-description"
                className="mb-2 block text-sm font-semibold text-foreground"
              >
                Description
              </label>
              <textarea
                id="service-description"
                rows={4}
                placeholder="What does this service include?"
                value={form.description ?? ""}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    description: event.target.value,
                  }))
                }
                className="w-full resize-y rounded-xl border border-(--border) px-4 py-3 text-sm outline-none transition focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed]"
              />
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <div>
                <label
                  htmlFor="service-duration"
                  className="mb-2 block text-sm font-semibold text-foreground"
                >
                  Duration
                </label>
                <input
                  id="service-duration"
                  type="number"
                  min={1}
                  placeholder="Minutes"
                  value={form.durationMinutes}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      durationMinutes: Number(event.target.value),
                    }))
                  }
                  className="w-full rounded-xl border border-(--border) px-4 py-3 text-sm outline-none transition focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed]"
                />
              </div>
              <div>
                <label
                  htmlFor="service-price"
                  className="mb-2 block text-sm font-semibold text-foreground"
                >
                  Price
                </label>
                <input
                  id="service-price"
                  type="number"
                  min={0}
                  step="0.01"
                  placeholder="0.00"
                  value={form.price}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      price: Number(event.target.value),
                    }))
                  }
                  className="w-full rounded-xl border border-(--border) px-4 py-3 text-sm outline-none transition focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed]"
                />
              </div>
            </div>

            <div className="flex flex-wrap gap-3 pt-2">
              <button
                type="submit"
                disabled={saving}
                className="rounded-xl bg-(--brand) px-4 py-3 text-sm font-bold text-white transition hover:bg-(--brand-dark) disabled:cursor-not-allowed disabled:opacity-50"
              >
                {saving
                  ? "Saving..."
                  : editingId
                    ? "Update service"
                    : "Create service"}
              </button>
              {editingId && (
                <button
                  type="button"
                  onClick={resetForm}
                  className="rounded-xl border border-(--border) px-4 py-3 text-sm font-semibold text-foreground transition hover:bg-gray-50"
                >
                  Cancel
                </button>
              )}
            </div>
          </form>
        </section>

        <section className="min-w-0 rounded-2xl border border-(--border) bg-white p-6 sm:p-8">
          <div className="flex flex-col justify-between gap-4 border-b border-(--border) pb-6 sm:flex-row sm:items-end">
            <div>
              <p className="text-xs font-bold uppercase tracking-[0.16em] text-(--muted)">
                Catalog
              </p>
              <h2 className="mt-2 text-2xl font-bold tracking-tight text-foreground">
                Services
              </h2>
            </div>
            <form
              onSubmit={(event) => {
                event.preventDefault();
                loadServices();
              }}
              className="flex w-full gap-2 sm:w-auto"
            >
              <input
                type="text"
                placeholder="Search services"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                className="min-w-0 flex-1 rounded-xl border border-(--border) px-4 py-2.5 text-sm outline-none focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed] sm:w-52"
              />
              <button
                type="submit"
                className="rounded-xl bg-gray-100 px-4 py-2.5 text-sm font-semibold text-foreground transition hover:bg-gray-200"
              >
                Search
              </button>
            </form>
          </div>

          {error && (
            <div
              className="mt-6 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700"
              role="alert"
            >
              {error}
            </div>
          )}

          {loading ? (
            <div className="mt-6 space-y-3">
              <div className="h-14 animate-pulse rounded-xl bg-gray-100" />
              <div className="h-14 animate-pulse rounded-xl bg-gray-100" />
              <div className="h-14 animate-pulse rounded-xl bg-gray-100" />
            </div>
          ) : services.length === 0 ? (
            <div className="mt-6 rounded-xl border border-dashed border-gray-300 px-6 py-12 text-center">
              <p className="font-semibold text-foreground">No services found</p>
              <p className="mt-1 text-sm text-(--muted)">
                Try a different search or create your first service.
              </p>
            </div>
          ) : (
            <div className="mt-6 overflow-x-auto">
              <table className="w-full min-w-170 text-left text-sm">
                <thead className="border-b border-(--border) text-xs uppercase tracking-wider text-gray-400">
                  <tr>
                    <th className="pb-3 pr-4 font-semibold">Name</th>
                    <th className="pb-3 pr-4 font-semibold">Duration</th>
                    <th className="pb-3 pr-4 font-semibold">Price</th>
                    <th className="pb-3 pr-4 font-semibold">Status</th>
                    <th className="pb-3 font-semibold">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {services.map((service) => (
                    <tr
                      key={service.id}
                      className="align-middle hover:bg-gray-50/70"
                    >
                      <td className="py-4 pr-4">
                        <p className="font-semibold text-foreground">
                          {service.name}
                        </p>
                        <p className="mt-1 max-w-xs truncate text-xs text-(--muted)">
                          {service.description || "No description"}
                        </p>
                      </td>
                      <td className="py-4 pr-4 text-(--muted)">
                        {service.durationMinutes} min
                      </td>
                      <td className="py-4 pr-4 font-semibold text-foreground">
                        {service.price.toLocaleString("vi-VN")}₫
                      </td>
                      <td className="py-4 pr-4">
                        <span
                          className={`rounded-full px-2.5 py-1 text-xs font-bold ${service.isActive ? "bg-[#e5f4ef] text-(--brand-dark)" : "bg-gray-100 text-gray-500"}`}
                        >
                          {service.isActive ? "Active" : "Inactive"}
                        </span>
                      </td>
                      <td className="py-4">
                        <div className="flex gap-2">
                          <button
                            type="button"
                            onClick={() => startEdit(service)}
                            className="rounded-lg border border-(--border) px-3 py-2 text-xs font-semibold text-foreground hover:bg-gray-50"
                          >
                            Edit
                          </button>
                          <button
                            type="button"
                            onClick={() => toggleActive(service)}
                            className="rounded-lg px-3 py-2 text-xs font-semibold text-(--brand-dark) hover:bg-[#edf7f4]"
                          >
                            {service.isActive ? "Deactivate" : "Activate"}
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </div>
    </main>
  );
}

export default function AdminServicesPage() {
  return (
    <AuthGuard requiredRole="Admin">
      <AdminServicesPageContent />
    </AuthGuard>
  );
}
