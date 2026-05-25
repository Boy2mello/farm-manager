import type { Metadata, Viewport } from "next";
import { QueryProvider } from "@/lib/providers/query-provider";
import { OfflineProvider } from "@/lib/providers/offline-provider";
import { UserPreferencesProvider } from "@/lib/providers/preferences-provider";
import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "Farm Manager",
    template: "%s · Farm Manager",
  },
  description:
    "Mobile-first livestock management — capture once, compute everything.",
  applicationName: "Farm Manager",
  appleWebApp: {
    capable: true,
    statusBarStyle: "default",
    title: "Farm Manager",
  },
  formatDetection: { telephone: false },
  manifest: "/manifest.webmanifest",
};

export const viewport: Viewport = {
  themeColor: "#15803d",
  width: "device-width",
  initialScale: 1,
  viewportFit: "cover",
  userScalable: false,
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body className="min-h-screen bg-background text-foreground antialiased">
        <UserPreferencesProvider>
          <QueryProvider>
            <OfflineProvider>{children}</OfflineProvider>
          </QueryProvider>
        </UserPreferencesProvider>
      </body>
    </html>
  );
}
