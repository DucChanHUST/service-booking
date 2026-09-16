"use client";

import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import type { Booking, BookingStatus } from "@/types/booking";

import AuthGuard from "@/components/auth/AuthGuard";

const statuses: BookingStatus[] = [
  "Pending",
  "Confirmed",
  "Completed",
  "Cancelled",
];

function formatDateTime(value: string) {
  return new Date(value).toLocaleString("vi-VN", {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

function getStatusLabel(status: BookingStatus) {
  switch (status) {
    case "Pending":
      return "Pending";

    case "Confirmed":
      return "Confirmed";

    case "Completed":
      return "Completed";

    case "Cancelled":
      return "Cancelled";

    default:
      return status;
  }
}

function getStatusClass(status: BookingStatus) {
  switch (status) {
    case "Confirmed":
      return "bg-[#e5f4ef] text-(--brand-dark)";
    case "Completed":
      return "bg-blue-50 text-blue-700";
    case "Cancelled":
      return "bg-gray-100 text-gray-500";
    default:
      return "bg-amber-50 text-amber-700";
  }
}

function AdminBookingPageContent() {
  const [bookings, setBookings] = useState<Booking[]>([]);

  const [date, setDate] = useState("");
  const [status, setStatus] = useState<BookingStatus | "">("");

  const [loading, setLoading] = useState(true);
  const [updatingId, setUpdatingId] = useState<string | null>(null);

  const [error, setError] = useState("");

  async function loadBookings() {
    try {
      setLoading(true);
      setError("");

      const query = new URLSearchParams({
        page: "1",
        pageSize: "100",
      });

      if (date) {
        query.set("date", date);
      }

      if (status) {
        query.set("status", status);
      }

      const result = await api.get<Booking[]>(`/bookings?${query.toString()}`);

      setBookings(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load bookings.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadBookings();
  }, []);

  async function handleStatusChange(
    booking: Booking,
    newStatus: BookingStatus,
  ) {
    if (booking.status === newStatus) {
      return;
    }

    try {
      setUpdatingId(booking.id);
      setError("");

      await api.patch(`/bookings/${booking.id}/status`, {
        status: newStatus,
      });

      await loadBookings();
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to update booking status.",
      );
    } finally {
      setUpdatingId(null);
    }
  }

  return (
    <main className="mx-auto max-w-350 px-4 py-10 sm:px-6 lg:px-8">
      <header className="mb-8">
        <p className="text-sm font-bold uppercase tracking-[0.16em] text-(--brand)">
          Admin workspace
        </p>
        <h1 className="mt-3 text-4xl font-bold tracking-tight text-foreground">
          Booking management
        </h1>
        <p className="mt-3 text-base leading-7 text-(--muted)">
          Review appointments, follow up with customers, and keep statuses
          current.
        </p>
      </header>

      <section className="rounded-2xl border border-(--border) bg-white p-5 sm:p-6">
        <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
          <div>
            <p className="text-xs font-bold uppercase tracking-[0.16em] text-(--muted)">
              Filters
            </p>
            <h2 className="mt-2 text-xl font-bold text-foreground">
              Find bookings
            </h2>
          </div>
          <p className="text-sm text-(--muted)">
            {bookings.length} result{bookings.length === 1 ? "" : "s"}
          </p>
        </div>
        <div className="mt-5 flex flex-col gap-4 sm:flex-row sm:items-end">
          <label className="flex-1">
            <span className="mb-2 block text-sm font-semibold text-foreground">
              Date
            </span>
            <input
              type="date"
              value={date}
              onChange={(event) => setDate(event.target.value)}
              className="w-full rounded-xl border border-(--border) px-4 py-3 text-sm outline-none focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed]"
            />
          </label>
          <label className="flex-1">
            <span className="mb-2 block text-sm font-semibold text-foreground">
              Status
            </span>
            <select
              value={status}
              onChange={(event) =>
                setStatus(event.target.value as BookingStatus | "")
              }
              className="w-full rounded-xl border border-(--border) bg-white px-4 py-3 text-sm outline-none focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed]"
            >
              <option value="">All statuses</option>
              {statuses.map((item) => (
                <option key={item} value={item}>
                  {getStatusLabel(item)}
                </option>
              ))}
            </select>
          </label>
          <div className="flex gap-3">
            <button
              type="button"
              onClick={loadBookings}
              className="rounded-xl bg-(--brand) px-4 py-3 text-sm font-bold text-white hover:bg-(--brand-dark)"
            >
              Apply filters
            </button>
            <button
              type="button"
              onClick={() => {
                setDate("");
                setStatus("");
              }}
              className="rounded-xl border border-(--border) px-4 py-3 text-sm font-semibold text-foreground hover:bg-gray-50"
            >
              Clear
            </button>
          </div>
        </div>
      </section>

      {error && (
        <div
          className="mt-6 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700"
          role="alert"
        >
          {error}
        </div>
      )}

      <section className="mt-6 rounded-2xl border border-(--border) bg-white p-5 sm:p-6">
        {loading ? (
          <div className="space-y-3">
            <div className="h-14 animate-pulse rounded-xl bg-gray-100" />
            <div className="h-14 animate-pulse rounded-xl bg-gray-100" />
            <div className="h-14 animate-pulse rounded-xl bg-gray-100" />
          </div>
        ) : bookings.length === 0 ? (
          <div className="rounded-xl border border-dashed border-gray-300 px-6 py-14 text-center">
            <p className="font-semibold text-foreground">No bookings found</p>
            <p className="mt-1 text-sm text-(--muted)">
              Try adjusting the date or status filters.
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-280 text-left text-sm">
              <thead className="border-b border-(--border) text-xs uppercase tracking-wider text-gray-400">
                <tr>
                  <th className="pb-3 pr-5 font-semibold">Booking</th>
                  <th className="pb-3 pr-5 font-semibold">Customer</th>
                  <th className="pb-3 pr-5 font-semibold">Service</th>
                  <th className="pb-3 pr-5 font-semibold">Staff</th>
                  <th className="pb-3 pr-5 font-semibold">Appointment</th>
                  <th className="pb-3 font-semibold">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {bookings.map((booking) => (
                  <tr
                    key={booking.id}
                    className="align-top hover:bg-gray-50/70"
                  >
                    <td className="py-4 pr-5">
                      <p className="font-bold text-foreground">
                        {booking.bookingCode}
                      </p>
                      <p className="mt-1 text-xs text-(--muted)">
                        Created {formatDateTime(booking.createdAt)}
                      </p>
                    </td>
                    <td className="py-4 pr-5">
                      <p className="font-medium text-foreground">
                        {booking.customerEmail}
                      </p>
                      {booking.customerNote && (
                        <p className="mt-1 max-w-xs text-xs text-(--muted)">
                          Note: {booking.customerNote}
                        </p>
                      )}
                    </td>
                    <td className="py-4 pr-5 font-medium text-foreground">
                      {booking.serviceName}
                    </td>
                    <td className="py-4 pr-5 text-(--muted)">
                      {booking.staffName}
                    </td>
                    <td className="py-4 pr-5">
                      <p className="font-medium text-foreground">
                        {formatDateTime(booking.startTime)}
                      </p>
                      <p className="mt-1 text-xs text-(--muted)">
                        Until {formatDateTime(booking.endTime)}
                      </p>
                    </td>
                    <td className="py-4">
                      <select
                        value={booking.status}
                        disabled={updatingId === booking.id}
                        onChange={(event) =>
                          handleStatusChange(
                            booking,
                            event.target.value as BookingStatus,
                          )
                        }
                        className={`rounded-lg border-0 px-3 py-2 text-xs font-bold outline-none ring-1 ring-inset ring-transparent focus:ring-(--brand) disabled:opacity-50 ${getStatusClass(booking.status)}`}
                      >
                        {statuses.map((item) => (
                          <option key={item} value={item}>
                            {getStatusLabel(item)}
                          </option>
                        ))}
                      </select>
                      {updatingId === booking.id && (
                        <p className="mt-2 text-xs text-(--muted)">
                          Updating...
                        </p>
                      )}
                      {booking.cancellationReason && (
                        <p className="mt-2 max-w-xs text-xs text-red-600">
                          Reason: {booking.cancellationReason}
                        </p>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </main>
  );
}

export default function AdminBookingsPage() {
  return (
    <AuthGuard requiredRole="Admin">
      <AdminBookingPageContent />
    </AuthGuard>
  );
}
