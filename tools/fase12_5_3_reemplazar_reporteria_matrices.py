from pathlib import Path
import base64
import zlib

payload_dir = Path(__file__).resolve().parent / 'fase12_5_3_payload'
payload = ''.join(path.read_text(encoding='utf-8').strip() for path in sorted(payload_dir.glob('part*.txt')))
source = zlib.decompress(base64.b64decode(payload)).decode('utf-8')
exec(compile(source, 'fase12_5_3_payload.py', 'exec'))
