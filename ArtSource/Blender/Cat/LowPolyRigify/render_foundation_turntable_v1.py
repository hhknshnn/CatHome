import bpy
import math
import os
from mathutils import Vector


ROOT = r"C:\Users\HAKAN\Desktop\CatHome\CatHome"
FRAMES = ROOT + r"\Temp\CatRigQA\LowPolyRigify_v1\turntable_v2_frames"


def point_camera(camera, target):
    camera.rotation_euler = (Vector(target) - camera.location).to_track_quat('-Z', 'Y').to_euler()


def render_turntable():
    os.makedirs(FRAMES, exist_ok=True)
    for filename in os.listdir(FRAMES):
        if filename.startswith('frame_') and filename.lower().endswith('.png'):
            os.remove(os.path.join(FRAMES, filename))
    scene = bpy.context.scene
    camera = bpy.data.objects.get('CAM_Preview')
    if camera is None:
        raise RuntimeError('CAM_Preview bulunamadı')

    target = (0.0, 0.05, 0.82)
    radius = 4.65
    height = 2.10
    scene.frame_start = 1
    scene.frame_end = 48
    camera.animation_data_clear()
    for frame, degrees in ((1, 36), (13, 126), (25, 216), (37, 306), (48, 396)):
        angle = math.radians(degrees)
        camera.location = (radius * math.sin(angle), radius * math.cos(angle), height)
        point_camera(camera, target)
        camera.keyframe_insert('location', frame=frame)
        camera.keyframe_insert('rotation_euler', frame=frame)

    action = camera.animation_data.action if camera.animation_data else None
    if action is not None and hasattr(action, 'fcurves'):
        for fcurve in action.fcurves:
            for point in fcurve.keyframe_points:
                point.interpolation = 'LINEAR'

    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = 512
    scene.render.resolution_y = 512
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.fps = 24
    scene.render.filepath = FRAMES + r"\frame_"
    scene.render.image_settings.color_mode = 'RGB'
    scene.render.film_transparent = False
    scene.frame_set(1)
    bpy.ops.render.render(animation=True)
    scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
    print({'turntable_frames': FRAMES, 'frames': 48, 'fps': 24})


if __name__ == '__main__':
    render_turntable()
