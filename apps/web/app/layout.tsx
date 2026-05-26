import type { Metadata, Viewport } from "next";
import { Inter } from "next/font/google";
import { QueryProvider } from "@/lib/providers/query-provider";
import { OfflineProvider } from "@/lib/providers/offline-provider";
import { UserPreferencesProvider } from "@/lib/providers/preferences-provider";
import { ToastProvider } from "@/components/ui/toast";
import "./globals.css";

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-sans",
  display: "swap",
});

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
    <html lang="en" suppressHydrationWarning className={inter.variable}>
      <body className="min-h-screen bg-background font-sans text-foreground antialiased">
        <UserPreferencesProvider>
          <QueryProvider>
            <OfflineProvider>
              <ToastProvider>{children}</ToastProvider>
            </OfflineProvider>
          </QueryProvider>
        </UserPreferencesProvider>
      </body>
    </html>
  );
}
