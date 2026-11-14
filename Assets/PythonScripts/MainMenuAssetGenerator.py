import numpy as np
from PIL import Image, ImageDraw

# --- EINSTELLUNGEN ---
WIDTH, HEIGHT = 1920, 1080  # Full-HD Auflösung (16:9)
NUM_STARS = 1200            # Deutlich mehr Sterne für ein dichtes All
FRAMES = 240                # 240 Frames bei 30 FPS = 8 Sekunden pro Loop (sehr langsam)
FPS = 30                    # Bilder pro Sekunde
MAX_DEPTH = 150.0           # Größere Raumtiefe für sanfteres Auftauchen
FOV = 500.0                 # Größeres Sichtfeld passend zur höheren Auflösung
OUTPUT_FILE = "starfield_menu_hd.gif"

# Reproduzierbarer Zufall
np.random.seed(42)

# Breiterer Streubereich für X und Y, damit auch bei 16:9 keine Ränder leer bleiben
x = np.random.uniform(-250, 250, NUM_STARS)
y = np.random.uniform(-150, 150, NUM_STARS)

# Zufällige Start-Tiefe
z_start = np.random.uniform(1, MAX_DEPTH, NUM_STARS)

# Leichte Farbvariationen für realistischere Sterne (Weiß, Warmweiß, Kühlblau)
star_colors = [
    (255, 255, 255),  # Reinweiß
    (220, 235, 255),  # Leichtes Blauweiß
    (255, 245, 225),  # Leichtes Warmweiß
    (190, 210, 255),  # Kühles Blau
]
color_indices = np.random.choice(len(star_colors), NUM_STARS, p=[0.6, 0.2, 0.15, 0.05])

frames = []

print("Generiere Frames... Das kann bei Full-HD einen Moment dauern.")

for i in range(FRAMES):
    img = Image.new("RGB", (WIDTH, HEIGHT), "black")
    draw = ImageDraw.Draw(img)
    
    progress = i / FRAMES
    z = (z_start - progress * MAX_DEPTH) % MAX_DEPTH
    z = np.clip(z, 0.1, MAX_DEPTH)
    
    # 3D-zu-2D-Projektion
    sx = (x / z) * FOV + (WIDTH / 2)
    sy = (y / z) * FOV + (HEIGHT / 2)
    
    for j in range(NUM_STARS):
        if 0 <= sx[j] < WIDTH and 0 <= sy[j] < HEIGHT:
            closeness = max(0.0, min(1.0, 1.0 - (z[j] / MAX_DEPTH)))
            
            # Radius skaliert für Full-HD (von 0.5 bis 4.5 Pixel)
            radius = max(0.5, (closeness ** 1.5) * 4.5)
            
            # Sanftes Einblenden aus der Ferne
            brightness_factor = closeness ** 0.9
            base_r, base_g, base_b = star_colors[color_indices[j]]
            
            color = (
                int(base_r * brightness_factor),
                int(base_g * brightness_factor),
                int(base_b * brightness_factor)
            )
            
            bbox = [sx[j] - radius, sy[j] - radius, sx[j] + radius, sy[j] + radius]
            draw.ellipse(bbox, fill=color)
            
    frames.append(img)

print("Speichere GIF...")
duration_ms = int(1000 / FPS)
frames[0].save(
    OUTPUT_FILE,
    save_all=True,
    append_images=frames[1:],
    duration=duration_ms,
    loop=0,
    optimize=True  # Versucht die Dateigröße etwas zu komprimieren
)

print(f"Fertig! Gespeichert als '{OUTPUT_FILE}'.")

