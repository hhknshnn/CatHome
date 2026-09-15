"""Give the cat clearance beneath the upper bowl; preserve the lower drinking basin."""
import bpy
import json
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT))
import premium_kit

bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'PatioWaterFountain_Source.blend'))
metrics = []
for name in ('PatioWaterFountain', 'PatioWaterFountainWater'):
    obj = bpy.data.objects[name]
    mesh = obj.data
    base = mesh.attributes.get('CatHome_original_position')
    if base is None:
        base = mesh.attributes.new('CatHome_original_position', 'FLOAT_VECTOR', 'POINT')
        for vertex, item in zip(mesh.vertices, base.data):
            item.vector = vertex.co
    changed = 0
    lower_change = 0.0
    for vertex, item in zip(mesh.vertices, base.data):
        original = item.vector.copy()
        amount = max(0.0, min(1.0, (original.y - .35) / .10))
        factor = 1.0 - .55 * amount
        vertex.co = (original.x * factor, original.y, original.z * factor)
        changed += (vertex.co - original).length > 1e-7
        if original.y <= .35:
            lower_change = max(lower_change, (vertex.co-original).length)
    mesh.update()
    obj['CatHome_upper_bowl_scale'] = .45
    metrics.append(dict(object=name, changed=changed, lower_basin_max_change=lower_change,
                        vertices=len(mesh.vertices)))
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'PatioWaterFountain_Source.blend'))
premium_kit.export_fixture(bpy.data.objects['PatioWaterFountain'], 'PatioWaterFountain_Premium.fbx')
premium_kit.export_fixture(bpy.data.objects['PatioWaterFountainWater'], 'PatioWaterFountainWater_Premium.fbx')
print(json.dumps(metrics))
