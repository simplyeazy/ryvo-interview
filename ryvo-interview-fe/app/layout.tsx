import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";

import { AppHeader } from "@/components/app-header";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: {
    default: "Ryvo Timesheets",
    template: "%s · Ryvo",
  },
  description: "Contractor timesheets and client invoices.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html
      lang="en"
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="flex min-h-full flex-col bg-background text-foreground">
        <AppHeader />
        <main className="flex-1">{children}</main>
        <footer className="border-t px-4 py-4">
          <p className="mx-auto w-full max-w-6xl text-xs text-muted-foreground">
            Ryvo timesheets demo · dev identities only, no real authentication
          </p>
        </footer>
      </body>
    </html>
  );
}
