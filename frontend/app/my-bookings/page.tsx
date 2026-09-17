"use client";

import { useEffect, useState, useCallback } from "react";
import { api } from "@/lib/api";
import { apiRoutes } from "@/lib/api-routes";
import type { Booking, BookingStatus } from "@/types/booking";
import { useBookingRealtime } from "@/lib/useBookingRealtime";

import AuthGuard from "@/components/auth/AuthGuard";

interface MyBookingsResponse {
  items: Booking[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

type BookingView = "list" | "week";

const statusFilters: Array<{ label: string; value: BookingStatus | "" }> = [
  { label: "All", value: "" },
  { label: "Pending", value: "Pending" },
  { label: "Confirmed", value: "Confirmed" },
  { label: "Completed", value: "Completed" },
  { label: "Cancelled", value: "Cancelled" },
];

function formatDateTime(value: string) {
  const date = new Date(value);

  return date.toLocaleString("vi-VN", {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

function formatDateKey(value: Date) {
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, "0");
  const day = String(value.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
}

function getWeekDays(value: Date) {
  const date = new Date(value);
  const dayOfWeek = date.getDay();
  const mondayOffset = dayOfWeek === 0 ? -6 : 1 - dayOfWeek;

  date.setDate(date.getDate() + mondayOffset);

  return Array.from({ length: 7 }, (_, index) => {
    const weekDay = new Date(date);
    weekDay.setDate(date.getDate() + index);

    return {
      date: formatDateKey(weekDay),
      label: weekDay.toLocaleDateString("en-US", { weekday: "short" }),
      day: weekDay.getDate(),
      month: weekDay.toLocaleDateString("en-US", { month: "short" }),
    };
  });
}

function getWeekRange(value: Date) {
  const days = getWeekDays(value);

  return {
    from: days[0].date,
    to: days[6].date,
  };
}

function formatTime(value: string) {
  return new Date(value).toLocaleTimeString("vi-VN", {
    hour: "2-digit",
    minute: "2-digit",
  });
}

function getStatusClass(status: BookingStatus) {
  switch (status) {
    case "Pending":
      return "bg-yellow-100 text-yellow-800";

    case "Confirmed":
      return "bg-blue-100 text-blue-800";

    case "Completed":
      return "bg-green-100 text-green-800";

    case "Cancelled":
      return "bg-gray-100 text-gray-600";

    default:
      return "bg-gray-100 text-gray-600";
  }
}

function MyBookingsPageContent() {
  const [bookings, setBookings] = useState<Booking[]>([]);

  const [status, setStatus] = useState<BookingStatus | "">("");

  const [view, setView] = useState<BookingView>("list");

  const [calendarDate, setCalendarDate] = useState(() => new Date());

  const [page, setPage] = useState(1);

  const [totalPages, setTotalPages] = useState(1);

  const [totalCount, setTotalCount] = useState(0);

  const [loading, setLoading] = useState(true);

  const [error, setError] = useState("");

  const [cancellingId, setCancellingId] = useState<string | null>(null);

  const [cancellationReason, setCancellationReason] = useState("");

  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    loadBookings();
  }, [page, status, view, calendarDate]);

  async function loadBookings() {
    try {
      setLoading(true);
      setError("");

      const weekRange = getWeekRange(calendarDate);

      const data = await api.get<MyBookingsResponse>(
        apiRoutes.bookings.mine({
          page,
          pageSize: view === "week" ? 100 : 10,
          status: status || undefined,
          from: view === "week" ? weekRange.from : undefined,
          to: view === "week" ? weekRange.to : undefined,
        }),
      );

      setBookings(data.items);
      setTotalPages(data.totalPages);
      setTotalCount(data.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load bookings.");
    } finally {
      setLoading(false);
    }
  }

  function handleStatusChange(value: BookingStatus | "") {
    setStatus(value);
    setPage(1);
    setCancellingId(null);
  }

  function goToPage(nextPage: number) {
    setPage(Math.min(Math.max(nextPage, 1), totalPages));
    setCancellingId(null);
  }

  function moveCalendarWeek(offset: number) {
    setCalendarDate((current) => {
      const next = new Date(current);
      next.setDate(next.getDate() + offset * 7);

      return next;
    });
  }

  function changeView(nextView: BookingView) {
    setView(nextView);
    setPage(1);
    setCancellingId(null);
  }

  function startCancel(bookingId: string) {
    setCancellingId(bookingId);
    setCancellationReason("");
    setError("");
  }

  function closeCancel() {
    setCancellingId(null);
    setCancellationReason("");
  }

  async function handleCancel(bookingId: string) {
    if (!cancellationReason.trim()) {
      setError("Cancellation reason is required.");

      return;
    }

    try {
      setSubmitting(true);
      setError("");

      await api.patch(apiRoutes.bookings.cancel(bookingId), {
        cancellationReason: cancellationReason.trim(),
      });

      closeCancel();

      await loadBookings();
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to cancel booking.",
      );
    } finally {
      setSubmitting(false);
    }
  }

  const handleBookingCreated = useCallback((booking: Booking) => {
    setBookings((current) => {
      const exists = current.some((item) => item.id === booking.id);

      if (exists) {
        return current;
      }

      return [booking, ...current];
    });
  }, []);

  const handleBookingStatusUpdated = useCallback((booking: Booking) => {
    setBookings((current) =>
      current.map((item) => (item.id === booking.id ? booking : item)),
    );
  }, []);

  const handleBookingCancelled = useCallback((booking: Booking) => {
    setBookings((current) =>
      current.map((item) => (item.id === booking.id ? booking : item)),
    );
  }, []);

  useBookingRealtime({
    onCreated: handleBookingCreated,
    onCancelled: handleBookingCancelled,
    onStatusUpdated: handleBookingStatusUpdated,
  });

  const weekDays = getWeekDays(calendarDate);
  const weekBookings = bookings.filter((booking) => {
    const bookingDate = formatDateKey(new Date(booking.startTime));

    return weekDays.some((weekDay) => weekDay.date === bookingDate);
  });

  function getBookingsForDay(date: string) {
    return weekBookings
      .filter((booking) => formatDateKey(new Date(booking.startTime)) === date)
      .sort((first, second) => first.startTime.localeCompare(second.startTime));
  }

  if (loading) {
    return <div className="mx-auto max-w-6xl p-6">Loading bookings...</div>;
  }

  return (
    <div className="mx-auto max-w-6xl p-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold">My Bookings</h1>

        <p className="mt-2 text-gray-600">View and manage your bookings.</p>
      </div>

      <div className="mb-6 overflow-x-auto border-b border-gray-200">
        <div className="flex min-w-max items-center gap-6">
          {statusFilters.map((filter) => (
            <button
              key={filter.value || "all"}
              type="button"
              onClick={() => handleStatusChange(filter.value)}
              className={`border-b-2 px-1 pb-3 text-sm font-semibold transition ${
                status === filter.value
                  ? "border-(--brand) text-(--brand)"
                  : "border-transparent text-gray-500 hover:text-gray-800"
              }`}
            >
              {filter.label}
            </button>
          ))}
        </div>
      </div>

      <div className="mb-6 flex w-fit rounded-lg border border-gray-200 bg-white p-1">
        <button
          type="button"
          onClick={() => changeView("list")}
          aria-pressed={view === "list"}
          className={`rounded-md px-4 py-2 text-sm font-semibold transition ${
            view === "list"
              ? "bg-(--brand) text-white"
              : "text-gray-500 hover:bg-gray-50 hover:text-gray-800"
          }`}
        >
          List view
        </button>
        <button
          type="button"
          onClick={() => changeView("week")}
          aria-pressed={view === "week"}
          className={`rounded-md px-4 py-2 text-sm font-semibold transition ${
            view === "week"
              ? "bg-(--brand) text-white"
              : "text-gray-500 hover:bg-gray-50 hover:text-gray-800"
          }`}
        >
          Week view
        </button>
      </div>

      {!loading && (
        <div className="mb-4 flex items-center justify-between text-sm text-gray-500">
          <span>
            {totalCount} booking{totalCount === 1 ? "" : "s"}
          </span>
          {totalPages > 1 && (
            <span>
              Page {page} of {totalPages}
            </span>
          )}
        </div>
      )}

      {error && (
        <div className="mb-6 rounded border border-red-200 bg-red-50 p-4 text-red-700">
          {error}
        </div>
      )}

      {view === "week" ? (
        <section className="overflow-x-auto rounded-lg border bg-white">
          <div className="flex min-w-245 flex-col justify-between gap-3 border-b p-4 sm:flex-row sm:items-center">
            <div>
              <h2 className="font-semibold">Weekly calendar</h2>
              <p className="mt-1 text-sm text-gray-500">
                {weekDays[0].date} to {weekDays[6].date}
              </p>
            </div>
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={() => moveCalendarWeek(-1)}
                className="rounded border px-3 py-2 text-sm font-medium hover:bg-gray-50"
                aria-label="Previous week"
              >
                Previous
              </button>
              <button
                type="button"
                onClick={() => setCalendarDate(new Date())}
                className="rounded border px-3 py-2 text-sm font-medium hover:bg-gray-50"
              >
                Today
              </button>
              <button
                type="button"
                onClick={() => moveCalendarWeek(1)}
                className="rounded border px-3 py-2 text-sm font-medium hover:bg-gray-50"
                aria-label="Next week"
              >
                Next
              </button>
            </div>
          </div>

          <div className="grid min-w-245 grid-cols-7 divide-x divide-gray-200">
            {weekDays.map((weekDay) => {
              const dayBookings = getBookingsForDay(weekDay.date);

              return (
                <div key={weekDay.date} className="min-h-64 bg-gray-50/40">
                  <div className="border-b border-gray-200 px-3 py-3 text-center">
                    <p className="text-xs font-semibold uppercase text-gray-500">
                      {weekDay.label}
                    </p>
                    <p className="mt-1 text-lg font-bold text-gray-900">
                      {weekDay.day}
                    </p>
                    <p className="text-xs text-gray-400">{weekDay.month}</p>
                  </div>
                  <div className="space-y-2 p-2">
                    {dayBookings.map((booking) => (
                      <article
                        key={booking.id}
                        className="rounded border-l-4 border-(--brand) bg-white p-2 shadow-sm"
                      >
                        <p className="text-xs font-bold text-(--brand-dark)">
                          {formatTime(booking.startTime)} -{" "}
                          {formatTime(booking.endTime)}
                        </p>
                        <p className="mt-1 text-sm font-semibold leading-5 text-gray-900">
                          {booking.serviceName}
                        </p>
                        <p className="mt-1 text-xs text-gray-500">
                          {booking.staffName}
                        </p>
                        <span
                          className={`mt-2 inline-block rounded-full px-2 py-1 text-[10px] font-medium ${getStatusClass(
                            booking.status,
                          )}`}
                        >
                          {booking.status}
                        </span>
                      </article>
                    ))}
                    {dayBookings.length === 0 && (
                      <p className="py-5 text-center text-xs text-gray-400">
                        No bookings
                      </p>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </section>
      ) : bookings.length === 0 ? (
        <div className="rounded-lg border bg-white p-10 text-center">
          <p className="text-gray-500">You don&apos;t have any bookings yet.</p>
        </div>
      ) : (
        <div className="space-y-4">
          {bookings.map((booking) => (
            <div
              key={booking.id}
              className="rounded-lg border bg-white p-6 shadow-sm"
            >
              <div className="flex flex-col justify-between gap-4 md:flex-row">
                <div>
                  <div className="flex items-center gap-3">
                    <h2 className="text-lg font-semibold">
                      {booking.serviceName}
                    </h2>

                    <span
                      className={`rounded-full px-3 py-1 text-xs font-medium ${getStatusClass(
                        booking.status,
                      )}`}
                    >
                      {booking.status}
                    </span>
                  </div>

                  <p className="mt-2 text-sm text-gray-500">
                    Booking code:{" "}
                    <span className="font-medium text-gray-700">
                      {booking.bookingCode}
                    </span>
                  </p>
                </div>

                <div className="text-sm text-gray-500">
                  Created {formatDateTime(booking.createdAt)}
                </div>
              </div>

              <div className="mt-6 grid gap-4 border-t pt-5 md:grid-cols-3">
                <div>
                  <p className="text-xs uppercase text-gray-500">Staff</p>

                  <p className="mt-1 font-medium">{booking.staffName}</p>
                </div>

                <div>
                  <p className="text-xs uppercase text-gray-500">Start</p>

                  <p className="mt-1 font-medium">
                    {formatDateTime(booking.startTime)}
                  </p>
                </div>

                <div>
                  <p className="text-xs uppercase text-gray-500">End</p>

                  <p className="mt-1 font-medium">
                    {formatDateTime(booking.endTime)}
                  </p>
                </div>
              </div>

              {booking.customerNote && (
                <div className="mt-5 rounded bg-gray-50 p-4">
                  <p className="text-xs font-medium uppercase text-gray-500">
                    Your note
                  </p>

                  <p className="mt-1 text-sm">{booking.customerNote}</p>
                </div>
              )}

              {booking.status === "Cancelled" && booking.cancellationReason && (
                <div className="mt-5 rounded bg-gray-50 p-4">
                  <p className="text-xs font-medium uppercase text-gray-500">
                    Cancellation reason
                  </p>

                  <p className="mt-1 text-sm">{booking.cancellationReason}</p>
                </div>
              )}

              {booking.status !== "Completed" &&
                booking.status !== "Cancelled" &&
                new Date(booking.startTime) > new Date() && (
                  <div className="mt-5">
                    {cancellingId === booking.id ? (
                      <div className="rounded border bg-gray-50 p-4">
                        <label
                          htmlFor={`reason-${booking.id}`}
                          className="mb-2 block text-sm font-medium"
                        >
                          Cancellation reason
                        </label>

                        <textarea
                          id={`reason-${booking.id}`}
                          value={cancellationReason}
                          onChange={(event) =>
                            setCancellationReason(event.target.value)
                          }
                          maxLength={500}
                          rows={3}
                          placeholder="Why do you want to cancel this booking?"
                          className="w-full rounded border bg-white px-3 py-2"
                        />

                        <div className="mt-3 flex gap-3">
                          <button
                            type="button"
                            onClick={() => handleCancel(booking.id)}
                            disabled={submitting}
                            className="rounded bg-red-600 px-4 py-2 text-sm text-white disabled:opacity-50"
                          >
                            {submitting
                              ? "Cancelling..."
                              : "Confirm cancellation"}
                          </button>

                          <button
                            type="button"
                            onClick={closeCancel}
                            disabled={submitting}
                            className="rounded border px-4 py-2 text-sm"
                          >
                            Keep booking
                          </button>
                        </div>
                      </div>
                    ) : (
                      <button
                        type="button"
                        onClick={() => startCancel(booking.id)}
                        className="rounded border border-red-300 px-4 py-2 text-sm text-red-600 hover:bg-red-50"
                      >
                        Cancel booking
                      </button>
                    )}
                  </div>
                )}
            </div>
          ))}
        </div>
      )}

      {totalPages > 1 && (
        <div className="mt-6 flex items-center justify-between">
          <button
            type="button"
            onClick={() => goToPage(page - 1)}
            disabled={page === 1 || loading}
            className="rounded border px-4 py-2 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-40"
          >
            Previous
          </button>
          <span className="text-sm text-gray-500">
            {page} / {totalPages}
          </span>
          <button
            type="button"
            onClick={() => goToPage(page + 1)}
            disabled={page === totalPages || loading}
            className="rounded border px-4 py-2 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-40"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}

export default function MyBookingsPage() {
  return (
    <AuthGuard requiredRole="Customer">
      <MyBookingsPageContent />
    </AuthGuard>
  );
}
