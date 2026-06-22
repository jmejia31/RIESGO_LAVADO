from reportlab.lib import colors
from reportlab.lib.pagesizes import A4, landscape
from reportlab.pdfgen import canvas
from reportlab.pdfbase.pdfmetrics import stringWidth


OUT = "docs/Diseno_Catalogos_LAFT.pdf"
PAGE_W, PAGE_H = landscape(A4)

NAVY = colors.HexColor("#0f2f64")
BLUE = colors.HexColor("#1d4ed8")
LIGHT_BLUE = colors.HexColor("#eef5ff")
MUTED = colors.HexColor("#64748b")
TEXT = colors.HexColor("#0f172a")
BORDER = colors.HexColor("#dbe3ef")
BG = colors.HexColor("#f4f6f9")
GREEN = colors.HexColor("#15803d")
AMBER = colors.HexColor("#b45309")
RED = colors.HexColor("#b91c1c")
WHITE = colors.white


def rounded(c, x, y, w, h, r=10, stroke=BORDER, fill=WHITE, sw=1):
    c.setStrokeColor(stroke)
    c.setFillColor(fill)
    c.setLineWidth(sw)
    c.roundRect(x, y, w, h, r, stroke=1, fill=1)


def txt(c, text, x, y, size=10, color=TEXT, font="Helvetica", bold=False):
    c.setFillColor(color)
    c.setFont("Helvetica-Bold" if bold else font, size)
    c.drawString(x, y, text)


def centered(c, text, x, y, w, size=10, color=TEXT, bold=False):
    c.setFillColor(color)
    c.setFont("Helvetica-Bold" if bold else "Helvetica", size)
    c.drawCentredString(x + w / 2, y, text)


def wrap_lines(text, max_w, size=9, bold=False):
    words = text.split()
    lines, current = [], ""
    font = "Helvetica-Bold" if bold else "Helvetica"
    for word in words:
        cand = word if not current else current + " " + word
        if stringWidth(cand, font, size) <= max_w:
            current = cand
        else:
            if current:
                lines.append(current)
            current = word
    if current:
        lines.append(current)
    return lines


def paragraph(c, text, x, y, w, size=9, color=MUTED, leading=13):
    for line in wrap_lines(text, w, size):
        txt(c, line, x, y, size, color)
        y -= leading
    return y


def pill(c, text, x, y, w, fill, color=TEXT):
    rounded(c, x, y, w, 20, 7, stroke=fill, fill=fill)
    centered(c, text, x, y + 6, w, 8, color, True)


def header(c, title, subtitle):
    c.setFillColor(BG)
    c.rect(0, 0, PAGE_W, PAGE_H, stroke=0, fill=1)
    rounded(c, 24, PAGE_H - 78, PAGE_W - 48, 52, 14, stroke=colors.HexColor("#e8edf5"), fill=WHITE)
    txt(c, title, 44, PAGE_H - 50, 18, TEXT, bold=True)
    txt(c, subtitle, 44, PAGE_H - 67, 9, MUTED)


def footer(c, page):
    txt(c, "Diseno conceptual - Catalogos LAFT / Monitoreo de Listas", 28, 18, 8, MUTED)
    centered(c, str(page), PAGE_W - 60, 18, 30, 8, MUTED)


def draw_table(c, x, y, w, rows, headers):
    col_w = [w * 0.18, w * 0.18, w * 0.20, w * 0.17, w * 0.17, w * 0.10]
    rounded(c, x, y - 32 - (len(rows) * 33), w, 36 + len(rows) * 33, 8, stroke=BORDER, fill=WHITE)
    c.setFillColor(colors.HexColor("#f8fafc"))
    c.rect(x + 1, y - 31, w - 2, 30, stroke=0, fill=1)
    cur = x
    for i, h in enumerate(headers):
        txt(c, h, cur + 8, y - 20, 7, MUTED, bold=True)
        cur += col_w[i]
    c.setStrokeColor(BORDER)
    c.line(x, y - 32, x + w, y - 32)
    yy = y - 55
    for row in rows:
        cur = x
        for i, cell in enumerate(row):
            if i in (3, 4):
                color = GREEN if "Cerrado" in cell or "Validacion" in cell else AMBER
                pill(c, cell, cur + 7, yy - 4, col_w[i] - 14, colors.HexColor("#ecfdf5") if color == GREEN else colors.HexColor("#fff7ed"), color)
            else:
                txt(c, cell, cur + 8, yy, 8, TEXT if i == 0 else MUTED, bold=(i == 0))
            cur += col_w[i]
        c.setStrokeColor(colors.HexColor("#eef2f7"))
        c.line(x, yy - 13, x + w, yy - 13)
        yy -= 33


def page_cover(c):
    c.setFillColor(NAVY)
    c.rect(0, 0, PAGE_W, PAGE_H, stroke=0, fill=1)
    c.setFillColor(colors.HexColor("#133d7c"))
    c.circle(PAGE_W - 80, PAGE_H - 70, 150, stroke=0, fill=1)
    c.setFillColor(colors.HexColor("#1e40af"))
    c.circle(80, 40, 115, stroke=0, fill=1)

    txt(c, "Propuesta de Diseno", 60, PAGE_H - 120, 18, colors.HexColor("#bfdbfe"), bold=True)
    txt(c, "Catalogos LAFT", 60, PAGE_H - 165, 38, WHITE, bold=True)
    txt(c, "Estados, acciones de seguimiento y tipos de evidencia", 60, PAGE_H - 195, 15, colors.HexColor("#dbeafe"))
    paragraph(c, "Maqueta conceptual para evaluar como se integrarian nuevos catalogos LAFT dentro del modulo Monitoreo de Listas sin alterar Active Directory ni los accesos actuales.", 60, PAGE_H - 245, 500, 11, colors.HexColor("#dbeafe"), 16)

    rounded(c, 60, 82, 260, 86, 12, stroke=colors.HexColor("#3b82f6"), fill=colors.HexColor("#102f66"))
    txt(c, "Alcance visual", 82, 140, 12, WHITE, bold=True)
    txt(c, "1. Modal de seguimiento", 82, 119, 10, colors.HexColor("#dbeafe"))
    txt(c, "2. Nueva pantalla Catalogos LAFT", 82, 102, 10, colors.HexColor("#dbeafe"))
    txt(c, "3. Impacto tecnico propuesto", 82, 85, 10, colors.HexColor("#dbeafe"))

    rounded(c, 356, 82, 330, 86, 12, stroke=colors.HexColor("#3b82f6"), fill=colors.HexColor("#102f66"))
    txt(c, "Nota de criterio", 378, 140, 12, WHITE, bold=True)
    paragraph(c, "Esto no reemplaza Active Directory. AD autentica al usuario; estos catalogos solo ordenarian el seguimiento LAFT dentro del sistema.", 378, 118, 280, 9, colors.HexColor("#dbeafe"), 13)
    footer(c, 1)


def page_monitoreo(c):
    header(c, "Pantalla propuesta: Monitoreo de Listas", "Los catalogos LAFT se usarian al registrar seguimiento, evidencia o cierre de un caso.")

    x, y, w = 34, PAGE_H - 120, PAGE_W - 68
    txt(c, "Monitoreo de Listas", x, y + 26, 15, TEXT, bold=True)
    txt(c, "Vista principal con estados y acciones normalizadas", x, y + 9, 9, MUTED)
    rounded(c, x, y - 224, w, 238, 14, stroke=colors.HexColor("#e5e7eb"), fill=WHITE)

    rounded(c, x + 18, y - 28, 160, 30, 8, stroke=BORDER, fill=colors.HexColor("#f8fafc"))
    txt(c, "Buscar documento...", x + 30, y - 10, 8, MUTED)
    pill(c, "Juridicas", x + 195, y - 25, 70, LIGHT_BLUE, BLUE)
    pill(c, "Naturales", x + 273, y - 25, 70, colors.HexColor("#f8fafc"), MUTED)
    pill(c, "Empleados", x + 351, y - 25, 72, colors.HexColor("#f8fafc"), MUTED)
    pill(c, "Exportar Excel", x + w - 128, y - 25, 100, colors.HexColor("#dcfce7"), GREEN)

    headers = ["Documento", "Nombre", "Lista", "Estado LAFT", "Ultima accion", ""]
    rows = [
        ["0714197700256", "Javier Mejia", "PEP / Interna", "En analisis", "Validacion", "Abrir"],
        ["0511198600466", "Maria Lopez", "ONU", "Pendiente", "Revision", "Abrir"],
        ["0706196000054", "Empresa Alfa", "OFAC", "Cerrado", "Cierre", "Abrir"],
    ]
    draw_table(c, x + 18, y - 60, w - 36, rows, headers)

    # Modal
    modal_x, modal_y, modal_w, modal_h = 190, 66, 460, 250
    c.setFillColor(colors.Color(0, 0, 0, alpha=0.08))
    c.roundRect(modal_x + 5, modal_y - 5, modal_w, modal_h, 14, stroke=0, fill=1)
    rounded(c, modal_x, modal_y, modal_w, modal_h, 14, stroke=BORDER, fill=WHITE)
    txt(c, "Registrar seguimiento LAFT", modal_x + 22, modal_y + modal_h - 32, 15, TEXT, bold=True)
    txt(c, "Caso: 0714197700256 - Javier Mejia", modal_x + 22, modal_y + modal_h - 50, 9, MUTED)

    labels = [
        ("Accion realizada", "Validacion documental"),
        ("Estado resultante", "En analisis"),
        ("Tipo de evidencia", "Informe / oficio"),
    ]
    lx = modal_x + 22
    ly = modal_y + modal_h - 92
    for i, (lab, val) in enumerate(labels):
        bx = lx + (i * 140)
        txt(c, lab, bx, ly + 27, 8, MUTED, bold=True)
        rounded(c, bx, ly, 126, 24, 7, stroke=BORDER, fill=colors.HexColor("#f8fafc"))
        txt(c, val, bx + 8, ly + 8, 8, TEXT)

    txt(c, "Comentario", modal_x + 22, modal_y + 118, 8, MUTED, bold=True)
    rounded(c, modal_x + 22, modal_y + 64, modal_w - 44, 46, 8, stroke=BORDER, fill=colors.HexColor("#f8fafc"))
    paragraph(c, "Se reviso documentacion soporte y se deja el caso en analisis para validacion de Cumplimiento.", modal_x + 32, modal_y + 94, modal_w - 64, 8, MUTED, 11)

    txt(c, "Adjuntar evidencia", modal_x + 22, modal_y + 42, 8, MUTED, bold=True)
    rounded(c, modal_x + 22, modal_y + 14, 240, 22, 7, stroke=BORDER, fill=colors.HexColor("#f8fafc"))
    txt(c, "oficio_validacion.pdf", modal_x + 32, modal_y + 22, 8, TEXT)
    pill(c, "Guardar seguimiento", modal_x + modal_w - 148, modal_y + 14, 126, BLUE, WHITE)

    footer(c, 2)


def page_catalogos(c):
    header(c, "Nueva pantalla propuesta: Catalogos LAFT", "Administracion de estados, acciones y tipos de evidencia sin mezclarlo con Active Directory.")

    x, y, w = 34, PAGE_H - 112, PAGE_W - 68
    rounded(c, x, 58, w, PAGE_H - 150, 14, stroke=colors.HexColor("#e5e7eb"), fill=WHITE)
    txt(c, "Catalogos LAFT", x + 22, y, 17, TEXT, bold=True)
    txt(c, "Mantenimiento administrado por usuarios con permiso al modulo.", x + 22, y - 17, 9, MUTED)
    pill(c, "Nuevo item", x + w - 110, y - 8, 86, BLUE, WHITE)

    tabs = [("Estados LAFT", True), ("Acciones", False), ("Tipos de evidencia", False)]
    tx = x + 22
    for name, active in tabs:
        pill(c, name, tx, y - 54, 118, LIGHT_BLUE if active else colors.HexColor("#f8fafc"), BLUE if active else MUTED)
        tx += 128

    left_x, top_y = x + 22, y - 92
    col_w = [70, 180, 320, 90, 90]
    headers = ["Codigo", "Nombre", "Descripcion", "Color", "Estado"]
    rows = [
        ["PEND", "Pendiente", "Caso pendiente de revision inicial", "Amarillo", "Activo"],
        ["ANAL", "En analisis", "Caso bajo revision de Cumplimiento", "Azul", "Activo"],
        ["CONF", "Confirmado", "Coincidencia confirmada como relevante", "Rojo", "Activo"],
        ["FALP", "Falso positivo", "Coincidencia descartada por validacion", "Verde", "Activo"],
        ["CERR", "Cerrado", "Caso cerrado con evidencia registrada", "Gris", "Activo"],
    ]
    table_w = sum(col_w)
    rounded(c, left_x, top_y - 32 - len(rows) * 34, table_w, 38 + len(rows) * 34, 8, stroke=BORDER, fill=WHITE)
    c.setFillColor(colors.HexColor("#f8fafc"))
    c.rect(left_x + 1, top_y - 31, table_w - 2, 30, stroke=0, fill=1)
    cur = left_x
    for i, h in enumerate(headers):
        txt(c, h, cur + 8, top_y - 20, 8, MUTED, bold=True)
        cur += col_w[i]
    yy = top_y - 55
    for r in rows:
        cur = left_x
        for i, cell in enumerate(r):
            if i == 3:
                fill = {
                    "Amarillo": colors.HexColor("#fef3c7"),
                    "Azul": colors.HexColor("#dbeafe"),
                    "Rojo": colors.HexColor("#fee2e2"),
                    "Verde": colors.HexColor("#dcfce7"),
                    "Gris": colors.HexColor("#f1f5f9"),
                }[cell]
                pill(c, cell, cur + 8, yy - 7, 68, fill, TEXT)
            elif i == 4:
                pill(c, cell, cur + 8, yy - 7, 62, colors.HexColor("#dcfce7"), GREEN)
            else:
                txt(c, cell, cur + 8, yy, 8, TEXT if i == 1 else MUTED, bold=(i == 1))
            cur += col_w[i]
        c.setStrokeColor(colors.HexColor("#eef2f7"))
        c.line(left_x, yy - 14, left_x + table_w, yy - 14)
        yy -= 34

    box_y = 78
    cards = [
        ("Estados LAFT", "Pendiente, en analisis, confirmado, falso positivo, cerrado."),
        ("Acciones", "Revision, validacion, escalamiento, cierre, solicitud de soporte."),
        ("Tipos de evidencia", "Oficio, acta, captura, informe, documento, constancia."),
    ]
    cx = x + 22
    for title, body in cards:
        rounded(c, cx, box_y, 238, 58, 10, stroke=BORDER, fill=colors.HexColor("#f8fafc"))
        txt(c, title, cx + 14, box_y + 36, 10, TEXT, bold=True)
        paragraph(c, body, cx + 14, box_y + 21, 208, 8, MUTED, 10)
        cx += 252

    footer(c, 3)


def page_impact(c):
    header(c, "Impacto tecnico si se implementa", "Separar catalogos LAFT requiere cambios de datos y formularios; por eso se recomienda como mejora planificada.")

    x, y = 44, PAGE_H - 120
    items = [
        ("Backend nuevo", "CatalogosLaftController, CatalogosLaftService y CatalogosLaftRepository para aislar estos catalogos del CatalogosController actual."),
        ("Base de datos", "Tablas nuevas: RL_CAT_ESTADOS_LAFT, RL_CAT_ACCIONES_SEGUIMIENTO y RL_CAT_TIPOS_EVIDENCIA. Opcionalmente llaves en RL_DETALLE_LISTA y RL_DETALLE_EVIDENCIA."),
        ("Frontend", "Monitoreo de Listas consumiria los catalogos en los formularios de seguimiento y evidencia. Una pantalla nueva permitiria administrarlos."),
        ("Auditoria", "Cada cambio de catalogo y cada uso en seguimiento quedaria trazado en RL_AUDITORIA con INSERT, UPDATE, DELETE o VER."),
    ]
    for i, (title, body) in enumerate(items):
        yy = y - i * 82
        rounded(c, x, yy - 48, 350, 64, 10, stroke=BORDER, fill=WHITE)
        pill(c, str(i + 1), x + 16, yy - 24, 24, BLUE, WHITE)
        txt(c, title, x + 52, yy - 7, 12, TEXT, bold=True)
        paragraph(c, body, x + 52, yy - 25, 270, 8, MUTED, 11)

    # Flow diagram
    fx, fy = 470, PAGE_H - 150
    nodes = [
        ("AD", "Autentica usuario"),
        ("SGRLA", "Carga modulos permitidos"),
        ("Catalogos LAFT", "Normaliza estados/acciones"),
        ("Monitoreo", "Registra seguimiento"),
        ("Bitacora", "Deja evidencia"),
    ]
    for i, (title, body) in enumerate(nodes):
        ny = fy - i * 62
        rounded(c, fx, ny, 260, 42, 10, stroke=BLUE if i == 2 else BORDER, fill=LIGHT_BLUE if i == 2 else WHITE)
        txt(c, title, fx + 16, ny + 24, 10, TEXT, bold=True)
        txt(c, body, fx + 16, ny + 10, 8, MUTED)
        if i < len(nodes) - 1:
            c.setStrokeColor(MUTED)
            c.line(fx + 130, ny - 4, fx + 130, ny - 18)
            c.line(fx + 125, ny - 13, fx + 130, ny - 18)
            c.line(fx + 135, ny - 13, fx + 130, ny - 18)

    rounded(c, 44, 56, PAGE_W - 88, 54, 12, stroke=colors.HexColor("#fde68a"), fill=colors.HexColor("#fffbeb"))
    txt(c, "Recomendacion", 62, 86, 11, AMBER, bold=True)
    paragraph(c, "No incluirlo como cierre obligatorio del modulo base. Conviene documentarlo como mejora futura, porque agrega reglas de negocio y migracion de datos.", 62, 70, PAGE_W - 130, 9, TEXT, 12)
    footer(c, 4)


def main():
    c = canvas.Canvas(OUT, pagesize=landscape(A4))
    c.setTitle("Diseno Catalogos LAFT")
    page_cover(c)
    c.showPage()
    page_monitoreo(c)
    c.showPage()
    page_catalogos(c)
    c.showPage()
    page_impact(c)
    c.save()


if __name__ == "__main__":
    main()
