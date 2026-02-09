#!/usr/bin/env python3
"""OpenTelemetry Lab 驗證腳本 — 零依賴，僅使用 Python 標準庫。"""

import json
import sys
import time
import urllib.error
import urllib.request

results: list[tuple[bool, str]] = []


def record(passed: bool, description: str, error: str = "") -> None:
    tag = "[PASS]" if passed else "[FAIL]"
    msg = f"{tag} {description}" if passed else f"{tag} {description} — {error}"
    print(msg)
    results.append((passed, description))


def http_get(url: str, *, timeout: int = 10) -> urllib.request.http.client.HTTPResponse:
    req = urllib.request.Request(url)
    return urllib.request.urlopen(req, timeout=timeout)


def http_post_json(url: str, body: dict, *, timeout: int = 10) -> urllib.request.http.client.HTTPResponse:
    data = json.dumps(body).encode()
    req = urllib.request.Request(url, data=data, headers={"Content-Type": "application/json"})
    return urllib.request.urlopen(req, timeout=timeout)


# ---------- 驗證項目 ----------


def verify_frontend_ui() -> None:
    """1. 前端 UI http://localhost:3000 → HTTP 200"""
    desc = "前端 UI (http://localhost:3000) 回應 HTTP 200"
    try:
        resp = http_get("http://localhost:3000")
        if resp.status == 200:
            record(True, desc)
        else:
            record(False, desc, f"HTTP {resp.status}")
    except Exception as e:
        record(False, desc, str(e))


def verify_get_weather_api() -> None:
    """2. GET /api/weather → JSON 陣列，包含必要欄位"""
    desc = "GET /api/weather 回傳含必要欄位的 JSON 陣列"
    try:
        resp = http_get("http://localhost:3000/api/weather")
        data = json.loads(resp.read())
        if not isinstance(data, list) or len(data) == 0:
            record(False, desc, f"預期非空 JSON 陣列，實際: {type(data).__name__} (len={len(data) if isinstance(data, list) else 'N/A'})")
            return
        required = {"date", "temperatureC", "temperatureF", "summary"}
        first = data[0]
        missing = required - set(first.keys())
        if missing:
            record(False, desc, f"缺少欄位: {missing}")
        else:
            record(True, desc)
    except Exception as e:
        record(False, desc, str(e))


def verify_post_weather_api() -> None:
    """3. POST /api/weather → 回傳含 temperatureF 的天氣資料"""
    desc = "POST /api/weather 回傳含 temperatureF 的天氣資料"
    try:
        body = {"date": "2025-01-01", "temperatureC": 25, "summary": "Warm"}
        resp = http_post_json("http://localhost:3000/api/weather", body)
        data = json.loads(resp.read())
        if "temperatureF" in (data if isinstance(data, dict) else {}):
            record(True, desc)
        else:
            record(False, desc, f"回傳中缺少 temperatureF，實際: {json.dumps(data, ensure_ascii=False)[:200]}")
    except Exception as e:
        record(False, desc, str(e))


def verify_backend_a() -> None:
    """4. backend-a http://localhost:5100/Weather → JSON 陣列"""
    desc = "backend-a (http://localhost:5100/Weather) 回傳 JSON 陣列"
    try:
        resp = http_get("http://localhost:5100/Weather")
        data = json.loads(resp.read())
        if isinstance(data, list) and len(data) > 0:
            record(True, desc)
        else:
            record(False, desc, f"預期非空 JSON 陣列，實際: {type(data).__name__}")
    except Exception as e:
        record(False, desc, str(e))


def verify_backend_b() -> None:
    """5. backend-b http://localhost:5200/Weather → JSON 陣列"""
    desc = "backend-b (http://localhost:5200/Weather) 回傳 JSON 陣列"
    try:
        resp = http_get("http://localhost:5200/Weather")
        data = json.loads(resp.read())
        if isinstance(data, list) and len(data) > 0:
            record(True, desc)
        else:
            record(False, desc, f"預期非空 JSON 陣列，實際: {type(data).__name__}")
    except Exception as e:
        record(False, desc, str(e))


def verify_jaeger_ui() -> None:
    """6. Jaeger UI http://localhost:16686 → HTTP 200"""
    desc = "Jaeger UI (http://localhost:16686) 回應 HTTP 200"
    try:
        resp = http_get("http://localhost:16686")
        if resp.status == 200:
            record(True, desc)
        else:
            record(False, desc, f"HTTP {resp.status}")
    except Exception as e:
        record(False, desc, str(e))


def verify_seq_ui() -> None:
    """7. Seq UI http://localhost:5341 → HTTP 200"""
    desc = "Seq UI (http://localhost:5341) 回應 HTTP 200"
    try:
        resp = http_get("http://localhost:5341")
        if resp.status == 200:
            record(True, desc)
        else:
            record(False, desc, f"HTTP {resp.status}")
    except Exception as e:
        record(False, desc, str(e))


def verify_aspire_dashboard() -> None:
    """8. Aspire Dashboard http://localhost:18888 → HTTP 200 或 302"""
    desc = "Aspire Dashboard (http://localhost:18888) 回應 HTTP 200 或 302"
    try:
        # 不跟隨重導向，手動檢查狀態碼
        req = urllib.request.Request("http://localhost:18888")
        opener = urllib.request.build_opener(urllib.request.HTTPHandler)
        try:
            resp = opener.open(req, timeout=10)
            status = resp.status
        except urllib.error.HTTPError as e:
            status = e.code
        if status in (200, 302):
            record(True, desc)
        else:
            record(False, desc, f"HTTP {status}")
    except Exception as e:
        record(False, desc, str(e))


def verify_jaeger_services() -> None:
    """9. Jaeger services 包含 frontend、backend-a、backend-b"""
    desc = "Jaeger 已註冊 frontend、backend-a、backend-b 服務"
    try:
        resp = http_get("http://localhost:16686/api/services")
        data = json.loads(resp.read())
        services = data.get("data", [])
        required = {"frontend", "backend-a", "backend-b"}
        found = required & set(services)
        missing = required - found
        if not missing:
            record(True, desc)
        else:
            record(False, desc, f"缺少服務: {missing}，現有: {services}")
    except Exception as e:
        record(False, desc, str(e))


# ---------- Trace 驗證輔助函式 ----------

_trace_cache: dict[str, list] = {}


def _ensure_traces_loaded() -> dict[str, list]:
    """觸發請求並快取 traces 資料，避免重複觸發與等待"""
    global _trace_cache
    if _trace_cache:
        return _trace_cache

    # 透過 frontend proxy 觸發完整鏈路
    try:
        http_get("http://localhost:3000/api/weather")
    except Exception:
        pass

    # 直接觸發 backend-a → backend-b
    try:
        http_get("http://localhost:5100/Weather")
    except Exception:
        pass

    time.sleep(3)

    for service in ("frontend", "backend-a", "backend-b"):
        try:
            resp = http_get(f"http://localhost:16686/api/traces?service={service}&limit=10&lookback=1h")
            data = json.loads(resp.read())
            _trace_cache[service] = data.get("data", [])
        except Exception:
            _trace_cache[service] = []

    return _trace_cache


def _get_service_names(trace: dict) -> set[str]:
    """取得 trace 中所有服務名稱"""
    return {
        proc.get("serviceName", "")
        for proc in trace.get("processes", {}).values()
        if proc.get("serviceName")
    }


def _get_span_service(span: dict, processes: dict) -> str:
    """取得 span 對應的服務名稱"""
    pid = span.get("processID", "")
    return processes.get(pid, {}).get("serviceName", "")


def _get_tag_value(tags: list, key: str):
    """從 tags 列表中取得指定 key 的值"""
    for tag in tags:
        if tag.get("key") == key:
            return tag.get("value")
    return None


def _find_trace_with_services(traces: list, required: set[str]) -> dict | None:
    """找到第一筆包含所有指定服務的 trace"""
    for trace in traces:
        if required.issubset(_get_service_names(trace)):
            return trace
    return None


# ---------- Trace 鏈路驗證項目 ----------


def verify_trace_backend_chain() -> None:
    """10. backend-a → backend-b 具有正確的 parent-child span 關係"""
    desc = "backend-a → backend-b trace 具有 parent-child span 關係"
    try:
        traces_by_svc = _ensure_traces_loaded()
        traces = traces_by_svc.get("backend-a", [])
        if not traces:
            record(False, desc, "找不到 backend-a 的 traces")
            return

        trace = _find_trace_with_services(traces, {"backend-a", "backend-b"})
        if not trace:
            record(False, desc, f"沒有 trace 同時包含 backend-a 與 backend-b (共 {len(traces)} 筆)")
            return

        spans = trace.get("spans", [])
        processes = trace.get("processes", {})
        span_map = {s["spanID"]: s for s in spans}

        # 尋找 backend-b 的 span，其 parent 為 backend-a 的 span
        for span in spans:
            if _get_span_service(span, processes) != "backend-b":
                continue
            for ref in span.get("references", []):
                if ref.get("refType") != "CHILD_OF":
                    continue
                parent = span_map.get(ref.get("spanID"))
                if parent and _get_span_service(parent, processes) == "backend-a":
                    record(True, desc)
                    return

        record(False, desc, "backend-b span 沒有以 CHILD_OF 連結到 backend-a span")
    except Exception as e:
        record(False, desc, str(e))


def verify_trace_frontend_chain() -> None:
    """11. 完整鏈路: trace 包含 frontend、backend-a、backend-b 三個服務"""
    desc = "完整 trace 鏈路包含 frontend → backend-a → backend-b 三個服務"
    try:
        traces_by_svc = _ensure_traces_loaded()

        # 合併所有來源的 traces 並去重
        seen: set[str] = set()
        all_traces: list[dict] = []
        for svc_traces in traces_by_svc.values():
            for trace in svc_traces:
                tid = trace.get("traceID")
                if tid and tid not in seen:
                    seen.add(tid)
                    all_traces.append(trace)

        trace = _find_trace_with_services(all_traces, {"frontend", "backend-a", "backend-b"})
        if trace:
            record(True, desc)
            return

        # 提供除錯資訊
        combos = []
        for t in all_traces[:10]:
            svcs = sorted(_get_service_names(t))
            if len(svcs) > 1:
                combos.append(str(svcs))
        hint = f"，找到的服務組合: {'; '.join(combos[:5])}" if combos else ""
        record(False, desc, f"沒有 trace 同時包含三個服務 (需先以瀏覽器操作前端){hint}")
    except Exception as e:
        record(False, desc, str(e))


def verify_trace_http_attributes() -> None:
    """12. Trace spans 包含 HTTP method 與 status code 屬性"""
    desc = "Trace spans 包含 HTTP method 與 status code 屬性"
    try:
        traces_by_svc = _ensure_traces_loaded()
        traces = traces_by_svc.get("backend-a", [])

        trace = _find_trace_with_services(traces, {"backend-a", "backend-b"})
        if not trace:
            record(False, desc, "找不到含 backend-a 與 backend-b 的 trace")
            return

        has_method = False
        has_status = False
        for span in trace.get("spans", []):
            tags = span.get("tags", [])
            if _get_tag_value(tags, "http.method") is not None or _get_tag_value(tags, "http.request.method") is not None:
                has_method = True
            if _get_tag_value(tags, "http.status_code") is not None or _get_tag_value(tags, "http.response.status_code") is not None:
                has_status = True

        if has_method and has_status:
            record(True, desc)
        else:
            missing = []
            if not has_method:
                missing.append("http.method / http.request.method")
            if not has_status:
                missing.append("http.status_code / http.response.status_code")
            record(False, desc, f"缺少屬性: {', '.join(missing)}")
    except Exception as e:
        record(False, desc, str(e))


def verify_trace_span_count() -> None:
    """13. trace 至少包含 3 個 spans 且具備 CLIENT 與 SERVER 類型"""
    desc = "backend-a → backend-b trace 至少 3 個 spans 且含 CLIENT 與 SERVER 類型"
    try:
        traces_by_svc = _ensure_traces_loaded()
        traces = traces_by_svc.get("backend-a", [])

        trace = _find_trace_with_services(traces, {"backend-a", "backend-b"})
        if not trace:
            record(False, desc, "找不到含 backend-a 與 backend-b 的 trace")
            return

        spans = trace.get("spans", [])
        span_count = len(spans)
        kinds: set[str] = set()
        for span in spans:
            kind = _get_tag_value(span.get("tags", []), "span.kind")
            if kind:
                kinds.add(kind)

        if span_count >= 3 and "client" in kinds and "server" in kinds:
            record(True, desc)
        elif span_count < 3:
            record(False, desc, f"僅有 {span_count} 個 spans，預期至少 3 個")
        else:
            record(False, desc, f"span.kind 不完整: 找到 {sorted(kinds)}，預期包含 client 與 server")
    except Exception as e:
        record(False, desc, str(e))


# ---------- 主程式 ----------

def main() -> None:
    print("=" * 60)
    print("OpenTelemetry Lab — 驗證腳本")
    print("=" * 60)
    print()

    checks = [
        verify_frontend_ui,
        verify_get_weather_api,
        verify_post_weather_api,
        verify_backend_a,
        verify_backend_b,
        verify_jaeger_ui,
        verify_seq_ui,
        verify_aspire_dashboard,
        verify_jaeger_services,
        verify_trace_backend_chain,
        verify_trace_frontend_chain,
        verify_trace_http_attributes,
        verify_trace_span_count,
    ]

    for check in checks:
        check()

    print()
    print("-" * 60)
    passed = sum(1 for ok, _ in results if ok)
    total = len(results)
    failed = total - passed
    print(f"結果：{passed}/{total} 通過，{failed} 失敗")

    sys.exit(0 if failed == 0 else 1)


if __name__ == "__main__":
    main()
