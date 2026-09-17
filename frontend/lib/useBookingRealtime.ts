"use client";

import { useEffect } from "react";
import type { Booking } from "@/types/booking";
import { createBookingHubConnection } from "@/lib/signalr";
import { getToken } from "@/lib/auth";

interface BookingRealtimeHandlers {
  onCreated?: (booking: Booking) => void;
  onCancelled?: (booking: Booking) => void;
  onStatusUpdated?: (booking: Booking) => void;
}

export function useBookingRealtime(handlers: BookingRealtimeHandlers) {
  const { onCreated, onCancelled, onStatusUpdated } = handlers;

  useEffect(() => {
    const token = getToken();

    if (!token) {
      return;
    }

    const connection = createBookingHubConnection(token);

    let disposed = false;
    let started = false;

    if (onCreated) {
      connection.on("BookingCreated", onCreated);
    }

    if (onCancelled) {
      connection.on("BookingCancelled", onCancelled);
    }

    if (onStatusUpdated) {
      connection.on("BookingStatusUpdated", onStatusUpdated);
    }

    const startConnection = async () => {
      try {
        await connection.start();

        if (disposed) {
          await connection.stop();
          return;
        }

        started = true;

        console.log("SignalR connected");
      } catch (error) {
        if (!disposed) {
          console.error("SignalR connection failed:", error);
        }
      }
    };

    startConnection();

    return () => {
      disposed = true;

      if (onCreated) {
        connection.off("BookingCreated", onCreated);
      }

      if (onCancelled) {
        connection.off("BookingCancelled", onCancelled);
      }

      if (onStatusUpdated) {
        connection.off("BookingStatusUpdated", onStatusUpdated);
      }

      if (started) {
        connection.stop().catch(() => {});
      }
    };
  }, [onCreated, onCancelled, onStatusUpdated]);
}
