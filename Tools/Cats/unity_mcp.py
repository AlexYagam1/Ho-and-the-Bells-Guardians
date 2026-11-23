"""Small stdio MCP client pinned to this Unity project; no global MCP configuration."""
import json
import pathlib
import subprocess
import sys

root = pathlib.Path(__file__).resolve().parents[2]
cli = pathlib.Path.home() / 'AppData/Local/Unity/bin/unity.exe'
process = subprocess.Popen([str(cli), 'mcp', '--project-path', str(root)],
                           stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                           stderr=subprocess.DEVNULL, text=True, encoding='utf-8')

def request(identifier, method, params):
    process.stdin.write(json.dumps({'jsonrpc': '2.0', 'id': identifier, 'method': method, 'params': params}) + '\n')
    process.stdin.flush()
    while True:
        line = process.stdout.readline()
        if not line:
            raise RuntimeError('Unity MCP closed unexpectedly')
        result = json.loads(line)
        if result.get('id') == identifier:
            if 'error' in result:
                raise RuntimeError(result['error'])
            return result['result']

try:
    request(1, 'initialize', {'protocolVersion': '2024-11-05', 'capabilities': {}, 'clientInfo': {'name': 'cat-npc-integration', 'version': '1'}})
    process.stdin.write(json.dumps({'jsonrpc': '2.0', 'method': 'notifications/initialized'}) + '\n')
    process.stdin.flush()
    if len(sys.argv) == 1:
        catalog = request(2, 'tools/list', {})
        print(json.dumps([t for t in catalog['tools'] if t['name'] in {'script_eval', 'script_run', 'console_status', 'editor_status', 'editor_playmode_start', 'editor_playmode_stop'}], ensure_ascii=False))
    else:
        args = json.loads(pathlib.Path(sys.argv[2]).read_text(encoding='utf-8-sig')) if len(sys.argv) > 2 else {}
        result = request(2, 'tools/call', {'name': sys.argv[1], 'arguments': args})
        print(json.dumps(result, ensure_ascii=False))
        if result.get('isError'):
            sys.exit(1)
finally:
    process.terminate()
    process.wait(timeout=10)
