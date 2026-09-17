import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from "@microsoft/signalr";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5172/api";

const HUB_URL = `${API_URL.replace(/\/api$/, "")}/hubs/bookings`;

export function createBookingHubConnection(token: string): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(HUB_URL, {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Information)
    .build();
}
