"""Cat Home: small sculpted UI objects, rendered natively with transparent pixels.
Run headless only. Geometry uses Blender Z-up; these are UI renders, not FBX.
"""
import bpy, math, random
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'Assets/Resources/PremiumInterface'
OUT.mkdir(parents=True,exist_ok=True)
random.seed(17)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)

def mat(name,hex,metal=0,rough=.3):
    m=bpy.data.materials.new(name);m.diffuse_color=(*[int(hex[i:i+2],16)/255 for i in (0,2,4)],1)
    m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=tuple(((c+.055)/1.055)**2.4 if c>.04045 else c/12.92 for c in m.diffuse_color[:3])+(1,)
    p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    return m
cream=mat('Pearl enamel','FFF4D9');gold=mat('Champagne hardware','E8B348',.55,.24)
teal=mat('Turquoise enamel','218F87',.15,.26);coral=mat('Coral cushion','F5786C',0,.4)
mint=mat('Mint upholstery','A6D7BC',0,.46);ink=mat('Dark inset','244A4B',0,.38)
lilac=mat('Lilac moon','A692D0',.1,.28);water=mat('Aqua water','26BFD7',.2,.2)
white=mat('Specular inlay','FFFDF4');food=mat('Terracotta bowl','DB792D',.1,.28)
kibble=mat('Golden kibble','A95B25',0,.58)
objects=[]
def finish(o,name,m):
    o.name=name;o.data.materials.append(m);objects.append(o);return o
def cube(name,pos,scale,m,bevel=.1):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=bpy.context.object;o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    b=o.modifiers.new('Soft manufactured edges','BEVEL');b.width=bevel;b.segments=5
    o.modifiers.new('Weighted surface normals','WEIGHTED_NORMAL');return finish(o,name,m)
def sphere(name,pos,scale,m):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=40,ring_count=24,location=pos);o=bpy.context.object;o.scale=scale
    for p in o.data.polygons:p.use_smooth=True
    return finish(o,name,m)
def tube(name,pts,r,m,closed=False):
    c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.bevel_depth=r;c.bevel_resolution=5
    s=c.splines.new('POLY');s.points.add(len(pts)-1)
    for p,v in zip(s.points,pts):p.co=(*v,1)
    s.use_cyclic_u=closed;o=bpy.data.objects.new(name,c);bpy.context.collection.objects.link(o);return finish(o,name,m)
def disc(name,pos,r,depth,m):
    bpy.ops.mesh.primitive_cylinder_add(vertices=80,radius=r,depth=depth,location=pos);o=bpy.context.object
    b=o.modifiers.new('Rolled edge','BEVEL');b.width=.025;b.segments=3
    o.modifiers.new('Weighted normals','WEIGHTED_NORMAL');return finish(o,name,m)
def paw(x,y,z,size=1,material=cream):
    sphere('Paw pad',(x,y,z),(size*.19,size*.035,size*.15),material)
    for dx,dz,ang in [(-.22,.21,-.3),(-.085,.30,-.12),(.085,.30,.12),(.22,.21,.3)]:
        o=sphere('Paw toe',(x+dx*size,y,z+dz*size),(size*.07,size*.038,size*.09),material);o.rotation_euler[1]=ang
def profile(name,pts,depth,m):
    c=bpy.data.curves.new(name,'CURVE');c.dimensions='2D';c.resolution_u=24;c.fill_mode='BOTH';c.extrude=depth/2;c.bevel_depth=.045;c.bevel_resolution=5
    s=c.splines.new('POLY');s.points.add(len(pts)-1)
    for p,v in zip(s.points,pts):p.co=(v[0],v[1],0,1)
    s.use_cyclic_u=True;o=bpy.data.objects.new(name,c);bpy.context.collection.objects.link(o);o.rotation_euler[0]=math.pi/2
    return finish(o,name,m)
def bag():
    cube('Tote gold foot',(0,0,.20),(1.33,.5,.16),gold,.07)
    cube('Tote sculpted body',(0,0,.84),(1.35,.5,1.26),teal,.14)
    cube('Tote upper welt',(0,0,1.43),(1.35,.5,.10),gold,.04)
    for y in [-.21,.21]:
        tube('Round carry handle',[(.34*math.cos(t),y,1.39+.52*math.sin(t)) for t in [i*math.pi/48 for i in range(49)]],.055,gold)
        for x in [-.34,.34]:sphere('Handle rivet',(x,y*1.3,1.38),(.08,.035,.07),gold)
    paw(0,-.27,.72,.95)
def sofa():
    for x in [-.66,.66]:
        for y in [-.30,.30]:cube('Gold sofa foot',(x,y,.16),(.12,.12,.25),gold,.03)
    cube('Sofa ivory base',(0,0,.40),(1.65,.80,.3),cream,.14)
    cube('Sofa back',(0,.28,.94),(1.54,.22,.9),cream,.12)
    for x in [-.37,.37]:
        cube('Coral back cushion',(x,.10,.99),(.71,.28,.66),coral,.11)
        cube('Coral seat cushion',(x,-.10,.61),(.71,.54,.20),coral,.085)
        sphere('Upholstery button',(x,-.055,1.06),(.025,.015,.025),gold)
    for x in [-.76,.76]:cube('Sofa rolled arm',(x,0,.70),(.25,.77,.40),cream,.12)
    paw(0,-.416,.33,.20,gold)
def gamepad():
    cube('Controller center',(0,0,.91),(1.53,.50,.65),cream,.28)
    for x,rot in [(-.61,-.32),(.61,.32)]:
        o=cube('Ergonomic grip',(x,0,.58),(.45,.48,.78),cream,.20);o.rotation_euler[1]=rot
    cube('Directional vertical',(-.46,-.275,1.0),(.12,.09,.40),teal,.025)
    cube('Directional horizontal',(-.46,-.276,1.0),(.40,.09,.12),teal,.025)
    for x,z,m in [(0,0,coral),(.13,.13,gold),(-.13,.13,water),(0,.26,lilac)]:sphere('Game button',(.46+x,-.29,.85+z),(.078,.055,.078),m)
    for x in [-.24,.24]:sphere('Thumb stick',(x,-.285,.72),(.105,.07,.105),ink)
    paw(0,-.27,1.06,.30,gold)
def bowl():
    # Lathed closed bowl with a real hollow inset and generous rolled lip.
    disc('Bowl foot',(0,0,.14),.61,.16,cream)
    bpy.ops.mesh.primitive_cone_add(vertices=80,radius1=.63,radius2=.49,depth=.51,location=(0,0,.40));finish(bpy.context.object,'Tapered food bowl',food)
    disc('Food dark recess',(0,0,.654),.47,.02,kibble)
    tube('Ivory rim',[(.52*math.cos(t),.52*math.sin(t),.68) for t in [i*math.tau/96 for i in range(96)]],.075,cream,True)
    for i in range(27):
        a=random.random()*math.tau;r=.39*math.sqrt(random.random());sphere('Individual kibble',(r*math.cos(a),r*math.sin(a),.69+random.random()*.035),(.067,.049,.04),gold if i%4==0 else kibble)
    paw(0,-.58,.36,.43,cream)
def drop():
    pts=[]
    for i in range(81):
        a=math.tau*i/80
        pts.append((.53*math.sin(a)*(1-.45*math.cos(a)),.91+.79*math.cos(a)))
    profile('Polished water droplet',pts,.25,water)
    tube('Enamel highlight',[(-.22,-.205,.65),(-.29,-.205,.77),(-.28,-.205,.94)],.035,white)
def moon():
    # Crescent outline: exterior arc and inset arc sharing both tips.
    pts=[(.70*math.cos(t),.99+.70*math.sin(t)) for t in [math.radians(65+i*230/90) for i in range(91)]]
    bottom=pts[-1];top=pts[0]
    pts += [(bottom[0]-.53*math.sin(math.pi*i/65),bottom[1]+(top[1]-bottom[1])*i/65) for i in range(1,66)]
    profile('Pearl lilac crescent',pts,.22,lilac)
    for x,z,size in [(.63,1.36,.13),(.85,.87,.09)]:
        star=[(x+size*(1 if i%2==0 else .28)*math.cos(math.pi/2+i*math.pi/4),z+size*(1 if i%2==0 else .28)*math.sin(math.pi/2+i*math.pi/4)) for i in range(8)]
        profile('Gold star',star,.08,gold)
def paw_icon():
    paw(0,-.04,.7,2.1,gold)

scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=scene.render.resolution_y=768;scene.render.resolution_percentage=100
scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
scene.world.color=(.3,.3,.3);scene.view_settings.view_transform='AgX'
def light(name,pos,power,size,color):
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=color
    o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,.9))-o.location).to_track_quat('-Z','Y').to_euler()
light('Large silk key',(-3,-4,6),550,4,(1,.91,.78));light('Soft mint fill',(4,-2,3),340,3,(.83,.95,1));light('Warm rim',(1,4,5),700,3,(1,.86,.62))
camera_data=bpy.data.cameras.new('Icon camera');camera=bpy.data.objects.new('Icon camera',camera_data);scene.collection.objects.link(camera);scene.camera=camera
camera_data.type='ORTHO';camera_data.ortho_scale=2.35
for name,build in [('Shop',bag),('Rooms',sofa),('Games',gamepad),('Food',bowl),('Water',drop),('Energy',moon),('Paw',paw_icon)]:
    for o in objects:bpy.data.objects.remove(o,do_unlink=True)
    objects.clear();build()
    camera.location=(2.0,-7,3.25) if name not in ['Water','Energy','Paw'] else (1.2,-7,1.8)
    target=Vector((0,0,.97 if name!='Food' else .40));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    # Frame the evaluated silhouette in CAMERA space. A shared world-space
    # target left the controller at the bottom and clipped the moon's stars.
    bpy.context.view_layer.update()
    graph=bpy.context.evaluated_depsgraph_get()
    inverse=camera.matrix_world.inverted()
    corners=[]
    for o in objects:
        evaluated=o.evaluated_get(graph)
        mesh=evaluated.to_mesh()
        if mesh is not None:
            corners.extend(inverse @ evaluated.matrix_world @ v.co for v in mesh.vertices)
            evaluated.to_mesh_clear()
    left,right=min(v.x for v in corners),max(v.x for v in corners)
    bottom,top=min(v.y for v in corners),max(v.y for v in corners)
    camera.location += camera.rotation_euler.to_matrix() @ Vector(((left+right)/2,(bottom+top)/2,0))
    camera_data.ortho_scale=max(right-left,top-bottom)*1.16
    scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
    print('CAT_HOME_ICON_READY '+name,flush=True)
bpy.ops.wm.save_as_mainfile(filepath=str(Path(__file__).with_name('InterfaceIcons_Source.blend')))
