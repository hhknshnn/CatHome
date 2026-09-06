import bpy
import math
import os
from mathutils import Vector


ROOT = r"C:\Users\HAKAN\Desktop\CatHome\CatHome"
FRAMES = ROOT + r"\Temp\CatRigQA\LowPolyRigify_v3\turntable_frames"


def look_at(camera, target=(0.0, 0.02, 0.83)):
    camera.rotation_euler = (Vector(target) - camera.location).to_track_quat('-Z', 'Y').to_euler()


def render():
    os.makedirs(FRAMES, exist_ok=True)
    for filename in os.listdir(FRAMES):
        if filename.startswith('frame_') and filename.lower().endswith('.png'):
            os.remove(os.path.join(FRAMES, filename))

    scene = bpy.context.scene
    camera = bpy.data.objects.get('CAM_ModelReview')
    if camera is None:
        raise RuntimeError('CAM_ModelReview bulunamadı')

    scene.frame_start = 1
    scene.frame_end = 48
    scene.frame_set(1)
    camera.animation_data_clear()
    radius = 4.85
    height = 2.02
    for frame, degrees in ((1, 35), (13, 125), (25, 215), (37, 305), (48, 395)):
        angle = math.radians(degrees)
        camera.location = (radius * math.sin(angle), radius * math.cos(angle), height)
        look_at(camera)
        camera.keyframe_insert('location', frame=frame)
        camera.keyframe_insert('rotation_euler', frame=frame)

    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = 512
    scene.render.resolution_y = 512
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGB'
    scene.render.filepath = FRAMES + r"\frame_"
    scene.render.fps = 24
    scene.frame_set(1)
    bpy.ops.render.render(animation=True)
    scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
    print({'frames': FRAMES, 'count': 48, 'fps': 24})


if __name__ == '__main__':
    render()
