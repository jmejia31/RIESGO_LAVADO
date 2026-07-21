from pathlib import Path

path = Path("backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosApplicationTests.cs")
text = path.read_text(encoding="utf-8")
old = "            var contenido = Encoding.UTF8.GetString(result.Data.Contenido);"
new = "            var contenido = System.Net.WebUtility.HtmlDecode(Encoding.UTF8.GetString(result.Data.Contenido));"

if new not in text:
    if old not in text:
        raise RuntimeError("No se encontró la validación de contenido Excel esperada.")
    text = text.replace(old, new, 1)
    path.write_text(text, encoding="utf-8")

print("Validación Excel ajustada para comparar contenido HTML decodificado.")
