from PIL import Image
from pathlib import Path
import os

POS_FILE = [
    (0, 0, "Unpressed/Default"),
    (4, 0, "Unpressed/Borderless"),
    (8, 0, "Unpressed/White"),
    (12, 0, "Unpressed/White Borderless"),
    (0, 2, "Pressed/Default"),
    (2, 2, "Pressed/Black"),
    (4, 2, "Pressed/Borderless")
]

INPUT_PATH = Path("pre")

for f in os.listdir(INPUT_PATH):
    print(f)
    img = Image.open(INPUT_PATH / f)

    w = img.width // 16
    h = img.height // 4

    for l, u, path in POS_FILE:
        p = Path(path) / Path(f).name.lower()

        crop = img.crop((w * l, h * u, w * (l + 1), h * (u + 1)))
        crop.save(p)

