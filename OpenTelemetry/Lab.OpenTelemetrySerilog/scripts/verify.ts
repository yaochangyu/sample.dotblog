#!/usr/bin/env npx tsx
/**
 * OpenTelemetry Lab 驗證腳本 — Node 18+ 內建 fetch，用 `npx tsx` 執行。
 */

const results: Array<{ passed: boolean; description: string }> = [];

function record(passed: boolean, description: string, error = ""): void {
  const tag = passed ? "[PASS]" : "[FAIL]";
  const msg = passed ? `${tag} ${description}` : `${tag} ${description} — ${error}`;
  console.log(msg);
  results.push({ passed, description });
}

async function httpGet(url: string): Promise<Response> {
  return fetch(url, { redirect: "manual" });
}

async function httpPostJson(url: string, body: Record<string, unknown>): Promise<Response> {
  return fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

// ---------- 驗證項目 ----------

async function verifyFrontendUi(): Promise<void> {
  const desc = "前端 UI (http://localhost:3000) 回應 HTTP 200";
  try {
    const resp = await httpGet("http://localhost:3000");
    if (resp.status === 200) {
      record(true, desc);
    } else {
      record(false, desc, `HTTP ${resp.status}`);
    }
  } catch (e: unknown) {
    record(false, desc, String(e));
  }
}

async function verifyGetWeatherApi(): Promise<void> {
  const desc = "GET /api/weather 回傳含必要欄位的 JSON 陣列";
  try {
    const resp = await fetch("http://localhost:3000/api/weather");
    const data = await resp.json();
    if (!Array.isArray(data) || data.length === 0) {
      record(false, desc, `預期非空 JSON 陣列，實際: ${typeof data} (len=${Array.isArray(data) ? data.length : "N/A"})`);
      return;
    }
    const required = ["date", "temperatureC", "temperatureF", "summary"];
    const keys = Object.keys(data[0]);
    const missing = required.filter((k) => !keys.includes(k));
    if (missing.length > 0) {
      record(false, desc, `缺少欄位: ${missing.join(", ")}`);
    } else {
      record(true, desc);
    }
  } catch (e: unknown) {
    record(false, desc, String(e));
  }
}

async function verifyPostWeatherApi(): Promise<void> {
  const desc = "POST /api/weather 回傳含 temperatureF 的天氣資料";
  try {
    const body = { date: "2025-01-01", temperatureC: 25, summary: "Warm" };
    const resp = await httpPostJson("http://localhost:3000/api/weather", body);
    const data = await resp.json();
    if (data && typeof data === "object" && "temperatureF" in data) {
      record(true, desc);
    } else {
      record(false, desc, `回傳中缺少 temperatureF，實際: ${JSON.stringify(data).slice(0, 200)}`);
    }
  } catch (e: unknown) {
    record(false, desc, String(e));
  }
}

async function verifyBackendA(): Promise<void> {
  const desc = "backend-a (http://localhost:5100/Weather) 回傳 JSON 陣列";
  try {
    const resp = await fetch("http://localhost:5100/Weather");
    const data = await resp.json();
    if (Array.isArray(data) && data.length > 0) {
      record(true, desc);
    } else {
      record(false, desc, `預期非空 JSON 陣列，實際: ${typeof data}`);
    }
  } catch (e: unknown) {
    record(false, desc, String(e));
  }
}

async function verifyBackendB(): Promise<void> {
  const desc = "backend-b (http://localhost:5200/Weather) 回傳 JSON 陣列";
  try {
    const resp = await fetch("http://localhost:5200/Weather");
    const data = await resp.json();
    if (Array.isArray(data) && data.length > 0) {
      record(true, desc);
    } else {
      record(false, desc, `預期非空 JSON 陣列，實際: ${typeof data}`);
    }
  } catch (e: unknown) {
    record(false, desc, String(e));
  }
}

async function verifyJaegerUi(): Promise<void> {
  const desc = "Jaeger UI (http://localhost:16686) 回應 HTTP 200";
  try {
    const resp = await httpGet("http://localhost:16686");
    if (resp.status === 200) {
      record(true, desc);
    } else {
      record(false, desc, `HTTP ${resp.status}`);
    }
  } catch (e: unknown) {
    record(false, desc, String(e));
  }
}

async function verifySeqUi(): Promise<void> {
  const desc = "Seq UI (http://localhost:5341) 回應 HTTP 200";
  try {
    const resp = await httpGet("http://localhost:5341");
    if (resp.status === 200) {
      record(true, desc);
    } else {
      record(false, desc, `HTTP ${resp.status}`);
    }
  } catch (e: unknown) {
    record(false, desc, String(e));
  }
}

async function verifyAspireDashboard(): Promise<void> {
  const desc = "Aspire Dashboard (http://localhost:18888) 回應 HTTP 200 或 302";
  try {
    const resp = await httpGet("http://localhost:18888");
    if (resp.status === 200 || resp.status === 302) {
      record(true, desc);
    } else {
      record(false, desc, `HTTP ${resp.status}`);
    }
  } catch (e: unknown) {
    record(false, desc, String(e));
  }
}

async function verifyJaegerServices(): Promise<void> {
  const desc = "Jaeger 已註冊 frontend、backend-a、backend-b 服務";
  try {
    const resp = await fetch("http://localhost:16686/api/services");
    const data = await resp.json();
    const services: string[] = data?.data ?? [];
    const required = ["frontend", "backend-a", "backend-b"];
    const missing = required.filter((s) => !services.includes(s));
    if (missing.length === 0) {
      record(true, desc);
    } else {
      record(false, desc, `缺少服務: ${missing.join(", ")}，現有: ${services.join(", ")}`);
    }
  } catch (e: unknown) {
    record(false, desc, String(e));
  }
}

// ---------- Trace 驗證輔助型別與函式 ----------

interface JaegerTag {
  key: string;
  type: string;
  value: unknown;
}

interface JaegerSpan {
  traceID: string;
  spanID: string;
  operationName: string;
  references: Array<{ refType: string; traceID: string; spanID: string }>;
  tags: JaegerTag[];
  processID: string;
}

interface JaegerTrace {
  traceID: string;
  spans: JaegerSpan[];
  processes: Record<string, { serviceName: string; tags: JaegerTag[] }>;
}

let traceCache: Record<string, JaegerTrace[]> | null = null;

async function ensureTracesLoaded(): Promise<Record<string, JaegerTrace[]>> {
  if (traceCache) return traceCache;

  // 透過 frontend proxy 觸發完整鏈路
  try {
    await fetch("http://localhost:3000/api/weather");
  } catch {
    // ignore
  }

  // 直接觸發 backend-a → backend-b
  try {
    await fetch("http://localhost:5100/Weather");
  } catch {
    // ignore
  }

  await sleep(3000);

  const cache: Record<string, JaegerTrace[]> = {};
  for (const service of ["frontend", "backend-a", "backend-b"]) {
    try {
      const resp = await fetch(`http://localhost:16686/api/traces?service=${service}&limit=10&lookback=1h`);
      const data = await resp.json();
      cache[service] = data?.data ?? [];
    } catch {
      cache[service] = [];
    }
  }

  traceCache = cache;
  return cache;
}

function getServiceNames(trace: JaegerTrace): Set<string> {
  const names = new Set<string>();
  for (const proc of Object.values(trace.processes ?? {})) {
    if (proc.serviceName) names.add(proc.serviceName);
  }
  return names;
}

function getSpanService(span: JaegerSpan, processes: JaegerTrace["processes"]): string {
  return processes[span.processID]?.serviceName ?? "";
}

function getTagValue(tags: JaegerTag[], key: string): unknown | undefined {
  return tags.find((t) => t.key === key)?.value;
}

function findTraceWithServices(traces: JaegerTrace[], required: Set<string>): JaegerTrace | undefined {
  return traces.find((trace) => {
    const svcs = getServiceNames(trace);
    for (const r of required) {
      if (!svcs.has(r)) return false;
    }
    return true;
  });
}

// ---------- Trace 鏈路驗證項目 ----------

async function verifyTraceBackendChain(): Promise<void> {
  const desc = "backend-a → backend-b trace 具有 parent-child span 關係";
  try {
    const cache = await ensureTracesLoaded();
    const traces = cache["backend-a"] ?? [];
    if (traces.length === 0) {
      record(false, desc, "找不到 backend-a 的 traces");
      return;
    }

    const trace = findTraceWithServices(traces, new Set(["backend-a", "backend-b"]));
    if (!trace) {
      record(false, desc, `沒有 trace 同時包含 backend-a 與 backend-b (共 ${traces.length} 筆)`);
      return;
    }

    const spanMap = new Map(trace.spans.map((s) => [s.spanID, s]));

    for (const span of trace.spans) {
      if (getSpanService(span, trace.processes) !== "backend-b") continue;
      for (const ref of span.references ?? []) {
        if (ref.refType !== "CHILD_OF") continue;
        const parent = spanMap.get(ref.spanID);
        if (parent && getSpanService(parent, trace.processes) === "backend-a") {
          record(true, desc);
          return;
        }
      }
    }

    record(false, desc, "backend-b span 沒有以 CHILD_OF 連結到 backend-a span");
  } catch (e: unknown) {
    record(false, desc, String(e));
  }
}

async function verifyTraceFrontendChain(): Promise<void> {
  const desc = "完整 trace 鏈路包含 frontend → backend-a → backend-b 三個服務";
  try {
    const cache = await ensureTracesLoaded();

    // 合併所有來源的 traces 並去重
    const seen = new Set<string>();
    const allTraces: JaegerTrace[] = [];
    for (const svcTraces of Object.values(cache)) {
      for (const trace of svcTraces) {
        if (trace.traceID && !seen.has(trace.traceID)) {
          seen.add(trace.traceID);
          allTraces.push(trace);
        }
      }
    }

    const required = new Set(["frontend", "backend-a", "backend-b"]);
    const trace = findTraceWithServices(allTraces, required);
    if (trace) {
      record(true, desc);
      return;
    }

    const combos: string[] = [];
    for (const t of allTraces.slice(0, 10)) {
      const svcs = [...getServiceNames(t)].sort();
      if (svcs.length > 1) combos.push(`[${svcs.join(", ")}]`);
    }
    const hint = combos.length > 0 ? `，找到的服務組合: ${combos.slice(0, 5).join("; ")}` : "";
    record(false, desc, `沒有 trace 同時包含三個服務 (需先以瀏覽器操作前端)${hint}`);
  } catch (e: unknown) {
    record(false, desc, String(e));
  }
}

async function verifyTraceHttpAttributes(): Promise<void> {
  const desc = "Trace spans 包含 HTTP method 與 status code 屬性";
  try {
    const cache = await ensureTracesLoaded();
    const traces = cache["backend-a"] ?? [];

    const trace = findTraceWithServices(traces, new Set(["backend-a", "backend-b"]));
    if (!trace) {
      record(false, desc, "找不到含 backend-a 與 backend-b 的 trace");
      return;
    }

    let hasMethod = false;
    let hasStatus = false;
    for (const span of trace.spans) {
      if (getTagValue(span.tags, "http.method") !== undefined || getTagValue(span.tags, "http.request.method") !== undefined) {
        hasMethod = true;
      }
      if (getTagValue(span.tags, "http.status_code") !== undefined || getTagValue(span.tags, "http.response.status_code") !== undefined) {
        hasStatus = true;
      }
    }

    if (hasMethod && hasStatus) {
      record(true, desc);
    } else {
      const missing: string[] = [];
      if (!hasMethod) missing.push("http.method / http.request.method");
      if (!hasStatus) missing.push("http.status_code / http.response.status_code");
      record(false, desc, `缺少屬性: ${missing.join(", ")}`);
    }
  } catch (e: unknown) {
    record(false, desc, String(e));
  }
}

async function verifyTraceSpanCount(): Promise<void> {
  const desc = "backend-a → backend-b trace 至少 3 個 spans 且含 CLIENT 與 SERVER 類型";
  try {
    const cache = await ensureTracesLoaded();
    const traces = cache["backend-a"] ?? [];

    const trace = findTraceWithServices(traces, new Set(["backend-a", "backend-b"]));
    if (!trace) {
      record(false, desc, "找不到含 backend-a 與 backend-b 的 trace");
      return;
    }

    const spanCount = trace.spans.length;
    const kinds = new Set<string>();
    for (const span of trace.spans) {
      const kind = getTagValue(span.tags, "span.kind");
      if (typeof kind === "string") kinds.add(kind);
    }

    if (spanCount >= 3 && kinds.has("client") && kinds.has("server")) {
      record(true, desc);
    } else if (spanCount < 3) {
      record(false, desc, `僅有 ${spanCount} 個 spans，預期至少 3 個`);
    } else {
      record(false, desc, `span.kind 不完整: 找到 [${[...kinds].sort().join(", ")}]，預期包含 client 與 server`);
    }
  } catch (e: unknown) {
    record(false, desc, String(e));
  }
}

// ---------- 主程式 ----------

async function main(): Promise<void> {
  console.log("=".repeat(60));
  console.log("OpenTelemetry Lab — 驗證腳本");
  console.log("=".repeat(60));
  console.log();

  const checks = [
    verifyFrontendUi,
    verifyGetWeatherApi,
    verifyPostWeatherApi,
    verifyBackendA,
    verifyBackendB,
    verifyJaegerUi,
    verifySeqUi,
    verifyAspireDashboard,
    verifyJaegerServices,
    verifyTraceBackendChain,
    verifyTraceFrontendChain,
    verifyTraceHttpAttributes,
    verifyTraceSpanCount,
  ];

  for (const check of checks) {
    await check();
  }

  console.log();
  console.log("-".repeat(60));
  const passed = results.filter((r) => r.passed).length;
  const total = results.length;
  const failed = total - passed;
  console.log(`結果：${passed}/${total} 通過，${failed} 失敗`);

  process.exit(failed === 0 ? 0 : 1);
}

main();
