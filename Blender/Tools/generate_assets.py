"""RIVE404 original low-poly asset kit. Run with Blender 4.3+: blender -b --python thisfile.
One Blender metre = one Unity unit. Models face Blender -Y (=Unity +Z).
No images, fonts, external geometry or copyrighted character designs are used.
"""
from pathlib import Path
import bpy, math, json
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'UnityProject/Assets/_RIVE404/Resources/Models'
OUT.mkdir(parents=True, exist_ok=True)
REPORT = []
bpy.context.preferences.filepaths.save_version=0

PALETTE = {
    'Ink': ((.026,.041,.064,1),0,.8), 'Track': ((.061,.099,.135,1),0,.8),
    'Red': ((.62,.065,.073,1),0,.67), 'Scarlet': ((.89,.16,.11,1),0,.67),
    'Green': ((.055,.37,.21,1),0,.8), 'Ginger': ((.64,.21,.055,1),0,.8),
    'HairLight': ((.87,.35,.09,1),0,.8), 'Skin': ((.73,.44,.28,1),0,.8),
    'SkinDark': ((.32,.16,.10,1),0,.8), 'SkinLight': ((.91,.68,.45,1),0,.8),
    'Teal': ((.035,.65,.68,1),.3,.4), 'Blue': ((.067,.22,.36,1),0,.7),
    'Cream': ((.86,.75,.57,1),0,.8), 'Plaster': ((.73,.54,.37,1),0,.86),
    'Concrete': ((.28,.36,.39,1),0,.9), 'Roof': ((.45,.11,.075,1),0,.9),
    'Wood': ((.34,.17,.078,1),0,.9), 'WoodLight': ((.58,.32,.14,1),0,.8),
    'Metal': ((.33,.45,.51,1),.75,.3), 'Gold': ((.88,.56,.16,1),.7,.35),
    'Leaf': ((.075,.32,.22,1),0,1), 'LeafLight': ((.20,.48,.27,1),0,1),
    'Glass': ((.05,.22,.28,1),.25,.18), 'Glow': ((.08,.96,.81,1),.1,.3),
    'Warning': ((1,.38,.05,1),.2,.5), 'Purple': ((.38,.105,.47,1),.2,.7),
}

def material(name):
    m = bpy.data.materials.get('MAT_'+name)
    if m: return m
    c, metallic, rough = PALETTE[name]
    m=bpy.data.materials.new('MAT_'+name); m.diffuse_color=c; m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=c
    bs.inputs['Metallic'].default_value=metallic; bs.inputs['Roughness'].default_value=rough
    if name in ('Glow','Warning'):
        bs.inputs['Emission Color'].default_value=c
        bs.inputs['Emission Strength'].default_value=1.5
    return m

def finish(obj,name,mat,bone=None):
    obj.name=name; obj.data.name=name+'_Mesh'; obj.data.materials.append(material(mat))
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bone: obj['bind_bone']=bone
    return obj

def box(name,pos,size,mat,rot=None,bone=None):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos)
    obj=bpy.context.object; obj.dimensions=size
    if rot: obj.rotation_euler=rot
    return finish(obj,name,mat,bone)

def ellipsoid(name,pos,scale,mat,bone=None,segments=12,rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,radius=1,location=pos)
    obj=bpy.context.object; obj.scale=scale
    return finish(obj,name,mat,bone)

def ico(name,pos,scale,mat,sub=1,bone=None):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=pos)
    obj=bpy.context.object; obj.scale=scale
    return finish(obj,name,mat,bone)

def rod(name,a,b,r,mat,vertices=8,bone=None,r2=None):
    mid=(Vector(a)+Vector(b))*.5; length=(Vector(b)-Vector(a)).length
    if r2 is None: bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=length,location=mid)
    else: bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r,radius2=r2,depth=length,location=mid)
    obj=bpy.context.object; obj.rotation_euler=(Vector(b)-Vector(a)).to_track_quat('Z','Y').to_euler()
    return finish(obj,name,mat,bone)

def mesh(name,verts,faces,mat,bone=None):
    data=bpy.data.meshes.new(name+'_Mesh'); data.from_pydata(verts,[],faces); data.update()
    obj=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(obj)
    return finish(obj,name,mat,bone)

def start(name):
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    for action in list(bpy.data.actions): bpy.data.actions.remove(action)
    bpy.context.scene.unit_settings.system='METRIC'; bpy.context.scene.unit_settings.scale_length=1
    bpy.context.scene.render.fps=24; bpy.context.scene.frame_start=1; bpy.context.scene.frame_end=48
    bpy.context.scene['Asset']=name

def save(name,category):
    # FBX contains model geometry and (if present) deform armature, never cameras/lights.
    objects=list(bpy.context.scene.objects)
    # Keep editable source pieces; export a merged renderer with material slots.
    # Character weights and the Armature modifier survive joining the copies.
    bpy.ops.object.select_all(action='DESELECT')
    copied=[]
    for original in objects:
        if original.type!='MESH': continue
        clone=original.copy(); clone.data=original.data.copy()
        bpy.context.collection.objects.link(clone); clone.select_set(True); copied.append(clone)
    bpy.context.view_layer.objects.active=copied[0]
    bpy.ops.object.join()
    combined=copied[0]; combined.name=name+'_RenderMesh'
    for obj in objects:
        if obj.type=='ARMATURE': obj.select_set(True)
    fbx=OUT/(name+'.fbx')
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,global_scale=1,
        apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
        axis_forward='-Z',axis_up='Y',object_types={'MESH','ARMATURE'},
        use_mesh_modifiers=True,mesh_smooth_type='FACE',add_leaf_bones=False,
        bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True,bake_anim_simplify_factor=0,
        path_mode='AUTO',embed_textures=False)
    bpy.data.objects.remove(combined,do_unlink=True)
    source=ROOT/'Blender'/category/(name+'.blend')
    source.parent.mkdir(parents=True,exist_ok=True)
    bpy.context.scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    meshes=[o for o in objects if o.type=='MESH']
    REPORT.append({'model':name,'meshes':len(meshes),'vertices':sum(len(o.data.vertices) for o in meshes),
        'polygons':sum(len(o.data.polygons) for o in meshes),'bones':sum(len(o.data.bones) for o in objects if o.type=='ARMATURE'),
        'actions':[a.name for a in bpy.data.actions], 'export_meshes':1,'fbx_bytes':fbx.stat().st_size,
        'blend_bytes':source.stat().st_size})

def rig(name):
    bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0))
    obj=bpy.context.object; obj.name='RIG_'+name
    arm=obj.data; arm.name='RIG_'+name; arm.edit_bones.remove(arm.edit_bones[0])
    def bone(n,h,t,p=None):
        b=arm.edit_bones.new(n); b.head=h; b.tail=t
        if p: b.parent=arm.edit_bones[p]
    bone('Root',(0,0,0),(0,0,.18))
    bone('Hips',(0,0,.91),(0,0,1.08),'Root')
    bone('Spine',(0,0,1.08),(0,0,1.39),'Hips')
    bone('Head',(0,0,1.39),(0,0,1.82),'Spine')
    for side,sign in [('L',1),('R',-1)]:
        bone('Arm.'+side,(sign*.26,0,1.34),(sign*.33,0,1.08),'Spine')
        bone('Forearm.'+side,(sign*.33,0,1.08),(sign*.35,0,.86),'Arm.'+side)
        bone('Thigh.'+side,(sign*.13,0,1.01),(sign*.13,0,.55),'Hips')
        bone('Shin.'+side,(sign*.13,0,.55),(sign*.13,0,.14),'Thigh.'+side)
        bone('Foot.'+side,(sign*.13,0,.14),(sign*.13,-.20,.09),'Shin.'+side)
    bpy.ops.object.mode_set(mode='OBJECT'); obj.show_in_front=True
    for m in list(bpy.context.scene.objects):
        if m.type!='MESH': continue
        namebone=m.get('bind_bone','Spine')
        m.parent=obj
        group=m.vertex_groups.new(name=namebone); group.add(list(range(len(m.data.vertices))),1,'REPLACE')
        mod=m.modifiers.new('Deformation','ARMATURE'); mod.object=obj
    obj.animation_data_create()
    for actionname,frames in [('Idle',[1,12,24,36,48]),('Walk',[1,7,13,19,25])]:
        act=bpy.data.actions.new(name+'_'+actionname); act.use_fake_user=True; obj.animation_data.action=act
        for f in frames:
            phase=(f-1)/(47 if actionname=='Idle' else 24)*2*math.pi
            for pb in obj.pose.bones:
                pb.rotation_mode='XYZ'; pb.rotation_euler=(0,0,0)
                if actionname=='Idle':
                    if pb.name=='Spine': pb.rotation_euler.x=math.sin(phase)*.018
                    if pb.name=='Head': pb.rotation_euler.z=math.sin(phase)*.025
                    if pb.name.startswith('Arm.'): pb.rotation_euler.x=math.sin(phase)*.022
                else:
                    sign=1 if pb.name.endswith('.L') else -1
                    if pb.name.startswith('Thigh.'): pb.rotation_euler.x=sign*math.sin(phase)*.50
                    if pb.name.startswith('Shin.'): pb.rotation_euler.x=max(0,-sign*math.sin(phase))*.52
                    if pb.name.startswith('Arm.'): pb.rotation_euler.x=-sign*math.sin(phase)*.36
                    if pb.name.startswith('Forearm.'): pb.rotation_euler.x=.10
                pb.keyframe_insert(data_path='rotation_euler',frame=f,group=pb.name)
        for fc in act.fcurves:
            for k in fc.keyframe_points: k.interpolation='LINEAR'
    obj.animation_data.action=bpy.data.actions.get(name+'_Idle')
    bpy.context.scene.frame_set(1)

def human(name,sillage=False,npc=False):
    start(name)
    skin='SkinDark' if npc else 'Skin'; hair='Ink' if npc else 'Ginger'
    cloth='Blue' if npc else ('Cream' if sillage else 'Track')
    ellipsoid('CHR_'+name+'_Torso',(0,0,1.20),(.25,.145,.27),cloth,'Spine')
    box('Waist',(0,0,1.0),(.37,.23,.13),'Ink',bone='Hips')
    rod('Neck',(0,0,1.36),(0,0,1.50),.078,skin,bone='Head')
    ellipsoid('Face',(0,-.005,1.63),(.155,.132,.19),skin,'Head')
    ellipsoid('Hair',(0,.025,1.77),(.159,.139,.105),hair,'Head')
    for x,z in [(-.11,1.81),(-.04,1.84),(.055,1.83),(.13,1.79)]:
        ico('Hair_Tuft',(x,-.016,z),(.065,.115,.048),'HairLight' if not npc else 'Ink',bone='Head')
    for x in (-.159,.159): ellipsoid('Ear',(x,0,1.64),(.026,.028,.046),skin,'Head',segments=8,rings=4)
    rod('Nose',(0,-.115,1.65),(0,-.166,1.62),.028,skin,bone='Head',vertices=6)
    if not npc:
        ellipsoid('Beard',(0,-.054,1.52),(.128,.102,.074),'Ginger','Head',segments=10,rings=4)
        box('Mouth',(0,-.148,1.55),(.054,.013,.014),'Ink',bone='Head')
    for side,sign in [('L',1),('R',-1)]:
        x=sign*.13
        rod('Trouser_'+side,(x,0,.60),(x,0,1.0),.10,'Ink',bone='Thigh.'+side,r2=.11)
        rod('Trouser_Calf_'+side,(x,0,.18),(x,0,.58),.079,'Ink',bone='Shin.'+side,r2=.10)
        box('Shoe_'+side,(x,-.069,.105),(.18,.32,.15),'Ink',bone='Foot.'+side)
        box('Sole_'+side,(x,-.075,.038),(.19,.34,.044),'Concrete',bone='Foot.'+side)
        box('Shoe_Accent_'+side,(x,-.175,.125),(.12,.05,.032),'Red' if side=='L' else 'Green',bone='Foot.'+side)
        rod('Sleeve_'+side,(sign*.30,0,1.14),(sign*.25,0,1.35),.093,cloth,bone='Arm.'+side)
        rod('Forearm_'+side,(sign*.35,0,.91),(sign*.31,0,1.15),.075,cloth,bone='Forearm.'+side,r2=.084)
        ellipsoid('Hand_'+side,(sign*.35,-.006,.875),(.064,.05,.08),skin,'Forearm.'+side,segments=8,rings=4)
    if npc:
        # Dock worker's cap and utility vest, distinct from Nico and Sillage.
        rod('Cap',(0,0,1.77),(0,0,1.83),.16,'Warning',vertices=12,bone='Head')
        box('Cap_Visor',(0,-.145,1.78),(.25,.13,.025),'Warning',bone='Head')
        box('Vest_L',(-.135,-.139,1.21),(.12,.025,.30),'WoodLight',bone='Spine')
        box('Vest_R',(.135,-.139,1.21),(.12,.025,.30),'WoodLight',bone='Spine')
        for x in (-.069,.069): box('Eyes',(x,-.134,1.67),(.025,.012,.018),'Ink',bone='Head')
    elif sillage:
        # Original asymmetrical officer coat, rectangular clasp, wave-shaped cape.
        box('Coat_L',(-.17,-.077,1.23),(.15,.145,.32),'Blue',bone='Spine')
        box('Coat_R',(.17,-.077,1.23),(.15,.145,.32),'Blue',bone='Spine')
        rod('Belt',(0,0,1.05),(0,0,1.09),.242,'Wood',vertices=12,bone='Hips')
        box('Buckle',(0,-.153,1.07),(.06,.018,.06),'Gold',bone='Hips')
        mesh('Scarlet_Cape',[(-.26,.13,1.39),(.26,.13,1.39),(-.38,.25,.48),(.38,.25,.48),(-.23,.40,.57),(.23,.40,.57)],
            [(0,1,3,2),(2,3,5,4),(0,2,4),(1,5,3)],'Red','Spine')
        rod('Cape_Clasp',(-.10,-.157,1.36),(.10,-.157,1.36),.017,'Gold',bone='Spine')
        # Held compact original sword, later combat visuals may articulate the forearm.
        rod('Sword_Handle',(.35,-.02,.86),(.35,-.02,.69),.026,'Wood',bone='Forearm.L')
        box('Sword_Guard',(.35,-.02,.67),(.22,.055,.035),'Gold',bone='Forearm.L')
        mesh('Sword_Blade',[(.31,-.035,.65),(.39,-.035,.65),(.31,-.005,.65),(.39,-.005,.65),(.35,-.02,.02)],
            [(0,1,4),(1,3,4),(3,2,4),(2,0,4),(0,2,3,1)],'Metal','Forearm.L')
        for x in (-.068,.068): box('Eyes',(x,-.135,1.67),(.027,.012,.015),'Ink',bone='Head')
        box('Scar',(-.08,-.14,1.64),(.035,.007,.006),'Red',bone='Head')
    else:
        box('Track_Zipper',(0,-.149,1.22),(.017,.015,.34),'Cream',bone='Spine')
        box('Track_Accent_L',(-.115,-.135,1.26),(.075,.025,.21),'Red',bone='Spine')
        box('Track_Accent_R',(.115,-.135,1.26),(.075,.025,.21),'Green',bone='Spine')
        for x in (-.077,.077):
            box('Lens',(x,-.140,1.675),(.10,.012,.058),'Glass',bone='Head')
            for dz in (-.030,.030): box('Glasses_Frame',(x,-.151,1.675+dz),(.115,.015,.010),'Ink',bone='Head')
            for dx in (-.056,.056): box('Glasses_Frame',(x+dx,-.151,1.675),(.010,.015,.063),'Ink',bone='Head')
        box('Glasses_Bridge',(0,-.151,1.675),(.04,.015,.01),'Ink',bone='Head')
        box('Wrist_Tool',(.36,-.085,1.015),(.10,.046,.11),'Teal',bone='Forearm.L')
        box('Wrist_Screen',(.36,-.111,1.02),(.065,.012,.065),'Glow',bone='Forearm.L')
    rig(name)
    save(name,'Characters')

def drone():
    start('Drone')
    ico('PFB_Enemy_Drone',(0,0,.47),(.35,.27,.19),'Ink',sub=2)
    box('Shell',(0,0,.51),(.43,.35,.11),'Metal')
    ico('Sensor',(0,-.27,.48),(.11,.035,.065),'Warning',sub=2)
    for x in (-.39,.39):
        for y in (-.33,.33):
            rod('Rotor_Arm',(0,0,.49),(x,y,.49),.035,'Metal')
            rod('Rotor_Ring',(x,y,.50),(x,y,.56),.19,'Ink',vertices=12)
            box('Rotor_Blade',(x,y,.572),(.38,.025,.012),'Concrete')
            box('Rotor_Blade',(x,y,.572),(.025,.38,.012),'Concrete')
    for x in (-.16,.16): rod('Landing',(x,0,.37),(x,0,.10),.025,'Metal')
    save('Drone','Characters')

def robot(name,boss=False):
    start(name); s=1.65 if boss else 1.0
    box('PFB_Enemy_'+name,(0,0,1.08),(.51,.30,.51),'Ink')
    box('Armour_Chest',(0,-.19,1.11),(.46,.08,.30),'Purple' if boss else 'Metal')
    ico('Core',(0,-.245,1.17),(.10,.031,.10),'Warning',sub=2)
    box('Head',(0,0,1.55),(.31,.29,.28),'Metal')
    box('Sensor_Visor',(0,-.154,1.56),(.26,.024,.071),'Warning')
    rod('Neck',(0,0,1.29),(0,0,1.46),.07,'Ink')
    box('Waist',(0,0,.79),(.25,.24,.14),'Metal')
    for sign in (-1,1):
        x=sign*.17
        rod('Thigh',(x,0,.47),(x,0,.77),.086,'Metal')
        ico('Knee',(x,0,.43),(.10,.09,.085),'Ink')
        box('Calf',(x,.025,.26),(.16,.19,.25),'Metal')
        box('Foot',(x,-.079,.08),(.23,.34,.14),'Ink')
        ico('Shoulder',(sign*.36,0,1.27),(.12,.13,.12),'Metal')
        rod('Arm',(sign*.41,0,1.02),(sign*.36,0,1.25),.09,'Ink')
        box('Forearm',(sign*.44,-.02,.91),(.15,.20,.21),'Metal')
        box('Hand',(sign*.44,-.02,.76),(.11,.14,.10),'Ink')
        if boss:
            mesh('Crown_Shard',[(sign*.12,0,1.68),(sign*.24,0,1.68),(sign*.27,0,1.96),
                (sign*.12,.08,1.68),(sign*.24,.08,1.68),(sign*.27,.08,1.96)],
                [(0,1,2),(3,5,4),(0,3,4,1),(1,4,5,2),(0,2,5,3)],'Gold')
            box('Mantle',(sign*.33,.09,1.32),(.29,.45,.14),'Purple')
    if boss:
        # Scale mesh data, retaining clean unit transform and floor pivot.
        for obj in bpy.context.scene.objects:
            obj.location*=s
            for v in obj.data.vertices: v.co*=s
        rod('Archive_Antenna',(0,.12,2.53),(0,.12,3.23),.023,'Glow')
    save(name,'Characters')

def corrupted():
    human('Corrupted',npc=True)
    # Change the saved template to a corrupted geometric citizen without copying other sources.
    for obj in bpy.context.scene.objects:
        if obj.type=='MESH' and obj.data.materials:
            if obj.data.materials[0].name in ('MAT_Blue','MAT_WoodLight','MAT_Warning'):
                obj.data.materials[0]=material('Purple')
    ico('Anomaly_Shoulder',(.28,-.01,1.40),(.13,.18,.13),'Glow',bone='Spine')
    box('Anomaly_Face',(0,-.15,1.67),(.14,.022,.025),'Glow',bone='Head')
    for obj in bpy.context.scene.objects:
        if obj.type=='MESH' and not obj.parent:
            arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
            obj.parent=arm; group=obj.vertex_groups.new(name=obj.get('bind_bone','Spine'))
            group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
            obj.modifiers.new('Deformation','ARMATURE').object=arm
    REPORT.pop(); save('Corrupted','Characters')

def roof(name,w,d,z,mat='Roof'):
    mesh(name,[(-w/2,-d/2,z),(w/2,-d/2,z),(-w/2,d/2,z),(w/2,d/2,z),
        (0,-d/2,z+1.6),(0,d/2,z+1.6)],
        [(0,1,4),(2,5,3),(0,4,5,2),(1,3,5,4),(0,2,3,1)],mat)

def window(x,y,z,w=1,h=1):
    box('Window',(x,y,z),(w,.09,h),'Glass')
    for dx in (-w/2,w/2): box('Window_Frame',(x+dx,y-.025,z),(.065,.13,h+.10),'Cream')
    for dz in (-h/2,h/2): box('Window_Frame',(x,y-.025,z+dz),(w,.13,.065),'Cream')
    box('Window_Cross',(x,y-.075,z),(.035,.05,h),'Cream')

def house():
    start('House')
    # Open-backed modular facade, doorway remains physically clear for runtime interiors.
    box('SM_Port_House_Left',(-2.0,0,1.6),(2.0,6,3.2),'Plaster')
    box('SM_Port_House_Right',(2.0,0,1.6),(2.0,6,3.2),'Plaster')
    box('SM_Port_House_Lintel',(0,0,2.82),(2,6,.76),'Plaster')
    box('Floor',(0,0,.06),(6.1,6.1,.12),'Concrete')
    roof('SM_Port_Roof',6.6,6.6,3.2)
    for x in (-2,2):
        window(x,-3.04,1.8,1.1,1.3)
        box('Shutter',(x-.68,-3.07,1.8),(.25,.15,1.40),'Teal')
        box('Shutter',(x+.68,-3.07,1.8),(.25,.15,1.40),'Teal')
    for x in (-1.03,1.03): box('Doorframe',(x,-3.10,1.17),(.14,.20,2.35),'Wood')
    box('Doorframe',(0,-3.10,2.38),(2.20,.20,.14),'Wood')
    box('Awning',(0,-3.45,2.54),(2.48,1.12,.12),'Red',rot=(.13,0,0))
    box('Chimney',(1.6,1.4,4.45),(.50,.50,1.5),'Concrete')
    rod('House_Light',(-1.39,-3.25,2.21),(-1.39,-3.25,2.45),.07,'Gold')
    save('House','Buildings')

def warehouse():
    start('Warehouse')
    box('SM_Depot_WallL',(-4.75,0,2.5),(.50,10,5),'Concrete')
    box('SM_Depot_WallR',(4.75,0,2.5),(.50,10,5),'Concrete')
    box('SM_Depot_Back',(0,4.75,2.5),(9,.50,5),'Concrete')
    for x in (-3.5,3.5): box('SM_Depot_Front',(x,-4.75,2.5),(3,.50,5),'Concrete')
    box('SM_Depot_Lintel',(0,-4.75,4.4),(4,.50,1.2),'Concrete')
    box('Floor',(0,0,.04),(10,10,.08),'Concrete')
    roof('SM_Depot_Roof',10.4,10.4,5,'Metal')
    for x in (-3.7,-1.7,1.7,3.7):
        box('Roof_Support',(x,0,5.2),(.10,10.5,.15),'Ink')
    for x in (-3.45,3.45): window(x,-5.03,2.9,1.30,1.30)
    box('Sign',(0,-5.11,4.43),(3.3,.10,.55),'Blue')
    for x in (-1.10,-.55,0,.55,1.10): box('Sign_Signal',(x,-5.18,4.43),(.20,.025,.30),'Glow')
    for x in (-2.08,2.08): box('Gateframe',(x,-5.04,1.9),(.20,.20,3.8),'Warning')
    save('Warehouse','Buildings')

def tower():
    start('Tower')
    box('SM_Neon_Tower',(0,0,8),(8,7,16),'Ink')
    box('Base',(0,0,.24),(9,8,.48),'Concrete')
    for z in (2,5,8,11,14):
        box('Horizontal_Trim',(0,-3.55,z),(8.1,.11,.16),'Metal')
        for x in (-2.6,0,2.6): window(x,-3.57,z+.65,1.7,1.1)
        for x in (-4.02,4.02): box('Side_Neon',(x,0,z),( .08,7,.055),'Teal')
    box('Canopy',(0,-4.1,2.8),(3.8,1.7,.25),'Teal')
    rod('Roof_Aerial',(0,0,16),(0,0,20),.065,'Metal')
    ico('Aerial_Light',(0,0,20.05),(.17,.17,.17),'Warning')
    box('Roof_Chiller',(1.5,1,16.55),(2.2,2.5,1.1),'Concrete')
    save('Tower','Buildings')

def tree():
    start('Tree')
    rod('SM_Tree_Trunk',(0,0,0),(.05,0,3.2),.23,'Wood',r2=.12)
    for a in [0,2.1,4.2]:
        rod('Branch',(0,0,1.9),(math.cos(a)*.9,math.sin(a)*.9,3.0),.065,'Wood',r2=.035)
    for pos,scale,color in [((0,0,3.55),(1.45,1.30,1.4),'Leaf'),((.80,0,3.15),(1.05,1,1),'LeafLight'),((-.7,.5,3.16),(1.12,1.03,.95),'Leaf'),((0,-.85,3.2),(1.13,.95,1.05),'LeafLight')]:
        ico('SM_Tree_Canopy',pos,scale,color,sub=1)
    save('Tree','Environment')

def lamp():
    start('Lamp')
    rod('SM_Neon_Lamp_Base',(0,0,0),(0,0,.17),.28,'Concrete',vertices=10)
    rod('SM_Neon_Lamp_Pole',(0,0,.17),(0,0,4.8),.065,'Ink')
    rod('SM_Neon_Lamp_Arm',(0,0,4.75),(0,-.75,4.75),.045,'Ink')
    box('Lamp_Shade',(0,-.73,4.68),(.51,.52,.10),'Metal')
    box('Lamp_Bulb',(0,-.73,4.62),(.40,.40,.035),'Glow')
    save('Lamp','Props')

def terminal():
    start('Terminal')
    box('Terminal_Base',(0,0,.08),(.75,.65,.16),'Concrete')
    box('Terminal_Body',(0,0,.76),(.56,.48,1.36),'Ink')
    box('Terminal_Shell',(0,-.08,1.3),(.66,.57,.52),'Metal',rot=(math.radians(12),0,0))
    box('Terminal_Screen',(0,-.385,1.32),(.51,.035,.34),'Glass',rot=(math.radians(12),0,0))
    for i in range(4):
        box('Terminal_Code',(-.04,-.409,1.41-i*.055),(.32-i*.045,.012,.016),'Glow')
    box('Keyboard',(0,-.29,1.01),(.53,.24,.06),'Concrete')
    for x in (-.16,-.08,0,.08,.16): box('Key',(x,-.32,1.05),(.045,.08,.025),'Ink')
    rod('Indicator',(.21,-.26,.84),(.21,-.28,.84),.022,'Warning')
    save('Terminal','Props')

def crate():
    start('Crate')
    box('Crate',(0,0,.50),(1,1,1),'Wood')
    for x in (-.44,.44): box('Crate_Edge',(x,0,.50),(.13,1.05,1.05),'WoodLight')
    for z in (.06,.94): box('Crate_Edge',(0,0,z),(1.03,1.06,.13),'WoodLight')
    for y in (-.514,.514):
        box('Crate_Diagonal',(0,y,.50),(.09,.06,1.16),'WoodLight',rot=(0,math.radians(41),0))
    box('Shipping_Plate',(0,-.548,.55),(.21,.03,.17),'Teal')
    save('Crate','Props')

def boat():
    start('Boat')
    verts=[(-1.05,-2.6,.55),(1.05,-2.6,.55),(-1.30,1.5,.55),(1.30,1.5,.55),(0,3.1,.55),
        (-.6,-2.4,0),(.6,-2.4,0),(-.6,1.25,0),(.6,1.25,0),(0,2.4,.10)]
    mesh('Boat_Hull',verts,[(0,1,6,5),(0,5,7,2),(1,3,8,6),(2,7,9,4),(3,4,9,8),(5,6,8,9,7)],'Wood')
    mesh('Boat_Deck',verts[:5],[(0,1,3,4,2)],'WoodLight')
    for x in (-1.1,1.1): rod('Rail',(x,-2.4,.65),(x,1.5,.65),.06,'Cream')
    box('Cabin',(0,-.80,1.15),(1.3,1.6,1.15),'Cream')
    box('Cabin_Roof',(0,-.80,1.8),(1.55,1.85,.14),'Red')
    window(0,-1.63,1.3,.86,.46)
    rod('Mast',(0,.56,.55),(0,.56,4.0),.065,'Wood')
    mesh('Flag',[(0,.56,3.80),(0,.56,3.25),(1,.56,3.42)],[(0,1,2)],'Red')
    rod('Mooring',(-.6,-2.2,.55),(-.6,-2.2,.73),.045,'Metal')
    save('Boat','Props')

def fort():
    start('Fort')
    # Open entry, open courtyard. Runtime supplies floor and walkable stairs as needed.
    for x in (-7.5,7.5):
        box('Fort_Wall',(x,0,3.0),(1.4,16,6),'Concrete')
    box('Fort_Back',(0,7.5,3.0),(14,1.4,6),'Concrete')
    for x in (-5.5,5.5): box('Fort_Front',(x,-7.5,3.0),(5,1.4,6),'Concrete')
    box('Fort_Gate_Header',(0,-7.5,5.15),(6,1.4,1.7),'Concrete')
    for x in (-7.5,7.5):
        for y in (-7.5,7.5):
            rod('Fort_Turret',(x,y,0),(x,y,8),1.75,'Concrete',vertices=8)
            rod('Turret_Trim',(x,y,7.7),(x,y,8.1),1.90,'Plaster',vertices=8)
            for a in range(8):
                ang=a*math.pi/4
                box('Turret_Crenel',(x+math.cos(ang)*1.5,y+math.sin(ang)*1.5,8.5),(.65,.65,.95),'Concrete')
    for x in range(-6,7,2): box('Wall_Crenel',(x,7.5,6.40),(.90,1.5,.80),'Concrete')
    for y in range(-5,6,2):
        for x in (-7.5,7.5): box('Wall_Crenel',(x,y,6.40),(1.5,.90,.80),'Concrete')
    for x in (-3.15,3.15): box('Gate_Accent',(x,-8.24,2.15),(.13,.05,4.3),'Red')
    box('Gate_Symbol',(0,-8.24,5.2),(1.1,.06,.70),'Teal')
    save('Fort','Buildings')

def main():
    human('Nico'); human('Sillage',sillage=True); human('NPC',npc=True)
    drone(); robot('Sentinel'); corrupted(); robot('Boss',boss=True)
    house(); warehouse(); tower(); tree(); lamp(); terminal(); crate(); boat(); fort()
    (ROOT/'Blender'/'asset_report.json').write_text(json.dumps(REPORT,indent=2),encoding='utf-8')
    print('RIVE404_ASSET_REPORT '+json.dumps(REPORT))

if __name__=='__main__': main()
