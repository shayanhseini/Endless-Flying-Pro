import http.server
import socketserver
import os

PORT = 8085
DIRECTORY = r"D:\clone-repos\Builds\WebGL"

class UnityWebGLHandler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=DIRECTORY, **kwargs)

    def end_headers(self):
        self.send_header("Access-Control-Allow-Origin", "*")
        path = self.translate_path(self.path)
        if path.endswith(".unityweb"):
            self.send_header("Content-Encoding", "br")
        super().end_headers()

    def guess_type(self, path):
        if path.endswith(".wasm.unityweb") or path.endswith(".wasm"):
            return "application/wasm"
        if path.endswith(".js.unityweb") or path.endswith(".js"):
            return "application/javascript"
        if path.endswith(".data.unityweb") or path.endswith(".data"):
            return "application/octet-stream"
        return super().guess_type(path)

class ThreadingHTTPServer(socketserver.ThreadingTCPServer):
    allow_reuse_address = True

if __name__ == "__main__":
    with ThreadingHTTPServer(("", PORT), UnityWebGLHandler) as httpd:
        print(f"Serving WebGL with multithreading at http://localhost:{PORT}")
        httpd.serve_forever()
