"use client";

import React, { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { apiRoutes } from "@/lib/api-routes";
import type { Staff } from "@/types/staff";
import type {
  CreateScheduleRequest,
  UpdateScheduleRequest,
} from "@/types/schedule";

import AuthGuard from "@/components/auth/AuthGuard";

interface Schedule {
  id: string;
  staffId: string;
  staffName: string;
  workDate: string;
  startTime: string;
  endTime: string;
}

interface ScheduleResponse {
  items?: Schedule[];
}

function getToday() {
  return new Date().toISOString().slice(0, 10);
}

function AdminSchedulesPageContent() {
  const [staffs, setStaffs] = useState<Staff[]>([]);
  const [schedules, setSchedules] = useState<Schedule[]>([]);

  const [selectedStaffId, setSelectedStaffId] = useState("");
  const [selectedDate, setSelectedDate] = useState(getToday());

  const [startTime, setStartTime] = useState("08:00");
  const [endTime, setEndTime] = useState("12:00");

  const [editingId, setEditingId] = useState<string | null>(null);

  const [loadingStaffs, setLoadingStaffs] = useState(true);
  const [loadingSchedules, setLoadingSchedules] = useState(false);
  const [saving, setSaving] = useState(false);

  const [error, setError] = useState("");

  async function loadStaffs() {
    try {
      setLoadingStaffs(true);
      setError("");

      const result = await api.get<Staff[]>(apiRoutes.staffs.list);

      setStaffs(result);

      if (result.length > 0 && !selectedStaffId) {
        setSelectedStaffId(result[0].id);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load staffs.");
    } finally {
      setLoadingStaffs(false);
    }
  }

  async function loadSchedules() {
    if (!selectedStaffId || !selectedDate) {
      setSchedules([]);
      return;
    }

    try {
      setLoadingSchedules(true);
      setError("");

      const result = await api.get<ScheduleResponse | Schedule[]>(
        apiRoutes.schedules.list(selectedStaffId, {
          from: selectedDate,
          to: selectedDate,
        }),
      );

      const items = Array.isArray(result) ? result : (result.items ?? []);

      setSchedules(
        [...items].sort((a, b) => a.startTime.localeCompare(b.startTime)),
      );
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to load schedules.",
      );
    } finally {
      setLoadingSchedules(false);
    }
  }

  useEffect(() => {
    loadStaffs();
  }, []);

  useEffect(() => {
    loadSchedules();
  }, [selectedStaffId, selectedDate]);

  function resetForm() {
    setEditingId(null);
    setStartTime("08:00");
    setEndTime("12:00");
  }

  function startEdit(schedule: Schedule) {
    setEditingId(schedule.id);

    setStartTime(schedule.startTime.slice(0, 5));
    setEndTime(schedule.endTime.slice(0, 5));
  }

  async function handleSubmit(event: React.SubmitEvent<HTMLFormElement>) {
    event.preventDefault();

    setError("");

    if (!selectedStaffId) {
      setError("Please select a staff member.");
      return;
    }

    if (!selectedDate) {
      setError("Please select a work date.");
      return;
    }

    if (startTime >= endTime) {
      setError("Start time must be earlier than end time.");
      return;
    }

    try {
      setSaving(true);

      if (editingId) {
        const body: UpdateScheduleRequest = {
          workDate: selectedDate,
          startTime,
          endTime,
        };

        await api.put(apiRoutes.schedules.update(editingId), body);
      } else {
        const body: CreateScheduleRequest = {
          workDate: selectedDate,
          startTime,
          endTime,
        };

        await api.post(apiRoutes.schedules.create(selectedStaffId), body);
      }

      resetForm();

      await loadSchedules();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save schedule.");
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(schedule: Schedule) {
    const confirmed = window.confirm(
      `Delete ${schedule.startTime.slice(0, 5)} - ${schedule.endTime.slice(0, 5)}?`,
    );

    if (!confirmed) {
      return;
    }

    try {
      setError("");

      await api.delete(apiRoutes.schedules.delete(schedule.id));

      if (editingId === schedule.id) {
        resetForm();
      }

      await loadSchedules();
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to delete schedule.",
      );
    }
  }

  const selectedStaff = staffs.find((staff) => staff.id === selectedStaffId);

  return (
    <main className="mx-auto max-w-7xl px-4 py-10 sm:px-6 lg:px-8">
      <header className="mb-8">
        <p className="text-sm font-bold uppercase tracking-[0.16em] text-(--brand)">
          Admin workspace
        </p>
        <h1 className="mt-3 text-4xl font-bold tracking-tight text-foreground">
          Schedule management
        </h1>
        <p className="mt-3 text-base leading-7 text-(--muted)">
          Set working hours so customers can see accurate availability.
        </p>
      </header>

      {error && (
        <div
          className="mb-6 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700"
          role="alert"
        >
          {error}
        </div>
      )}

      <div className="grid items-start gap-6 lg:grid-cols-[minmax(280px,0.75fr)_minmax(0,1.25fr)]">
        <section className="rounded-2xl border border-(--border) bg-white p-6 sm:p-8">
          <p className="text-xs font-bold uppercase tracking-[0.16em] text-(--muted)">
            Working hours
          </p>
          <h2 className="mt-2 text-2xl font-bold tracking-tight text-foreground">
            {editingId ? "Edit schedule" : "Create schedule"}
          </h2>
          <form onSubmit={handleSubmit} className="mt-6 space-y-5">
            <div>
              <label
                htmlFor="schedule-staff"
                className="mb-2 block text-sm font-semibold text-foreground"
              >
                Staff member
              </label>
              <select
                id="schedule-staff"
                value={selectedStaffId}
                disabled={loadingStaffs || Boolean(editingId)}
                onChange={(event) => setSelectedStaffId(event.target.value)}
                className="w-full rounded-xl border border-(--border) px-4 py-3 text-sm outline-none transition focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed] disabled:bg-gray-50"
              >
                {staffs.map((staff) => (
                  <option key={staff.id} value={staff.id}>
                    {staff.fullName} — {staff.email}
                    {!staff.isActive ? " (Inactive)" : ""}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label
                htmlFor="schedule-date"
                className="mb-2 block text-sm font-semibold text-foreground"
              >
                Work date
              </label>
              <input
                id="schedule-date"
                type="date"
                value={selectedDate}
                onChange={(event) => setSelectedDate(event.target.value)}
                className="w-full rounded-xl border border-(--border) px-4 py-3 text-sm outline-none transition focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed]"
              />
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <div>
                <label
                  htmlFor="schedule-start"
                  className="mb-2 block text-sm font-semibold text-foreground"
                >
                  Start time
                </label>
                <input
                  id="schedule-start"
                  type="time"
                  value={startTime}
                  onChange={(event) => setStartTime(event.target.value)}
                  className="w-full rounded-xl border border-(--border) px-4 py-3 text-sm outline-none transition focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed]"
                />
              </div>
              <div>
                <label
                  htmlFor="schedule-end"
                  className="mb-2 block text-sm font-semibold text-foreground"
                >
                  End time
                </label>
                <input
                  id="schedule-end"
                  type="time"
                  value={endTime}
                  onChange={(event) => setEndTime(event.target.value)}
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
                    ? "Update schedule"
                    : "Create schedule"}
              </button>
              {editingId && (
                <button
                  type="button"
                  onClick={resetForm}
                  className="rounded-xl border border-(--border) px-4 py-3 text-sm font-semibold text-foreground hover:bg-gray-50"
                >
                  Cancel
                </button>
              )}
            </div>
          </form>
        </section>

        <section className="rounded-2xl border border-(--border) bg-white p-6 sm:p-8">
          <div className="border-b border-(--border) pb-6">
            <p className="text-xs font-bold uppercase tracking-[0.16em] text-(--muted)">
              Daily coverage
            </p>
            <h2 className="mt-2 text-2xl font-bold tracking-tight text-foreground">
              Schedules
            </h2>
            {selectedStaff && (
              <p className="mt-2 text-sm text-(--muted)">
                {selectedStaff.fullName} · {selectedDate}
              </p>
            )}
          </div>
          {loadingSchedules ? (
            <div className="mt-6 space-y-3">
              <div className="h-20 animate-pulse rounded-xl bg-gray-100" />
              <div className="h-20 animate-pulse rounded-xl bg-gray-100" />
            </div>
          ) : schedules.length === 0 ? (
            <div className="mt-6 rounded-xl border border-dashed border-gray-300 px-6 py-12 text-center">
              <p className="font-semibold text-foreground">
                No schedules for this date
              </p>
              <p className="mt-1 text-sm text-(--muted)">
                Create a working window to make this day bookable.
              </p>
            </div>
          ) : (
            <div className="mt-6 space-y-3">
              {schedules.map((schedule) => (
                <div
                  key={schedule.id}
                  className="flex flex-col justify-between gap-4 rounded-xl border border-(--border) bg-gray-50/60 p-4 sm:flex-row sm:items-center"
                >
                  <div>
                    <p className="text-lg font-bold text-foreground">
                      {schedule.startTime.slice(0, 5)}{" "}
                      <span className="font-normal text-gray-400">to</span>{" "}
                      {schedule.endTime.slice(0, 5)}
                    </p>
                    <p className="mt-1 text-sm text-(--muted)">
                      {schedule.workDate}
                    </p>
                  </div>
                  <div className="flex gap-2">
                    <button
                      type="button"
                      onClick={() => startEdit(schedule)}
                      className="rounded-lg border border-(--border) bg-white px-3 py-2 text-xs font-semibold text-foreground hover:bg-gray-50"
                    >
                      Edit
                    </button>
                    <button
                      type="button"
                      onClick={() => handleDelete(schedule)}
                      className="rounded-lg px-3 py-2 text-xs font-semibold text-red-600 hover:bg-red-50"
                    >
                      Delete
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>
    </main>
  );
}

export default function AdminSchedulesPage() {
  return (
    <AuthGuard requiredRole="Admin">
      <AdminSchedulesPageContent />
    </AuthGuard>
  );
}
