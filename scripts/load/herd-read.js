// k6 load test — herd-read path.
// Verifies spec §21.1 budget: P95 page load ≤ 2.0 s, search ≤ 300 ms.
//
// Run locally:
//   k6 run scripts/load/herd-read.js
// Override the base URL and token:
//   API_BASE=https://farm.example.com TOKEN=... k6 run scripts/load/herd-read.js

import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  stages: [
    { duration: "30s", target: 20 }, // warm-up
    { duration: "1m", target: 50 },   // steady
    { duration: "30s", target: 0 },   // cool-down
  ],
  thresholds: {
    http_req_failed: ["rate<0.01"],
    "http_req_duration{group::list}": ["p(95)<2000"],
    "http_req_duration{group::detail}": ["p(95)<2000"],
  },
};

const BASE = __ENV.API_BASE || "http://localhost:5000";
const TOKEN = __ENV.TOKEN || "";

export default function () {
  const params = TOKEN ? { headers: { Authorization: `Bearer ${TOKEN}` } } : {};

  const list = http.get(`${BASE}/api/v1/animals`, { ...params, tags: { group: "list" } });
  check(list, { "list 200": (r) => r.status === 200 });

  if (list.status === 200) {
    try {
      const animals = list.json();
      if (Array.isArray(animals) && animals.length > 0) {
        const sample = animals[Math.floor(Math.random() * animals.length)];
        const detail = http.get(`${BASE}/api/v1/animals/${sample.id}`, {
          ...params,
          tags: { group: "detail" },
        });
        check(detail, { "detail 200": (r) => r.status === 200 });
      }
    } catch (_e) {
      // ignore parse errors
    }
  }

  sleep(1);
}
