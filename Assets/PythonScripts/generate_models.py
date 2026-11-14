import bpy

# 1. Alte Objekte in der Szene löschen
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete()

# Farbdefinitionen aus OpenSCAD (RGB-Werte 0-1)
c_concrete   = (0.75, 0.75, 0.75, 1.0)
c_dark_metal = (0.20, 0.20, 0.22, 1.0)
c_glass      = (0.20, 0.50, 0.80, 0.6) # Mit Transparenz für Unity
c_accent     = (0.85, 0.25, 0.15, 1.0)
c_orange     = (0.90, 0.60, 0.10, 1.0)
c_pad        = (0.30, 0.30, 0.35, 1.0)
c_asphalt    = (0.20, 0.22, 0.24, 1.0)

# Hilfsfunktion zum Erstellen von Unity-kompatiblen Materialien
def create_mat(name, rgba, roughness=0.5, metallic=0.0):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs['Base Color'].default_value = rgba
    bsdf.inputs['Roughness'].default_value = roughness
    bsdf.inputs['Metallic'].default_value = metallic
    if rgba[3] < 1.0:
        bsdf.inputs['Alpha'].default_value = rgba[3]
        mat.blend_method = 'BLEND'
    return mat

mat_con  = create_mat("Mat_Beton", c_concrete, 0.8)
mat_dark = create_mat("Mat_Stahl", c_dark_metal, 0.3, 0.8)
mat_glas = create_mat("Mat_Glas", c_glass, 0.1)
mat_acc  = create_mat("Mat_Accent", c_accent, 0.4)
mat_org  = create_mat("Mat_Orange", c_orange, 0.5)
mat_pad  = create_mat("Mat_Pad", c_pad, 0.7)
mat_asp  = create_mat("Mat_Asphalt", c_asphalt, 0.9)

def assign_material(obj, mat):
    if obj.data.materials:
        obj.data.materials[0] = mat
    else:
        obj.data.materials.append(mat)

# ==========================================
# MODELL 1: KONTROLLTURM (-120, 0, 0)
# ==========================================
objects_turm = []

# Turm-Schaft (6-kantig)
bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=6, depth=70, location=(-120, 0, 35))
turm_schaft = bpy.context.active_object
assign_material(turm_schaft, mat_con)
objects_turm.append(turm_schaft)

# Vertikale Akzente
for i in range(6):
    angle = i * (360 / 6)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(-120, 0, 35))
    acc = bpy.context.active_object
    acc.scale = (1, 0.8, 70)
    bpy.ops.object.transform_apply(scale=True)
    acc.rotation_euler[2] = math_radians = angle * (3.14159 / 180)
    # Einfache radiale Positionierung per Hand/Offset
    assign_material(acc, mat_acc)
    objects_turm.append(acc)

# Aussichtsplattform Kabine
bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=10, depth=8, location=(-120, 0, 69))
kabine = bpy.context.active_object
assign_material(kabine, mat_dark)
objects_turm.append(kabine)

bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=9.5, depth=5, location=(-120, 0, 71.5))
glas_kabine = bpy.context.active_object
assign_material(glas_kabine, mat_glas)
objects_turm.append(glas_kabine)

# Export Modell 1
bpy.ops.object.select_all(action='DESELECT')
for obj in objects_turm:
    obj.select_set(True)
bpy.context.view_layer.objects.active = objects_turm[0]
bpy.ops.object.join()
turm_obj = bpy.context.active_object
turm_obj.name = "SpaceAgency_ControlTower"
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.uv.smart_project()
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.export_scene.obj(filepath="C:/tmp/SpaceAgency_ControlTower.obj", use_selection=True)


# ==========================================
# MODELL 2: HAUPTGEBÄUDE & VAB
# ==========================================
objects_hq = []

# Admin Gebäude
bpy.ops.mesh.primitive_cube_add(size=2, location=(-30 + 25, -20 + 45 + 15, 11))
admin = bpy.context.active_object
admin.scale = (25, 15, 11)
bpy.ops.object.transform_apply(scale=True)
assign_material(admin, mat_con)
objects_hq.append(admin)

# VAB Halle
bpy.ops.mesh.primitive_cube_add(size=2, location=(-30 + 10 + 22.5, -20 - 20 + 30, 22.5))
vab = bpy.context.active_object
vab.scale = (22.5, 30, 22.5)
bpy.ops.object.transform_apply(scale=True)
assign_material(vab, mat_con)
objects_hq.append(vab)

# Export Modell 2
bpy.ops.object.select_all(action='DESELECT')
for obj in objects_hq:
    obj.select_set(True)
bpy.context.view_layer.objects.active = objects_hq[0]
bpy.ops.object.join()
hq_obj = bpy.context.active_object
hq_obj.name = "SpaceAgency_HQ_VAB"
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.uv.smart_project()
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.export_scene.obj(filepath="C:/tmp/SpaceAgency_HQ_VAB.obj", use_selection=True)


# ==========================================
# MODELL 3: STARTRAMPE (STARTPAD & GANTRY)
# ==========================================
objects_pad = []

# Betonsockel
bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=26, depth=3, location=(130, 10, 1.5))
pad_sockel = bpy.context.active_object
assign_material(pad_sockel, mat_pad)
objects_pad.append(pad_sockel)

# Gantry Serviceturm
bpy.ops.mesh.primitive_cube_add(size=2, location=(130 + 14 + 3.5, 10 - 8 + 8, 26))
gantry = bpy.context.active_object
gantry.scale = (3.5, 8, 26)
bpy.ops.object.transform_apply(scale=True)
assign_material(gantry, mat_dark)
objects_pad.append(gantry)

# Export Modell 3
bpy.ops.object.select_all(action='DESELECT')
for obj in objects_pad:
    obj.select_set(True)
bpy.context.view_layer.objects.active = objects_pad[0]
bpy.ops.object.join()
pad_obj = bpy.context.active_object
pad_obj.name = "SpaceAgency_LaunchPad"
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.uv.smart_project()
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.export_scene.obj(filepath="C:/tmp/SpaceAgency_LaunchPad.obj", use_selection=True)

print("Export erfolgreich abgeschlossen: 3 OBJ-Dateien in C:/tmp/ gespeichert!")
