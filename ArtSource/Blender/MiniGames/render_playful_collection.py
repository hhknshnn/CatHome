import bpy,os,json
from mathutils import Vector,Matrix
ROOT=os.path.dirname(os.path.abspath(__file__))
bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT,'PlayfulBonuses_Source.blend'))
OUT=os.path.join(ROOT,'PlayfulGenerated','Portraits');os.makedirs(OUT,exist_ok=True)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=40
scene.cycles.use_denoising=True;scene.render.resolution_x=640;scene.render.resolution_y=640;scene.render.resolution_percentage=100
scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
scene.world.color=(.4,.4,.4);scene.view_settings.view_transform='AgX'
camera=bpy.data.objects.new('CollectionCamera',bpy.data.cameras.new('CollectionCamera'));bpy.context.collection.objects.link(camera)
camera.data.type='ORTHO';camera.data.ortho_scale=1.28;scene.camera=camera
def aim(obj,point):obj.rotation_euler=(Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()
for name,position,power,size,color in [('Key',(-2.5,3,-4),450,3.8,(1,.94,.85)),('Fill',(3,1,-3),280,2.6,(.78,.9,1)),('Rim',(-1,2,2),340,2,(1,1,1))]:
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=color
 lamp=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(lamp);lamp.location=position;aim(lamp,(0,.25,0))
items=json.load(open(os.path.join(ROOT,'PlayfulGenerated','metrics.json')))
for item in items:
 for obj in bpy.data.objects:
  if obj.type=='MESH':obj.hide_render=True
 obj=bpy.data.objects[item['name']];obj.hide_render=False;obj.hide_set(False)
 center=sum((obj.matrix_world@Vector(c) for c in obj.bound_box),Vector())/8
 camera.location=center+Vector((1.0,.75,-3.6) if item['name']!='BonusScoreStar' else (.32,.25,-3.6))
 back=(camera.location-center).normalized();right=Vector((0,1,0)).cross(back).normalized();up=back.cross(right)
 camera.rotation_euler=Matrix((right,up,back)).transposed().to_euler()
 camera.data.ortho_scale=1.28 if not item['name'].startswith('Bonus') else .94
 scene.render.filepath=os.path.join(OUT,item['name']+'.png');bpy.ops.render.render(write_still=True)
 print('PORTRAIT',scene.render.filepath)
