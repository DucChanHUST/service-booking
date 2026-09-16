"use client";

import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import type { Booking, BookingStatus } from "@/types/booking";

import AuthGuard from "@/components/auth/AuthGuard";

function formatDateTime(value: string) {
  const date = new Date(value);

  return date.toLocaleString("vi-VN", {
    dateStyle: "medium",
    timeStyle: "short",
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

  const [loading, setLoading] = useState(true);

  const [error, setError] = useState("");

  const [cancellingId, setCancellingId] = useState<string | null>(null);

  const [cancellationReason, setCancellationReason] = useState("");

  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    loadBookings();
  }, []);

  async function loadBookings() {
    try {
      setLoading(true);
      setError("");

      const data = await api.get<Booking[]>("/bookings/my");

      setBookings(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load bookings.");
    } finally {
      setLoading(false);
    }
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

      await api.patch(`/bookings/${bookingId}/cancel`, {
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

  if (loading) {
    return <div className="mx-auto max-w-6xl p-6">Loading bookings...</div>;
  }

  return (
    <div className="mx-auto max-w-6xl p-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold">My Bookings</h1>

        <p className="mt-2 text-gray-600">View and manage your bookings.</p>
      </div>

      {error && (
        <div className="mb-6 rounded border border-red-200 bg-red-50 p-4 text-red-700">
          {error}
        </div>
      )}

      {bookings.length === 0 ? (
        <div className="rounded-lg border bg-white p-10 text-center">
          <p className="text-gray-500">You don't have any bookings yet.</p>
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
