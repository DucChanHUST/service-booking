"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { apiRoutes } from "@/lib/api-routes";
import type { Service } from "@/types/service";

import AuthGuard from "@/components/auth/AuthGuard";

interface ServiceListResponse {
  items: Service[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

function ServicesPageContent() {
  const [services, setServices] = useState<Service[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    loadServices();
  }, []);

  async function loadServices() {
    try {
      setLoading(true);
      setError("");

      const data = await api.get<ServiceListResponse>(
        apiRoutes.services.list({ page: 1, pageSize: 20 }),
      );

      setServices(data.items.filter((service) => service.isActive));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load services.");
    } finally {
      setLoading(false);
    }
  }

  if (loading) {
    return (
      <main className="mx-auto max-w-7xl px-4 py-10 sm:px-6 lg:px-8">
        <div className="mb-10 max-w-2xl animate-pulse">
          <div className="h-3 w-28 rounded bg-gray-200" />
          <div className="mt-4 h-12 w-full max-w-lg rounded bg-gray-200" />
          <div className="mt-4 h-5 w-full max-w-xl rounded bg-gray-200" />
        </div>
        <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {[1, 2, 3].map((item) => (
            <div
              key={item}
              className="h-64 animate-pulse rounded-2xl border border-(--border) bg-white p-6"
            >
              <div className="h-5 w-2/3 rounded bg-gray-200" />
              <div className="mt-4 h-12 rounded bg-gray-100" />
              <div className="mt-10 h-10 rounded bg-gray-200" />
            </div>
          ))}
        </div>
      </main>
    );
  }

  if (error) {
    return (
      <main className="mx-auto max-w-7xl px-4 py-10 sm:px-6 lg:px-8">
        <div className="rounded-2xl border border-red-200 bg-red-50 p-6 text-red-800">
          <p className="text-sm font-semibold">
            We couldn&apos;t load the services
          </p>
          <p className="mt-1 text-sm text-red-700">{error}</p>
          <button
            type="button"
            onClick={loadServices}
            className="mt-5 rounded-lg bg-red-700 px-4 py-2 text-sm font-semibold text-white hover:bg-red-800"
          >
            Try again
          </button>
        </div>
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-7xl px-4 py-10 sm:px-6 sm:py-14 lg:px-8">
      <header className="mb-10 flex flex-col justify-between gap-6 border-b border-(--border) pb-8 sm:flex-row sm:items-end">
        <div className="max-w-2xl">
          <p className="text-sm font-bold uppercase tracking-[0.16em] text-(--brand)">
            Your time, well spent
          </p>
          <h1 className="mt-3 text-4xl font-bold tracking-tight text-foreground sm:text-5xl">
            Find the right service.
          </h1>
          <p className="mt-4 max-w-xl text-base leading-7 text-(--muted)">
            Browse our available services and choose a time that works for you.
          </p>
        </div>
        <div className="w-fit rounded-full border border-(--border) bg-white px-4 py-2 text-sm font-semibold text-(--muted)">
          {services.length} {services.length === 1 ? "service" : "services"}{" "}
          available
        </div>
      </header>

      {services.length === 0 ? (
        <div className="rounded-2xl border border-dashed border-gray-300 bg-white px-6 py-16 text-center">
          <p className="text-lg font-semibold text-foreground">
            No services available right now
          </p>
          <p className="mt-2 text-sm text-(--muted)">
            Please check back soon for new availability.
          </p>
        </div>
      ) : (
        <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {services.map((service) => (
            <article
              key={service.id}
              className="group flex min-h-72 flex-col overflow-hidden rounded-2xl border border-(--border) bg-white transition duration-200 hover:-translate-y-0.5 hover:border-[#b7d8d0] hover:shadow-[0_12px_32px_rgba(23,107,91,0.08)]"
            >
              <div className="h-1.5 bg-(--brand)" />
              <div className="flex flex-1 flex-col p-6">
                <div className="flex items-start justify-between gap-4">
                  <h2 className="text-xl font-bold tracking-tight text-foreground">
                    {service.name}
                  </h2>
                  <span className="shrink-0 rounded-full bg-[#edf7f4] px-2.5 py-1 text-xs font-bold text-(--brand-dark)">
                    {service.durationMinutes} min
                  </span>
                </div>
                <p className="mt-4 min-h-14 text-sm leading-6 text-(--muted)">
                  {service.description ||
                    "A focused session tailored to your needs."}
                </p>
                <div className="mt-auto flex items-end justify-between gap-4 pt-8">
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-wider text-gray-400">
                      Starting at
                    </p>
                    <p className="mt-1 text-2xl font-bold tracking-tight text-foreground">
                      {service.price.toLocaleString("vi-VN")}₫
                    </p>
                  </div>
                  <Link
                    href={`/booking?serviceId=${service.id}`}
                    className="rounded-lg bg-(--brand) px-4 py-2.5 text-sm font-bold text-white transition hover:bg-(--brand-dark)"
                  >
                    Book now
                  </Link>
                </div>
              </div>
            </article>
          ))}
        </div>
      )}
    </main>
  );
}

export default function ServicesPage() {
  return (
    <AuthGuard>
      <ServicesPageContent />
    </AuthGuard>
  );
}
