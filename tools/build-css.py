"""Assemble les feuilles de WaesesUI/Styles en un seul fichier minifié : WaesesUI/wwwroot/css/waeses.min.css.

Usage : python tools/build-css.py            (écrit le fichier)
        python tools/build-css.py --check    (échoue si le fichier versionné n'est pas à jour, utilisé en CI)
L'ordre est celui de la cascade : theme.css, puis components-extended.css (qui surcharge theme.css).
"""
import pathlib, re, sys

ROOT = pathlib.Path(__file__).resolve().parent.parent / "WaesesUI"
SOURCES = ["theme.css", "components-extended.css"]
OUTPUT = ROOT / "wwwroot" / "css" / "waeses.min.css"
HEADER = "/*! WaesesUI - theme + composants. Généré par tools/build-css.py : ne pas modifier à la main. */"


def minify(css: str) -> str:
    css = re.sub(r"/\*.*?\*/", "", css, flags=re.S)
    css = re.sub(r"\s+", " ", css)
    css = re.sub(r"\s*([{};,])\s*", r"\1", css)      # l'espace avant ':' reste : "a :is(b)" est un combinateur
    css = re.sub(r":\s+", ":", css)
    css = css.replace(";}", "}")
    return css.strip()


def build() -> str:
    parts = [(ROOT / "Styles" / name).read_text(encoding="utf-8") for name in SOURCES]
    return HEADER + "\n" + minify("\n".join(parts)) + "\n"


if __name__ == "__main__":
    built = build()
    if "--check" in sys.argv:
        current = OUTPUT.read_text(encoding="utf-8") if OUTPUT.exists() else ""
        if current.replace("\r\n", "\n") != built:
            sys.exit("waeses.min.css n'est pas à jour : lancez python tools/build-css.py")
    else:
        OUTPUT.write_bytes(built.encode("utf-8"))
        print(f"{OUTPUT.relative_to(ROOT.parent)} : {len(built)} octets")
