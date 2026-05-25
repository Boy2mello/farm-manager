"use client";

import { useEffect, useRef, useState } from "react";

type BarcodeDetectorCtor = new (init?: { formats?: string[] }) => {
  detect(image: ImageBitmap | HTMLVideoElement): Promise<Array<{ rawValue: string }>>;
};

declare global {
  interface Window {
    BarcodeDetector?: BarcodeDetectorCtor;
  }
}

/**
 * Ear-tag scanner — uses the BarcodeDetector + getUserMedia APIs (spec §7.5).
 * Supported on Chrome/Edge (desktop + Android). Safari iOS falls back to manual entry.
 */
export function BarcodeScanner({
  onDetect,
  onClose,
}: {
  onDetect: (value: string) => void;
  onClose: () => void;
}) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const [status, setStatus] = useState<"idle" | "scanning" | "unsupported" | "error">("idle");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    let stream: MediaStream | null = null;
    let raf = 0;
    let cancelled = false;

    async function start() {
      if (typeof window === "undefined" || !window.BarcodeDetector || !navigator.mediaDevices) {
        setStatus("unsupported");
        return;
      }

      try {
        stream = await navigator.mediaDevices.getUserMedia({
          video: { facingMode: { ideal: "environment" } },
          audio: false,
        });

        if (cancelled) return;
        if (videoRef.current) {
          videoRef.current.srcObject = stream;
          await videoRef.current.play();
        }

        const detector = new window.BarcodeDetector({
          formats: ["qr_code", "code_128", "code_39", "ean_13", "ean_8", "data_matrix"],
        });

        setStatus("scanning");

        async function tick() {
          if (cancelled || !videoRef.current) return;
          try {
            const codes = await detector.detect(videoRef.current);
            if (codes.length > 0) {
              onDetect(codes[0].rawValue);
              return;
            }
          } catch {
            /* keep retrying */
          }
          raf = requestAnimationFrame(tick);
        }
        raf = requestAnimationFrame(tick);
      } catch (err) {
        setStatus("error");
        setErrorMessage(err instanceof Error ? err.message : "Camera permission denied.");
      }
    }

    void start();

    return () => {
      cancelled = true;
      cancelAnimationFrame(raf);
      stream?.getTracks().forEach((t) => t.stop());
    };
  }, [onDetect]);

  return (
    <div className="fixed inset-0 z-50 flex flex-col bg-black/90">
      <header className="flex items-center justify-between p-4 text-white">
        <h2 className="text-lg font-semibold">Scan ear tag</h2>
        <button onClick={onClose} className="rounded bg-white/10 px-3 py-1 text-sm">Close</button>
      </header>

      <div className="relative flex-1">
        <video ref={videoRef} className="h-full w-full object-cover" playsInline muted />
        <div className="pointer-events-none absolute inset-x-12 top-1/3 h-32 rounded-lg border-2 border-white/70" />
      </div>

      <footer className="p-4 text-center text-sm text-white/80">
        {status === "unsupported" && (
          <p>Your browser can't scan codes here. Type the tag in manually instead.</p>
        )}
        {status === "scanning" && <p>Point the camera at the ear tag.</p>}
        {status === "error" && <p className="text-red-300">{errorMessage}</p>}
      </footer>
    </div>
  );
}
