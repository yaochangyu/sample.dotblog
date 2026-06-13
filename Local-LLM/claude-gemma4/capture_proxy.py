#!/usr/bin/env python3
"""攔截 Claude Code 請求，記錄 system prompt 與 token 數，再轉發給 LiteLLM。"""
import json, http.server, urllib.request, urllib.error

UPSTREAM = "http://localhost:4000"
LOG_FILE = "/tmp/claude_code_requests.log"

class LoggingProxy(http.server.BaseHTTPRequestHandler):
    def do_POST(self):
        length = int(self.headers.get("Content-Length", 0))
        body = self.rfile.read(length)

        # 記錄請求
        try:
            data = json.loads(body)
            system = data.get("system", "")
            messages = data.get("messages", [])
            tools = data.get("tools", [])
            max_tokens = data.get("max_tokens")

            with open(LOG_FILE, "a") as f:
                f.write("=" * 60 + "\n")
                f.write(f"system_len: {len(system)} chars (~{len(system)//4} tokens)\n")
                f.write(f"tools: {[t['name'] for t in tools]}\n")
                f.write(f"messages: {len(messages)} turns\n")
                f.write(f"max_tokens: {max_tokens}\n")
                f.write(f"system_preview: {repr(system[:200])}\n")
                f.write("\n")
        except Exception as e:
            with open(LOG_FILE, "a") as f:
                f.write(f"parse error: {e}\n")

        # 轉發給 LiteLLM
        headers = {k: v for k, v in self.headers.items()
                   if k.lower() not in ("host", "content-length")}
        headers["Content-Length"] = str(len(body))

        req = urllib.request.Request(
            UPSTREAM + self.path, data=body,
            headers=headers, method="POST"
        )
        try:
            with urllib.request.urlopen(req) as resp:
                resp_body = resp.read()
                self.send_response(resp.status)
                for k, v in resp.getheaders():
                    self.send_header(k, v)
                self.end_headers()
                self.wfile.write(resp_body)
        except urllib.error.HTTPError as e:
            resp_body = e.read()
            self.send_response(e.code)
            self.end_headers()
            self.wfile.write(resp_body)

    def do_GET(self):
        req = urllib.request.Request(UPSTREAM + self.path, headers=dict(self.headers))
        try:
            with urllib.request.urlopen(req) as resp:
                resp_body = resp.read()
                self.send_response(resp.status)
                for k, v in resp.getheaders():
                    self.send_header(k, v)
                self.end_headers()
                self.wfile.write(resp_body)
        except Exception as e:
            self.send_response(500)
            self.end_headers()

    def log_message(self, fmt, *args):
        pass  # 靜默

if __name__ == "__main__":
    server = http.server.HTTPServer(("localhost", 4001), LoggingProxy)
    print("Logging proxy on :4001 → LiteLLM :4000")
    print(f"Requests logged to {LOG_FILE}")
    server.serve_forever()
