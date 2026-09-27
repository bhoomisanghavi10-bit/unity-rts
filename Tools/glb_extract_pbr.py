#!/usr/bin/env python3
"""Extract a Meshy glb's PBR textures for LowpolyBuildingImporter.

usage: glb_extract_pbr.py <in.glb> <out_folder> <name> [size=2048]
Writes <name>.glb (copy), <name>_albedo.png, <name>_normal.png and
<name>_metallicSmoothness.png (glTF metallicRoughness -> Unity: metallic=B,
smoothness=1-roughness(G) in alpha).
"""
import io, json, shutil, struct, sys, os
from PIL import Image

src, out, name = sys.argv[1:4]
size = int(sys.argv[4]) if len(sys.argv) > 4 else 2048
os.makedirs(out, exist_ok=True)
with open(src, 'rb') as f:
    f.read(12)
    n, _ = struct.unpack('<II', f.read(8))
    j = json.loads(f.read(n))
    f.read(8)
    blob = f.read()

def img(tex):
    bv = j['bufferViews'][j['images'][j['textures'][tex['index']]['source']]['bufferView']]
    return Image.open(io.BytesIO(blob[bv['byteOffset']:bv['byteOffset'] + bv['byteLength']]))

m = j['materials'][0]
pbr = m['pbrMetallicRoughness']
rs = lambda i: i.resize((size, size), Image.LANCZOS) if i.size[0] > size else i
rs(img(pbr['baseColorTexture']).convert('RGB')).save(f'{out}/{name}_albedo.png')
if 'normalTexture' in m:
    rs(img(m['normalTexture']).convert('RGB')).save(f'{out}/{name}_normal.png')
if 'metallicRoughnessTexture' in pbr:
    mr = rs(img(pbr['metallicRoughnessTexture']).convert('RGB'))
    _, g, b = mr.split()
    Image.merge('RGBA', (b, b, b, g.point(lambda v: 255 - v))).save(f'{out}/{name}_metallicSmoothness.png')
shutil.copy(src, f'{out}/{name}.glb')
print(name, 'ok', {k: os.path.getsize(f'{out}/{k}') // 1024 for k in os.listdir(out) if k.startswith(name)})
