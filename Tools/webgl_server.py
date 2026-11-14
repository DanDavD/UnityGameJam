import http.server
import socketserver
import os
import sys

PORT = 8085
DIRECTORY = os.path.abspath("Builds/WebGL")

class UnityWebGLHandler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=DIRECTORY, **kwargs)

    def end_headers(self):
        self.send_header('Access-Control-Allow-Origin', '*')
        self.send_header('Cache-Control', 'no-store, no-cache, must-revalidate, max-age=0')
        self.send_header('Pragma', 'no-cache')
        self.send_header('Expires', '0')
        super().end_headers()

    def guess_type(self, path):
        if path.endswith('.data.unityweb'):
            return 'application/octet-stream'
        if path.endswith('.wasm.unityweb'):
            return 'application/wasm'
        if path.endswith('.js.unityweb') or path.endswith('.framework.js.unityweb'):
            return 'application/javascript'
        return super().guess_type(path)

    def send_head(self):
        path = self.translate_path(self.path)
        if path.endswith('.unityweb') and os.path.isfile(path):
            try:
                f = open(path, 'rb')
            except OSError:
                self.send_error(http.server.HTTPStatus.NOT_FOUND, "File not found")
                return None
            fs = os.fstat(f.fileno())
            self.send_response(http.server.HTTPStatus.OK)
            self.send_header('Content-Type', self.guess_type(path))
            self.send_header('Content-Encoding', 'gzip')
            self.send_header('Content-Length', str(fs[6]))
            self.send_header('Last-Modified', self.date_time_string(fs.st_mtime))
            self.end_headers()
            return f
        return super().send_head()

class ReusableTCPServer(socketserver.TCPServer):
    allow_reuse_address = True

if __name__ == '__main__':
    with ReusableTCPServer(("0.0.0.0", PORT), UnityWebGLHandler) as httpd:
        print(f"Serving Unity WebGL from {DIRECTORY} on http://localhost:{PORT}", flush=True)
        try:
            httpd.serve_forever()
        except KeyboardInterrupt:
            pass
