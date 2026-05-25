"use client";

import { useEffect, useRef, useState } from "react";
import { Mic, Square, Trash2 } from "lucide-react";

/**
 * Voice-note recorder using MediaRecorder (spec §7.5). Returns a blob URL for playback
 * and a Blob for upload. Transcription is server-side in Phase D.
 */
export function VoiceRecorder({
  onRecorded,
}: {
  onRecorded: (blob: Blob | null) => void;
}) {
  const recRef = useRef<MediaRecorder | null>(null);
  const chunks = useRef<Blob[]>([]);
  const [recording, setRecording] = useState(false);
  const [seconds, setSeconds] = useState(0);
  const [audioUrl, setAudioUrl] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let interval: ReturnType<typeof setInterval> | null = null;
    if (recording) {
      interval = setInterval(() => setSeconds((s) => s + 1), 1000);
    } else {
      setSeconds(0);
    }
    return () => {
      if (interval) clearInterval(interval);
    };
  }, [recording]);

  async function start() {
    setError(null);
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const rec = new MediaRecorder(stream, { mimeType: pickMimeType() });
      recRef.current = rec;
      chunks.current = [];

      rec.ondataavailable = (e) => {
        if (e.data.size > 0) chunks.current.push(e.data);
      };
      rec.onstop = () => {
        stream.getTracks().forEach((t) => t.stop());
        const blob = new Blob(chunks.current, { type: rec.mimeType });
        const url = URL.createObjectURL(blob);
        setAudioUrl(url);
        onRecorded(blob);
      };

      rec.start();
      setRecording(true);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Microphone permission denied.");
    }
  }

  function stop() {
    recRef.current?.stop();
    setRecording(false);
  }

  function discard() {
    if (audioUrl) URL.revokeObjectURL(audioUrl);
    setAudioUrl(null);
    onRecorded(null);
  }

  return (
    <div className="space-y-2 rounded-md border bg-card p-3">
      <div className="flex items-center justify-between gap-3">
        <span className="text-sm font-medium">Voice note</span>
        {!recording && !audioUrl && (
          <button
            type="button"
            onClick={start}
            className="flex items-center gap-1 rounded bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground"
          >
            <Mic className="h-4 w-4" /> Record
          </button>
        )}
        {recording && (
          <button
            type="button"
            onClick={stop}
            className="flex items-center gap-1 rounded bg-destructive px-3 py-1.5 text-sm font-medium text-destructive-foreground"
          >
            <Square className="h-4 w-4" /> Stop · {seconds}s
          </button>
        )}
      </div>

      {audioUrl && (
        <div className="flex items-center gap-2">
          <audio src={audioUrl} controls className="flex-1" />
          <button
            type="button"
            onClick={discard}
            className="rounded bg-muted px-2 py-1.5 text-muted-foreground"
            aria-label="Discard voice note"
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      )}

      {error && <p className="text-xs text-destructive">{error}</p>}
    </div>
  );
}

function pickMimeType(): string {
  const candidates = ["audio/webm;codecs=opus", "audio/webm", "audio/mp4"];
  for (const c of candidates) {
    if (typeof MediaRecorder !== "undefined" && MediaRecorder.isTypeSupported?.(c)) {
      return c;
    }
  }
  return "";
}
